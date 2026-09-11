#!/usr/bin/env python3
"""
QA 총괄 시스템 (Orchestrator) — 여러 QA 인스턴스를 한 곳에서 지휘한다.

지금은 Unity 에디터 1개지만, 나중엔 빌드 exe를 N개 띄워 병렬로 돌린다.
그 둘을 같은 인터페이스로 다루는 게 이 파일의 목적이다.

  Claude(에이전트) ──CLI──▶ orchestrator ──파일──▶ QA 인스턴스 N개(에디터/빌드)
                    ◀─JSON──              ◀──────

의존성 없음(파이썬 표준 라이브러리만). 게임·Unity와 독립적으로 돈다.

사용법
------
  python tools/qa_orchestrator.py status                  # 모든 인스턴스 상태
  python tools/qa_orchestrator.py launch --instance B     # 빌드 인스턴스 기동
  python tools/qa_orchestrator.py run --scenario tutorial --instances A,B
  python tools/qa_orchestrator.py wait --timeout 600      # 전부 끝날 때까지
  python tools/qa_orchestrator.py collect                 # 결과 집계(PASS/FAIL)
  python tools/qa_orchestrator.py resume --action retry --note "고쳤음"
  python tools/qa_orchestrator.py kill --instance B

  아무 명령에나 --json 을 붙이면 기계 판독용 출력(에이전트가 이걸 읽는다).

인스턴스 등록: tools/qa-instances.json (없으면 기본값 자동 생성)
"""

import argparse
import json
import os
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
INSTANCES_FILE = HERE / "qa-instances.json"
MANIFEST_FILE = HERE / "qa-manifest.json"

DEFAULT_COMPANY = "Studio Pod Games"
DEFAULT_PRODUCT = "demo13-flashlight"


# ─────────────────────────────────────────────────────────────
#  경로 / 설정
# ─────────────────────────────────────────────────────────────

def default_data_dir() -> Path:
    """Unity Application.persistentDataPath."""
    base = Path(os.environ.get("USERPROFILE", Path.home()))
    return base / "AppData" / "LocalLow" / DEFAULT_COMPANY / DEFAULT_PRODUCT


def load_instances() -> dict:
    """인스턴스 레지스트리. 없으면 에디터 1개짜리 기본본을 만들어 둔다."""
    if INSTANCES_FILE.exists():
        try:
            return json.loads(INSTANCES_FILE.read_text(encoding="utf-8"))
        except Exception as e:
            print(f"[orch] 인스턴스 파일 파싱 실패: {e}", file=sys.stderr)

    cfg = {
        "_note": "QA 인스턴스 레지스트리. kind=editor는 사람이 Unity에서 F9로 띄우는 것(자동 기동 불가), "
                 "kind=build는 exePath를 orchestrator가 직접 실행한다. "
                 "instance 이름이 곧 파일 접두(A-qa-command.json). 빈 문자열은 접두 없음(에디터 기본).",
        "dataDir": str(default_data_dir()),
        "instances": [
            {
                "name": "", "kind": "editor", "label": "Unity 에디터 (F9 수동)",
                "exePath": "", "extraArgs": []
            },
            {
                "name": "A", "kind": "build", "label": "빌드 A",
                "exePath": "", "extraArgs": ["-qa-serve", "-qa-minutes=10"]
            }
        ]
    }
    INSTANCES_FILE.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[orch] 기본 인스턴스 레지스트리 생성: {INSTANCES_FILE}")
    return cfg


def data_dir(cfg) -> Path:
    """QA 데이터 폴더. 등록된 경로에 사용자 이름이 박혀 있어 **다른 PC에서는 안 맞는다**
    (집/회사 왕복). 설정 경로가 없고 이 PC의 기본 경로가 있으면 그쪽을 쓴다."""
    configured = cfg.get("dataDir")
    if configured:
        p = Path(configured)
        if p.exists():
            return p
        fallback = default_data_dir()
        if fallback.exists():
            return fallback
        return p
    return default_data_dir()


def fname(inst: str, base: str) -> str:
    """인스턴스 접두 규칙 — QaBot.F()와 반드시 같아야 한다."""
    return base if not inst else f"{inst}-{base}"


