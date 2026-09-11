#!/usr/bin/env python3
"""
QA 자동 성장 루프 — 마더가 **사람 없이** 돌린다.

    투입 → 결과 대기 → 판정 → (실패면) Claude 호출해 교정 → 재투입 → …
                             (통과면) 다음 단계로

사람이 매번 "돌려 → 결과 봐줘 → 고쳐 → 다시" 하던 것을 루프가 대신한다.
성장은 **단계(curriculum)** 로 정의한다. 아래에서 위로 하나씩 통과해 올라간다:

    1 이동·발견   상자를 하나라도 **찾는가** (발견율 > 0)
    2 루팅        찾은 걸 **집는가** (루팅가치 > 0)
    3 탈출        레이드를 **끝내는가** (탈출 1회 이상)
    4 완주        계획한 사이클을 **다 도는가**
    5 수익        판당 **순이익이 양수**인가  ← 여기부터가 밸런스

각 라운드 결과는 qa-growth.jsonl에 쌓여 **성장 곡선**이 된다.

전제: 차일드가 **명령 대기 모드**여야 한다.
  • 에디터 — Play 후 **F10** (마더가 반복 지휘 가능)
  • 빌드   — `-qa-serve`

사용:
  py tools/qa_autoloop.py --rounds 12
  py tools/qa_autoloop.py --rounds 6 --no-agent      # Claude 호출 없이 관찰만
  py tools/qa_autoloop.py --stage 3                  # 특정 단계부터
"""

import argparse
import glob
import json
import os
import shutil
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
sys.path.insert(0, str(HERE))
import qa_orchestrator as orch  # noqa: E402

GROWTH = HERE / "qa-growth.jsonl"
AGENT_REQ = HERE / "qa-autoloop-request.md"
GAME_ISSUES = HERE / "qa-game-issues.md"      # 사람이 판단해야 할 게임 결함 모음

# ─────────────────────────────────────────────────────────────
#  권한 경계 (사용자 결정 2026-07-28)
#
#  "AI 버그를 수정하는 건 자체적으로 해도 되는데,
#   시스템 버그는 나에게 정리해서 말해주고 고쳐야 해."
#
#  → 루프가 스스로 고쳐도 되는 것: **QA/AI 쪽만**.
#     게임 코드·씬·데이터는 손대지 않고 qa-game-issues.md에 적어 사람에게 올린다.
#  지시만으로는 못 믿으므로 **git으로 검증**하고, 넘으면 루프를 세운다.
# ─────────────────────────────────────────────────────────────

SELF_FIX_ALLOWED = (
    "demo13-flashlight/Assets/Scripts/QA/",   # QA 봇·AI 코드
    "demo13-flashlight/docs/qa.md",           # QA 문서
    "tools/",                                 # 마더·루프 도구
)


def git_state():
    """{경로: 변경량} — HEAD 대비 + 미추적."""
    state = {}
    try:
        r = subprocess.run(["git", "diff", "--numstat", "HEAD"], cwd=str(REPO),
                           capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=60)
        for line in r.stdout.splitlines():
            parts = line.split("\t")
            if len(parts) == 3:
                state[parts[2]] = (parts[0], parts[1])
        r2 = subprocess.run(["git", "ls-files", "--others", "--exclude-standard"],
                            cwd=str(REPO), capture_output=True, text=True,
                            encoding="utf-8", errors="replace", timeout=60)
        for p in r2.stdout.splitlines():
            if p.strip():
                state.setdefault(p.strip(), ("신규", "-"))
    except Exception:
        pass
    return state


def changed_between(before, after):
    return sorted(p for p, v in after.items() if before.get(p) != v)


def split_scope(paths):
    """(QA쪽 = 자체 수정 허용, 게임쪽 = 사람 승인 필요)"""
    mine, theirs = [], []
    for p in paths:
        (mine if p.replace("\\", "/").startswith(SELF_FIX_ALLOWED) else theirs).append(p)
    return mine, theirs


# ─────────────────────────────────────────────────────────────
#  단계 정의 — "성장"이 무엇인지의 단일 정의
# ─────────────────────────────────────────────────────────────

STAGES = [
    {
        "name": "이동·발견",
        "goal": "상자를 하나라도 찾는다(발견율 > 0)",
        "check": lambda m: m["discovered"] > 0,
        "hint": "발견율 0 = 시야에 상자가 한 번도 안 들어옴. 시야 수치(visionRange/FovDegrees), "
                "상자 배치, 또는 AI가 탐색을 안 하는 것(정책 wExplore)이 원인.",
    },
    {
        "name": "루팅",
        "goal": "찾은 상자를 실제로 집는다(루팅가치 > 0)",
        "check": lambda m: m["lootValue"] > 0,
        "hint": "발견은 하는데 회수가 0 = 도달 실패(길찾기), E 상호작용 실패, 또는 "
                "상자가 비어 있음(루트 테이블).",
    },
    {
        "name": "탈출",
        "goal": "레이드를 끝낸다(탈출 1회 이상)",
        "check": lambda m: m["extracts"] > 0,
        "hint": "탈출구를 못 찾거나(발견), 못 가거나(길찾기), 건물 안에 갇힘(LeaveBuilding).",
    },
    {
        "name": "완주",
        "goal": "계획한 사이클을 다 돈다",
        "check": lambda m: m["cyclesDone"] >= m["cyclesPlanned"] > 0,
        "hint": "중간에 죽거나 막혀서 사이클이 끊긴다. DEATH/STUCK/RECOVER 로그를 볼 것.",
    },
    {
        "name": "수익",
        "goal": "판당 순이익이 양수 — 여기부터 밸런스",
        "check": lambda m: m["lootValue"] > 0 and m["cyclesDone"] > 0
                           and (m["lootValue"] / max(1, m["cyclesDone"])) > 0,
        "hint": "루팅은 되는데 수익이 안 남는다 = 경제 밸런스(sellRate·루트 가치·소모품 비용).",
    },
]


# ─────────────────────────────────────────────────────────────
#  결과 → 지표
# ─────────────────────────────────────────────────────────────

def newest_result(d: Path):
    files = [p for p in d.glob("*qa-result-*.json") if "latest" not in p.name]
    return max(files, key=lambda p: p.stat().st_mtime) if files else None


def metrics_of(j: dict) -> dict:
    """단계 판정에 쓰는 값만 뽑는다."""
    anomalies = j.get("anomalies", [])
    kinds = {}
    for a in anomalies:
        k = a.get("kind", "?")
        kinds[k] = kinds.get(k, 0) + 1

    loot = 0.0
    crates = 0
    for cy in j.get("cycles", []):
        loot += float(cy.get("lootValue", 0) or 0)
        crates += int(cy.get("cratesOpened", 0) or 0)

    # 발견율은 리포트 metric에 들어간다(ai.play가 기록).
    # JsonUtility가 Dictionary를 못 실어서 [{key,value}] 배열로 온다.
    disc = 0
    total = 0
    for entry in (j.get("metrics") or []):
        k = str(entry.get("key", ""))
        v = float(entry.get("value", 0) or 0)
        if "발견한 상자" in k:
            disc = max(disc, int(v))
        elif "상자 수" in k:
            total = max(total, int(v))

    checks = {c.get("name"): bool(c.get("passed")) for c in j.get("checks", [])}
    extracts = 0
    for c in j.get("checks", []):
        if c.get("name") == "레이드 탈출":
            det = c.get("detail", "")
            # "0/3 회 탈출 성공"
            try:
                extracts = int(det.split("/")[0])
            except Exception:
                extracts = 1 if c.get("passed") else 0

    return {
        "verdict": j.get("verdict"),
        "reason": j.get("verdictReason", ""),
        "cyclesDone": int(j.get("cyclesCompleted", 0) or 0),
        "cyclesPlanned": int(j.get("cyclesPlanned", 0) or 0),
        "durationSec": round(float(j.get("durationSec", 0) or 0), 1),
        "errors": int(j.get("errorCount", 0) or 0),
        "warns": int(j.get("warnCount", 0) or 0),
        "lootValue": loot,
        "crates": crates,
        "discovered": disc,
        "totalCrates": total,
        "discoveryRate": round(disc / total * 100, 1) if total else 0.0,
        "extracts": extracts,
        "anomalies": kinds,
        "checks": checks,
        "navGrids": j.get("navGrids", []),
    }


def read_json(p):
    try:
        return json.loads(Path(p).read_text(encoding="utf-8"))
    except Exception:
        return None


# ─────────────────────────────────────────────────────────────
#  Claude 호출 — 실패했을 때만
# ─────────────────────────────────────────────────────────────

def claude_exe():
    return shutil.which("claude") or shutil.which("claude.cmd")