def pick(cfg, names):
    """--instances 필터. 미지정이면 전부."""
    all_i = cfg.get("instances", [])
    if not names:
        return all_i
    want = [n.strip() for n in names.split(",")]
    out = []
    for i in all_i:
        if i.get("name", "") in want or i.get("label", "") in want:
            out.append(i)
    return out


# ─────────────────────────────────────────────────────────────
#  상태
# ─────────────────────────────────────────────────────────────

def read_json(p: Path):
    try:
        if p.exists():
            return json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        pass
    return None


def status_rows(cfg, names=""):
    """인스턴스별 현재 상태. CLI와 GUI가 같은 함수를 본다."""
    d = data_dir(cfg)
    rows = []
    for inst in pick(cfg, names):
        name = inst.get("name", "")
        sp = d / fname(name, "qa-status.json")
        s = read_json(sp)
        age = None
        if sp.exists():
            age = round(time.time() - sp.stat().st_mtime, 1)
        bp = d / fname(name, "qa-blocked.json")
        alive = bool(s) and age is not None and age < 10
        # 막힘 요청은 '살아서 대기 중'일 때만 유효하다. 런이 끝났는데 파일이 남아 있으면
        # 잔재다(대기 중 종료되면 QaBridge가 지울 기회를 못 얻는다) — 배너를 띄우면 안 된다.
        blocked_live = bp.exists() and alive and (s or {}).get("state") == "running"
        rows.append({
            "instance": name or "(기본)",
            "name": name,
            "label": inst.get("label", ""),
            "kind": inst.get("kind", ""),
            "exePath": inst.get("exePath", ""),
            "extraArgs": inst.get("extraArgs", []),
            "alive": alive,
            "ageSec": age,
            "state": (s or {}).get("state"),
            "step": (s or {}).get("step"),
            "cycle": (s or {}).get("cycle"),
            "scene": (s or {}).get("scene"),
            "elapsedSec": round((s or {}).get("elapsedSec", 0), 1),
            "errors": (s or {}).get("errors"),
            "warns": (s or {}).get("warns"),
            "blocked": blocked_live,
            "blockedStale": bp.exists() and not blocked_live,
            "blockedInfo": read_json(bp) if bp.exists() else None,
            "recent": (s or {}).get("recent", []),
        })
    return rows


def cmd_status(cfg, args):
    d = data_dir(cfg)
    rows = status_rows(cfg, args.instances)

    if args.json:
        out(rows)
        return 0

    print(f"데이터 폴더: {d}")
    print(f"{'인스턴스':<10}{'종류':<8}{'상태':<10}{'단계':<18}{'씬':<14}{'경과':>7}{'E/W':>8}  비고")
    for r in rows:
        mark = "●" if r["alive"] else "○"
        note = "⛔막힘" if r["blocked"] else ("" if r["alive"] else f"끊김 {r['ageSec']}s" if r["ageSec"] else "미기동")
        print(f"{mark}{r['instance']:<9}{r['kind']:<8}{str(r['state'] or '-'):<10}"
              f"{str(r['step'] or '-'):<18}{str(r['scene'] or '-'):<14}"
              f"{r['elapsedSec']:>6}s{str(r['errors'])+'/'+str(r['warns']):>8}  {note}")
        for line in (r.get("recent") or [])[:2]:
            print(f"     └ {line}")
    return 0


# ─────────────────────────────────────────────────────────────
#  기동 / 종료
# ─────────────────────────────────────────────────────────────

def launch_instances(cfg, names="", minutes=0.0):
    launched, skipped = [], []
    for inst in pick(cfg, names):
        name, kind = inst.get("name", ""), inst.get("kind", "")
        if kind != "build":
            skipped.append({"instance": name, "why": "kind!=build — 에디터는 사람이 F9로 띄운다"})
            continue
        exe = inst.get("exePath", "")
        if not exe or not Path(exe).exists():
            skipped.append({"instance": name, "why": f"exePath 없음/존재하지 않음: {exe or '(미설정)'}"})
            continue

        cmdline = [exe] + list(inst.get("extraArgs", []))
        if name:
            cmdline.append(f"-qa-instance={name}")
        if minutes:
            cmdline.append(f"-qa-minutes={minutes}")
        try:
            p = subprocess.Popen(cmdline, cwd=str(Path(exe).parent))
            launched.append({"instance": name, "pid": p.pid, "cmd": cmdline})
        except Exception as e:
            skipped.append({"instance": name, "why": f"실행 실패: {e}"})
    return {"launched": launched, "skipped": skipped}


def cmd_launch(cfg, args):
    res = launch_instances(cfg, args.instances, args.minutes)
    launched, skipped = res["launched"], res["skipped"]
    if args.json:
        out(res)
    else:
        for l in launched:
            print(f"[기동] {l['instance'] or '(기본)'} pid={l['pid']}")
        for s in skipped:
            print(f"[스킵] {s['instance'] or '(기본)'} — {s['why']}")
    return 0


def kill_instances(cfg, names=""):
    """빌드 인스턴스 종료(윈도우: taskkill로 이미지명 매칭)."""
    killed = []
    for inst in pick(cfg, names):
        exe = inst.get("exePath", "")
        if inst.get("kind") != "build" or not exe:
            continue
        image = Path(exe).name
        try:
            subprocess.run(["taskkill", "/IM", image, "/F"], capture_output=True)
            killed.append(inst.get("name", ""))
        except Exception as e:
            print(f"[orch] 종료 실패 {image}: {e}", file=sys.stderr)
    return killed


def cmd_kill(cfg, args):
    killed = kill_instances(cfg, args.instances)
    if args.json:
        out({"killed": killed})
    else:
        print(f"종료: {killed or '없음'}")
    return 0


# ─────────────────────────────────────────────────────────────
#  명령 투입
# ─────────────────────────────────────────────────────────────

def load_scenario(name: str):
    """시나리오 이름 → Resources/QA/qa-scenario[-name].json"""
    qa_dir = REPO / "demo13-flashlight" / "Assets" / "Resources" / "QA"
    cands = [qa_dir / f"qa-scenario-{name}.json", qa_dir / f"{name}.json", Path(name)]
    if name in ("default", "standard", "기본"):
        cands.insert(0, qa_dir / "qa-scenario.json")
    for c in cands:
        if c.exists():
            return json.loads(c.read_text(encoding="utf-8")), str(c)
    return None, None


def list_scenarios():
    """Resources/QA 안의 시나리오 파일 목록 → [{name, file, label, steps, cycles}]"""
    qa_dir = REPO / "demo13-flashlight" / "Assets" / "Resources" / "QA"
    found = []
    if not qa_dir.exists():
        return found
    for p in sorted(qa_dir.glob("qa-scenario*.json")):
        stem = p.stem  # qa-scenario / qa-scenario-tutorial
        name = "default" if stem == "qa-scenario" else stem.replace("qa-scenario-", "")
        j = read_json(p) or {}
        found.append({
            "name": name,
            "file": str(p),
            "label": j.get("name") or name,
            "steps": len(j.get("steps", [])),
            "cycles": j.get("cycles", 1),
        })
    return found


def dispatch(cfg, scenario_name="default", names="", cycles=0, run_id="", note="", vary_seed=False):
    """시나리오를 대상 인스턴스의 명령 파일로 투입."""
    d = data_dir(cfg)
    d.mkdir(parents=True, exist_ok=True)

    scenario, src = load_scenario(scenario_name)
    if scenario is None:
        return {"error": f"시나리오 '{scenario_name}'를 못 찾음 (Resources/QA/ 확인)"}

    if cycles:
        scenario["cycles"] = cycles

    dispatched = []
    stamp = datetime.now().strftime("%H%M%S")
    for inst in pick(cfg, names):
        name = inst.get("name", "")
        # 인스턴스마다 시드를 흔들어 같은 시나리오라도 다른 플레이가 되게(병렬의 의미)
        sc = json.loads(json.dumps(scenario))
        if vary_seed and name:
            sc["seed"] = int(sc.get("seed", 0)) + sum(ord(ch) for ch in name)

        cmd = {
            "id": f"{run_id or stamp}{('-' + name) if name else ''}",
            "note": note or f"orchestrator 투입 · 시나리오={scenario_name}",
            "scenario": sc,
        }
        p = d / fname(name, "qa-command.json")
        p.write_text(json.dumps(cmd, indent=2, ensure_ascii=False), encoding="utf-8")
        dispatched.append({"instance": name, "file": str(p), "id": cmd["id"], "seed": sc.get("seed")})

    return {"scenario": scenario_name, "source": src, "dispatched": dispatched}