def build_prompt(stage, m, prev, data_dir, rnd):
    L = [f"# QA 자동 루프 — 라운드 {rnd} 실패 (단계 {stage['name']})", "",
         f"**이 단계의 목표**: {stage['goal']}",
         f"**통과 못 한 이유 힌트**: {stage['hint']}", "",
         "게임: demo13-flashlight (Unity 6 탑다운 2D). QA 설명은 `demo13-flashlight/docs/qa.md`.",
         "봇 AI는 `Assets/Scripts/QA/QaBrain.cs`(판단) + `QaPerception.cs`(지각) + `QaSteps.cs`(실행).", "",
         "## 이번 라운드 결과", "",
         f"- 판정 {m['verdict']} — {m['reason']}",
         f"- 사이클 {m['cyclesDone']}/{m['cyclesPlanned']} · {m['durationSec']}초 · 오류 {m['errors']} 경고 {m['warns']}",
         f"- 상자 발견 {m['discovered']}/{m['totalCrates']} ({m['discoveryRate']}%) · 수색 {m['crates']}개 · 루팅가치 {m['lootValue']:.0f}",
         f"- 탈출 {m['extracts']}회", ""]

    if m["anomalies"]:
        L.append("### 이상")
        L += [f"- {k} × {v}" for k, v in sorted(m["anomalies"].items(), key=lambda kv: -kv[1])]
        L.append("")

    if m["navGrids"]:
        L.append("### 길찾기 격자")
        for g in m["navGrids"][:6]:
            if g.get("present"):
                L.append(f"- {g['scene']}: {g['width']}x{g['height']} 셀 {g['cellSize']:.2f}m 막힘 {g['blockedPct']:.1f}%")
            else:
                L.append(f"- {g['scene']}: 격자 없음")
        L.append("")

    if prev:
        L += ["### 직전 라운드 대비", "",
              f"- 발견 {prev['discovered']} → {m['discovered']}",
              f"- 루팅가치 {prev['lootValue']:.0f} → {m['lootValue']:.0f}",
              f"- 탈출 {prev['extracts']} → {m['extracts']}",
              f"- 사이클 {prev['cyclesDone']} → {m['cyclesDone']}", ""]

    L += ["## 해야 할 일", "",
          "1. **결정 로그를 먼저 읽어라** — AI가 무엇을 왜 골랐는지가 거기 있다:",
          f"   - `{data_dir / 'qa-decisions.jsonl'}`  (한 줄 = 한 결정: 상태·후보점수·선택·margin)",
          f"   - `{data_dir / 'qa-ask.json'}`  (AI가 스스로 애매하다고 신고한 것)",
          f"   - `{data_dir / 'qa-report-latest.txt'}` 또는 최신 `qa-report-*.txt` (사람이 읽는 전체 로그)",
          "2. 원인을 **하나만** 정하고 고쳐라. 한 라운드에 여러 개를 바꾸면 무엇이 효과였는지 알 수 없다.",
          "",
          "## ⛔ 손대도 되는 범위 (사용자 결정 — 반드시 지킬 것)", "",
          "**AI/QA 버그는 네가 고쳐라. 게임 시스템 버그는 절대 고치지 말고 보고만 해라.**", "",
          "고쳐도 되는 것:",
          f"   - `{data_dir / 'qa-policy.json'}` — AI 판단 가중치·임계 (QaBrain.Policy 필드명 그대로)",
          f"   - `{data_dir / 'qa-cases.json'}` — 조건→정답 행동",
          "   - `demo13-flashlight/Assets/Scripts/QA/**` — QA 봇·AI 코드",
          "   - `tools/**`, `demo13-flashlight/docs/qa.md`", "",
          "**절대 고치지 말 것** (게임 쪽): `Assets/Scripts/` 의 QA 폴더 밖 전부, `Assets/Scenes/`,",
          "`Assets/Resources/Data/`(StatDB·GameTuning 포함), 프리팹, 그 외 게임 에셋.", "",
          f"원인이 **게임 결함**이라고 판단되면 고치지 말고 `{GAME_ISSUES}` 에 아래 형식으로 **덧붙여라**:", "",
          "```markdown",
          "## [YYYY-MM-DD HH:MM] <한 줄 제목>",
          "- **증상**: (QA 로그에서 관찰된 것)",
          "- **근거**: (파일:줄, 좌표, 수치)",
          "- **원인 추정**: ",
          "- **제안 수정**: (어디를 어떻게)",
          "- **영향**: (플레이어가 겪는 결과)",
          "```", "",
          "그리고 그 라운드에는 QA 쪽에서 할 수 있는 **우회/완화**만 하거나, 없으면 아무것도 고치지 마라.",
          "",
          "3. 마지막에 **무엇을 왜 고쳤는지 한 줄**로 정리해라.", "",
          "정책 파일이 없으면 만들어라(QaBrain.Policy 필드명 그대로).",
          "사례 파일 형식: `{\"cases\":[{\"id\":\"...\",\"doGoal\":\"Extract\",\"why\":\"...\",\"weightMin\":0.9,\"exitDistMax\":25}]}`"]
    return "\n".join(L)