def cmd_run(cfg, args):
    res = dispatch(cfg, args.scenario, args.instances, args.cycles,
                   args.id, args.note, args.vary_seed)
    if res.get("error"):
        out(res) if args.json else print(f"[orch] {res['error']}", file=sys.stderr)
        return 2
    src, dispatched = res["source"], res["dispatched"]
    if args.json:
        out(res)
    else:
        print(f"시나리오: {args.scenario}  ({src})")
        for x in dispatched:
            print(f"  → {x['instance'] or '(기본)'}  id={x['id']} seed={x['seed']}")
        print("\n※ 인스턴스가 -qa-serve 로 떠 있어야 명령을 집어간다. 에디터(F9)는 명령 대기 모드가 아니다.")
    return 0


def write_resume(cfg, names="", action="skip", note="", force=False):
    """막힘 응답 — Claude가 진단 후 지시."""
    d = data_dir(cfg)
    wrote = []
    for inst in pick(cfg, names):
        name = inst.get("name", "")
        if not (d / fname(name, "qa-blocked.json")).exists() and not force:
            continue
        p = d / fname(name, "qa-resume.json")
        p.write_text(json.dumps({"action": action, "note": note or ""},
                                indent=2, ensure_ascii=False), encoding="utf-8")
        wrote.append({"instance": name, "file": str(p), "action": action})
    return wrote


def clear_stale(cfg, names=""):
    """끝난 런이 남긴 잔재(막힘 요청·집어가지 않은 명령) 제거."""
    d = data_dir(cfg)
    removed = []
    for row in status_rows(cfg, names):
        name = row["name"]
        if row["blockedStale"]:
            p = d / fname(name, "qa-blocked.json")
            try:
                p.unlink()
                removed.append(p.name)
            except Exception:
                pass
        if not row["alive"]:
            p = d / fname(name, "qa-command.json")
            if p.exists():
                try:
                    p.unlink()
                    removed.append(p.name)
                except Exception:
                    pass
    return removed


def cmd_clear(cfg, args):
    removed = clear_stale(cfg, args.instances)
    out({"removed": removed}) if args.json else print(f"정리: {removed or '없음'}")
    return 0


def cmd_resume(cfg, args):
    wrote = write_resume(cfg, args.instances, args.action, args.note, args.force)
    if args.json:
        out({"resumed": wrote})
    else:
        print(f"응답 전송: {[w['instance'] or '(기본)' for w in wrote] or '막힌 인스턴스 없음'}")
    return 0


# ─────────────────────────────────────────────────────────────
#  대기 / 수집
# ─────────────────────────────────────────────────────────────

def cmd_wait(cfg, args):
    """모든 대상 인스턴스가 idle/done 이 될 때까지. 막히면 즉시 반환."""
    d = data_dir(cfg)
    targets = pick(cfg, args.instances)
    deadline = time.time() + args.timeout
    while time.time() < deadline:
        running, blocked = [], []
        for inst in targets:
            name = inst.get("name", "")
            sp = d / fname(name, "qa-status.json")
            s = read_json(sp) or {}
            fresh = sp.exists() and (time.time() - sp.stat().st_mtime) < 10
            if (d / fname(name, "qa-blocked.json")).exists():
                blocked.append(name)
            elif fresh and s.get("state") == "running":
                running.append(name)

        if blocked:
            res = {"result": "blocked", "instances": blocked}
            out(res) if args.json else print(f"⛔ 막힘: {blocked} — collect 로 상세 확인")
            return 3
        if not running:
            res = {"result": "idle", "waitedSec": round(args.timeout - (deadline - time.time()), 1)}
            out(res) if args.json else print("모든 인스턴스 대기 상태(런 종료)")
            return 0
        if not args.json:
            print(f"  실행 중: {running} …", flush=True)
        time.sleep(args.poll)

    out({"result": "timeout"}) if args.json else print("타임아웃")
    return 4