def call_claude(prompt, model, allow_edits, timeout, log):
    exe = claude_exe()
    if not exe:
        log("  [!] claude CLI 없음 — 교정 건너뜀")
        return False
    AGENT_REQ.write_text(prompt, encoding="utf-8")
    cmd = [exe, "-p", f"{AGENT_REQ} 파일을 읽고 거기 적힌 대로 처리해줘.",
           "--agent", "dev-fixer", "--model", model]
    if allow_edits:
        cmd += ["--permission-mode", "acceptEdits"]
    log(f"  → Claude 호출 (dev-fixer, {model}, {'수정허용' if allow_edits else '진단만'})")
    try:
        p = subprocess.run(cmd, cwd=str(REPO), capture_output=True, text=True,
                           encoding="utf-8", errors="replace", timeout=timeout)
        out = (p.stdout or "").strip()
        for line in out.splitlines()[-25:]:
            log(f"    {line}")
        return p.returncode == 0
    except subprocess.TimeoutExpired:
        log(f"  [!] Claude 타임아웃({timeout}s)")
        return False
    except Exception as e:
        log(f"  [!] Claude 실행 실패: {e}")
        return False


# ─────────────────────────────────────────────────────────────
#  루프
# ─────────────────────────────────────────────────────────────

def wait_idle(cfg, names, timeout, poll, log):
    """차일드가 런을 끝낼 때까지. (running → 아님)"""
    d = orch.data_dir(cfg)
    deadline = time.time() + timeout
    started = False
    while time.time() < deadline:
        rows = orch.status_rows(cfg, names)
        alive = [r for r in rows if r["alive"]]
        if not alive:
            time.sleep(poll)
            continue
        running = [r for r in alive if r["state"] == "running"]
        if running:
            started = True
        elif started:
            return True          # 시작했다가 멈췄다 = 끝
        time.sleep(poll)
    return False