def collect_summary(cfg, limit=20):
    """결과 파일 집계 — PASS/FAIL 통계 + 이상 상위 + 공간 문제 지점."""
    d = data_dir(cfg)
    runs = []
    seen = set()
    # latest 사본은 타임스탬프본과 같은 런이다. 무조건 건너뛰면 F9 수동 런(commandId 없음 →
    # latest만 남는다)을 통째로 놓치므로, 버리지 말고 (차일드,시작시각)으로 중복만 제거한다.
    # 정렬 2차 키: 같은 시각이면 이름이 고정된 latest보다 타임스탬프본을 택한다.
    for p in sorted(d.glob("*qa-result-*.json"),
                    key=lambda x: (x.stat().st_mtime, "latest" not in x.name), reverse=True):
        j = read_json(p)
        if not j:
            continue
        key = (j.get("instance", ""), j.get("startedAt", ""), j.get("commandId", ""))
        if key in seen:
            continue
        seen.add(key)
        runs.append((p, j))
        if len(runs) >= limit:
            break

    summary = {
        "dataDir": str(d),
        "runCount": len(runs),
        "pass": sum(1 for _, j in runs if j.get("verdict") == "PASS"),
        "fail": sum(1 for _, j in runs if j.get("verdict") == "FAIL"),
        "runs": [],
        "topAnomalies": {},
        "problemSpots": [],
        "failedChecks": {},
    }

    for p, j in runs:
        summary["runs"].append({
            "file": p.name,
            "at": datetime.fromtimestamp(p.stat().st_mtime).strftime("%m-%d %H:%M"),
            "instance": j.get("instance", ""),
            "scenario": j.get("scenario"),
            "verdict": j.get("verdict"),
            "reason": j.get("verdictReason"),
            "cycles": f'{j.get("cyclesCompleted")}/{j.get("cyclesPlanned")}',
            "errors": j.get("errorCount"),
            "warns": j.get("warnCount"),
            "durationSec": round(j.get("durationSec", 0), 1),
            "checks": j.get("checks", []),
            "anomalies": j.get("anomalies", []),
            "navGrids": j.get("navGrids", []),
            "path": str(p),
        })
        for a in j.get("anomalies", []):
            k = a.get("kind", "?")
            summary["topAnomalies"][k] = summary["topAnomalies"].get(k, 0) + 1
        for c in j.get("checks", []):
            if not c.get("passed"):
                n = c.get("name", "?")
                summary["failedChecks"][n] = summary["failedChecks"].get(n, 0) + 1
        for cell in j.get("cells", []):
            if cell.get("stuck") or cell.get("unreachable") or cell.get("deaths"):
                summary["problemSpots"].append({
                    "scene": cell.get("scene"), "x": round(cell.get("x", 0)), "y": round(cell.get("y", 0)),
                    "stuck": cell.get("stuck"), "unreachable": cell.get("unreachable"), "deaths": cell.get("deaths"),
                })

    summary["topAnomalies"] = dict(sorted(summary["topAnomalies"].items(), key=lambda kv: -kv[1]))
    summary["failedChecks"] = dict(sorted(summary["failedChecks"].items(), key=lambda kv: -kv[1]))
    summary["problemSpots"] = summary["problemSpots"][:30]
    return summary


def cmd_collect(cfg, args):
    summary = collect_summary(cfg, args.limit)
    d = summary["dataDir"]

    if args.json:
        out(summary)
        return 0

    print(f"데이터: {d}")
    print(f"런 {summary['runCount']}  ·  통과 {summary['pass']}  ·  미통과 {summary['fail']}")
    if summary["runs"]:
        print(f"\n{'시각':<13}{'인스턴스':<10}{'판정':<7}{'사이클':<8}{'E/W':<8}{'시간':>7}  사유")
        for r in summary["runs"]:
            print(f"{r['at']:<13}{(r['instance'] or '-'):<10}{r['verdict']:<7}{r['cycles']:<8}"
                  f"{str(r['errors'])+'/'+str(r['warns']):<8}{r['durationSec']:>6}s  {(r['reason'] or '')[:60]}")
    if summary["failedChecks"]:
        print("\n실패한 판정 항목:")
        for k, v in summary["failedChecks"].items():
            print(f"  {k:<20} {v}회")
    if summary["topAnomalies"]:
        print("\n이상 빈도:")
        for k, v in summary["topAnomalies"].items():
            print(f"  {k:<22} {v}")
    if summary["problemSpots"]:
        print("\n문제 지점(스턱/길막힘/사망):")
        for s in summary["problemSpots"][:12]:
            print(f"  {s['scene']:<12} ({s['x']:>4},{s['y']:>4})  스턱{s['stuck']} 막힘{s['unreachable']} 사망{s['deaths']}")
    return 0