def run_loop(args, log):
    cfg = orch.load_instances()
    d = orch.data_dir(cfg)
    stage_i = max(0, min(args.stage - 1, len(STAGES) - 1))
    prev = None
    fails_in_stage = 0

    log(f"데이터: {d}")
    log(f"단계 {stage_i + 1}/{len(STAGES)} '{STAGES[stage_i]['name']}' 부터 시작 · 최대 {args.rounds}라운드")
    log("※ 차일드가 명령 대기 모드여야 한다 — 에디터는 Play 후 F10, 빌드는 -qa-serve\n")

    before = newest_result(d)
    before_m = before.stat().st_mtime if before else 0

    for rnd in range(1, args.rounds + 1):
        stage = STAGES[stage_i]
        log(f"── 라운드 {rnd} · 단계 {stage_i + 1} '{stage['name']}' ──")

        res = orch.dispatch(cfg, args.scenario, args.instances, args.cycles, "",
                            f"autoloop r{rnd} stage{stage_i + 1}", vary_seed=True)
        if res.get("error"):
            log(f"  [!] 투입 실패: {res['error']}")
            return
        log(f"  투입: {', '.join(x['instance'] or '(기본)' for x in res['dispatched'])}")

        if not wait_idle(cfg, args.instances, args.timeout, 3.0, log):
            log("  [!] 차일드가 런을 시작/종료하지 않음 — 명령 대기 모드인지 확인(F10)")
            return

        newest = newest_result(d)
        if newest is None or newest.stat().st_mtime <= before_m:
            log("  [!] 새 결과 파일이 없음 — 건너뜀")
            time.sleep(2)
            continue
        before_m = newest.stat().st_mtime

        j = read_json(newest)
        if not j:
            log("  [!] 결과 파싱 실패")
            continue
        m = metrics_of(j)

        log(f"  결과: {m['verdict']} · 사이클 {m['cyclesDone']}/{m['cyclesPlanned']} · "
            f"발견 {m['discovered']}/{m['totalCrates']}({m['discoveryRate']}%) · "
            f"루팅 {m['lootValue']:.0f} · 탈출 {m['extracts']} · 오류 {m['errors']}")

        rec = {"at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"), "round": rnd,
               "stage": stage_i + 1, "stageName": stage["name"], "file": newest.name, **m}
        rec.pop("navGrids", None)
        try:
            with open(GROWTH, "a", encoding="utf-8") as fp:
                fp.write(json.dumps(rec, ensure_ascii=False) + "\n")
        except Exception:
            pass

        if stage["check"](m):
            log(f"  ✔ 단계 '{stage['name']}' 통과")
            stage_i += 1
            fails_in_stage = 0
            prev = m
            if stage_i >= len(STAGES):
                log("\n🎉 모든 단계 통과 — 여기서부터는 밸런스 튜닝 영역")
                return
            log(f"  → 다음 단계: {STAGES[stage_i]['name']} ({STAGES[stage_i]['goal']})")
            continue

        fails_in_stage += 1
        log(f"  ✘ 미통과 ({fails_in_stage}/{args.max_fails})")

        if fails_in_stage >= args.max_fails:
            log(f"  [!] 단계 '{stage['name']}'에서 {args.max_fails}회 연속 실패 — 사람 판단 필요. 중단")
            return

        if args.no_agent:
            log("  (--no-agent — 교정 없이 다음 라운드)")
        else:
            git_before = git_state()
            call_claude(build_prompt(stage, m, prev, d, rnd), args.model,
                        not args.diagnose_only, args.agent_timeout, log)

            # 권한 경계 검증 — 지시만으로는 못 믿는다
            touched = changed_between(git_before, git_state())
            mine, theirs = split_scope(touched)
            if mine:
                log(f"  수정(QA/AI): {', '.join(mine[:6])}" + (" …" if len(mine) > 6 else ""))
            if not touched:
                log("  변경 없음 — 에이전트가 고치지 않았다")

            if theirs:
                log("")
                log("  ⛔ 게임 코드가 수정됐다 — 사용자 승인 없이 건드리면 안 되는 범위:")
                for p in theirs:
                    log(f"       {p}")
                log(f"  → 루프를 세운다. 위 변경을 확인하고 유지할지 되돌릴지 사용자가 정해야 한다.")
                log(f"     (되돌리려면: git checkout -- {' '.join(theirs[:5])})")
                return

            if GAME_ISSUES.exists():
                try:
                    n = GAME_ISSUES.read_text(encoding='utf-8').count("\n## ")
                    log(f"  게임 결함 보고서 누적 {n}건 → {GAME_ISSUES}")
                except Exception:
                    pass
        prev = m

    log("\n최대 라운드 도달 — 종료")
    report_game_issues(log)


def report_game_issues(log):
    """루프가 스스로 고치지 않고 올린 게임 결함 — 사람이 판단할 것."""
    if not GAME_ISSUES.exists():
        return
    try:
        body = GAME_ISSUES.read_text(encoding="utf-8")
    except Exception:
        return
    titles = [ln.strip() for ln in body.splitlines() if ln.startswith("## ")]
    if not titles:
        return
    log("")
    log("═" * 60)
    log(f"  사람 판단이 필요한 **게임 결함** {len(titles)}건 — 루프는 손대지 않았다")
    log("═" * 60)
    for t in titles[-12:]:
        log(f"  {t[3:]}")
    log(f"\n  전문: {GAME_ISSUES}")


def main():
    for s in (sys.stdout, sys.stderr):
        try:
            s.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass

    ap = argparse.ArgumentParser(description="QA 자동 성장 루프")
    ap.add_argument("--rounds", type=int, default=10)
    ap.add_argument("--stage", type=int, default=1, help="시작 단계(1~5)")
    ap.add_argument("--scenario", default="default")
    ap.add_argument("--instances", default="")
    ap.add_argument("--cycles", type=int, default=0)
    ap.add_argument("--timeout", type=float, default=900, help="한 런 대기 상한(초)")
    ap.add_argument("--max-fails", type=int, default=3, help="한 단계 연속 실패 상한")
    ap.add_argument("--model", default="opus")
    ap.add_argument("--agent-timeout", type=float, default=900)
    ap.add_argument("--no-agent", action="store_true", help="Claude 호출 없이 관찰만")
    ap.add_argument("--diagnose-only", action="store_true", help="Claude가 진단만(파일 수정 금지)")
    args = ap.parse_args()

    def log(msg):
        print(msg, flush=True)

    try:
        run_loop(args, log)
    except KeyboardInterrupt:
        log("\n사용자 중단")


if __name__ == "__main__":
    main()