def coverage_summary():
    """QA가 무엇을 아는지 — 매니페스트 요약(사각 지대 확인용)."""
    m = read_json(MANIFEST_FILE)
    if not m:
        return {"error": "qa-manifest.json 없음"}
    cov = m.get("coverage", [])
    by = {}
    for c in cov:
        by.setdefault(c.get("status", "?"), []).append(c)
    return {
        "total": len(cov),
        "counts": {k: len(v) for k, v in by.items()},
        "blind": [c["system"] for c in by.get("blind", [])],
        "ops": [o["op"] for o in m.get("ops", [])],
        "rows": cov,
        "checks": m.get("checks", []),
        "limits": m.get("limits", []),
        "opRows": m.get("ops", []),
    }


def cmd_coverage(cfg, args):
    res = coverage_summary()
    if res.get("error"):
        out(res) if args.json else print("매니페스트 없음")
        return 2
    if args.json:
        out(res)
    else:
        print(f"게임 시스템 {res['total']}개")
        for k, v in res["counts"].items():
            print(f"  {k:<10} {v}")
        if res["blind"]:
            print("\n사각(op 없음):")
            for s in res["blind"]:
                print(f"  - {s}")
    return 0


# ─────────────────────────────────────────────────────────────

def out(obj):
    print(json.dumps(obj, indent=2, ensure_ascii=False))


def main():
    # 윈도우 콘솔 기본이 cp949 → 한글/기호(⛔ ●) 출력에서 죽는다. utf-8로 강제.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass

    ap = argparse.ArgumentParser(description="QA 총괄 시스템")
    ap.add_argument("--json", action="store_true", help="기계 판독용 JSON 출력(에이전트용)")
    sub = ap.add_subparsers(dest="cmd", required=True)

    def common(p):
        p.add_argument("--instances", default="", help="쉼표 구분(미지정=전부)")
        return p

    common(sub.add_parser("status", help="인스턴스 상태"))

    p = common(sub.add_parser("launch", help="빌드 인스턴스 기동"))
    p.add_argument("--minutes", type=float, default=0)

    common(sub.add_parser("kill", help="빌드 인스턴스 종료"))

    p = common(sub.add_parser("run", help="시나리오 투입"))
    p.add_argument("--scenario", default="default", help="default | tutorial | 파일경로")
    p.add_argument("--cycles", type=int, default=0)
    p.add_argument("--id", default="")
    p.add_argument("--note", default="")
    p.add_argument("--vary-seed", action="store_true", help="인스턴스별로 시드를 다르게(병렬 다양성)")

    p = common(sub.add_parser("resume", help="막힘 응답"))
    p.add_argument("--action", default="skip", choices=["retry", "skip", "abort"])
    p.add_argument("--note", default="")
    p.add_argument("--force", action="store_true")

    p = common(sub.add_parser("wait", help="런 종료 대기"))
    p.add_argument("--timeout", type=float, default=900)
    p.add_argument("--poll", type=float, default=3)

    p = common(sub.add_parser("collect", help="결과 집계"))
    p.add_argument("--limit", type=int, default=20)

    common(sub.add_parser("coverage", help="커버리지/사각 요약"))

    common(sub.add_parser("clear", help="끝난 런의 잔재(막힘·미소비 명령) 정리"))

    args = ap.parse_args()
    cfg = load_instances()

    fn = {
        "status": cmd_status, "launch": cmd_launch, "kill": cmd_kill, "run": cmd_run,
        "resume": cmd_resume, "wait": cmd_wait, "collect": cmd_collect, "coverage": cmd_coverage,
        "clear": cmd_clear,
    }[args.cmd]
    return fn(cfg, args)


if __name__ == "__main__":
    sys.exit(main())
