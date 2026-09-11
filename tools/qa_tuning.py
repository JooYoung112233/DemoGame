#!/usr/bin/env python3
"""
밸런스 값 읽기/쓰기 — 마더 GUI의 밸런스 탭이 쓴다.

게임의 수치는 두 에셋에 나뉘어 있다.
  • GameTuning.asset  — 게임 전역 노브(루팅·소음·시야·무게…)      메타: GameTuning.cs
  • StatDB.asset      — 플레이어 스탯(이속·스태미너·전투)          메타: PlayerStatData.cs
                        + 적 유닛 스탯(유닛별 HP·공격·이속…)       메타: UnitStatData.cs

셋을 **소스(source)** 로 묶어 같은 인터페이스로 다룬다. 어느 쪽이든
`[Header]`/`[Tooltip]`/`[Range]`/기본값을 .cs에서 읽어 현재 값과 짝지어 보여준다.

쓰기는 **해당 줄의 값만 치환**한다. YAML 전체를 다시 쓰지 않으므로 Unity 메타·서식이
그대로 보존되고, 병렬 세션이 다른 필드를 고쳐도 충돌 범위가 최소가 된다.
"""

import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
GAME = REPO / "demo13-flashlight" / "Assets"

CS_TUNING = GAME / "Scripts" / "Systems" / "GameTuning.cs"
CS_PLAYER = GAME / "Scripts" / "Data" / "PlayerStatData.cs"
CS_UNIT = GAME / "Scripts" / "Data" / "UnitStatData.cs"
ASSET_TUNING = GAME / "Resources" / "Data" / "GameTuning.asset"
ASSET_STATDB = GAME / "Resources" / "Data" / "StatDB.asset"

_HEADER = re.compile(r'\[Header\("([^"]*)"\)\]')
_RANGE = re.compile(r'\[Range\(\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?\s*\)\]')
_FIELD = re.compile(r'public\s+(float|int|bool)\s+(\w+)\s*=\s*([^;]+);')
_STR = re.compile(r'"((?:[^"\\]|\\.)*)"')


def _num(s):
    s = s.strip().rstrip("f").strip()
    try:
        return float(s)
    except ValueError:
        return None


def _read(p):
    """(줄 목록, 원본 줄바꿈). read_text는 universal-newline이라 CRLF를 \\n으로 바꿔
    읽어버린다 — 줄바꿈 판별은 반드시 **원본 바이트**로 해야 한다.
    (이걸 틀리면 저장할 때 파일 전체 줄끝이 뒤집혀 통짜 diff가 난다)"""
    raw = Path(p).read_bytes()
    nl = "\r\n" if b"\r\n" in raw else "\n"
    return raw.decode("utf-8", errors="replace").replace("\r\n", "\n").split("\n"), nl


def _lines(p):
    return _read(p)[0]


# ─────────────────────────────────────────────────────────────
#  소스 정의
# ─────────────────────────────────────────────────────────────

def unit_ids():
    """StatDB.asset의 유닛 id 목록."""
    if not ASSET_STATDB.exists():
        return []
    out = []
    for line in _lines(ASSET_STATDB):
        m = re.match(r"^  - id: (\S+)$", line)
        if m:
            out.append(m.group(1))
    return out


def list_sources():
    """밸런스 탭의 소스 선택 목록."""
    out = [
        {"key": "gametuning", "label": "GameTuning — 게임 전역 노브",
         "cs": CS_TUNING, "asset": ASSET_TUNING, "kind": "flat"},
        {"key": "player", "label": "플레이어 스탯 — 이속·스태미너·전투",
         "cs": CS_PLAYER, "asset": ASSET_STATDB, "kind": "block", "block": "playerStat"},
    ]
    for uid in unit_ids():
        out.append({"key": f"unit:{uid}", "label": f"적 유닛 — {uid}",
                    "cs": CS_UNIT, "asset": ASSET_STATDB, "kind": "unit", "unit": uid})
    return out


def find_source(key):
    for s in list_sources():
        if s["key"] == key:
            return s
    return list_sources()[0]


# ─────────────────────────────────────────────────────────────
#  메타데이터(.cs) — [Header]/[Tooltip]/[Range]/기본값
# ─────────────────────────────────────────────────────────────

def parse_fields(cs_path):
    """[{name,type,group,tooltip,min,max,default}] (선언 순서 유지)"""
    if not Path(cs_path).exists():
        return []
    lines = _lines(Path(cs_path))

    out, group, tooltip, rng = [], "기타", "", None
    i = 0
    while i < len(lines):
        line = lines[i]

        m = _HEADER.search(line)
        if m:
            group = m.group(1)
            i += 1
            continue

        if "[Tooltip(" in line:
            # 여러 줄 + 문자열 이어붙이기(" ... " + " ... ")를 모두 모은다
            chunk = line
            while ")]" not in chunk and i + 1 < len(lines):
                i += 1
                chunk += "\n" + lines[i]
            tooltip = " ".join(s.replace("\\n", " ").replace('\\"', '"')
                               for s in _STR.findall(chunk)).strip()
            i += 1
            continue

        m = _RANGE.search(line)
        if m:
            rng = (_num(m.group(1)), _num(m.group(2)))   # 같은 줄에 필드가 이어질 수 있다

        m = _FIELD.search(line)
        if m:
            typ, name, dflt = m.group(1), m.group(2), m.group(3).strip()
            default = (1 if dflt == "true" else 0) if typ == "bool" else _num(dflt)
            out.append({"name": name, "type": typ, "group": group, "tooltip": tooltip,
                        "min": rng[0] if rng else None, "max": rng[1] if rng else None,
                        "default": default})
            tooltip, rng = "", None

        i += 1
    return out


# ─────────────────────────────────────────────────────────────
#  현재 값(.asset)
# ─────────────────────────────────────────────────────────────

def _span(src, lines):
    """소스가 차지하는 줄 구간 (start, end, indent)."""
    if src["kind"] == "flat":
        for n, line in enumerate(lines):
            if "m_EditorClassIdentifier" in line:
                return n + 1, len(lines), 2
        return 0, len(lines), 2

    if src["kind"] == "block":
        head = f"  {src['block']}:"
        for n, line in enumerate(lines):
            if line == head:
                end = n + 1
                while end < len(lines) and (lines[end].startswith("    ") or not lines[end].strip()):
                    end += 1
                return n + 1, end, 4
        return 0, 0, 4

    # kind == "unit": "  - id: X" 부터 다음 "  - " 전까지
    head = f"  - id: {src['unit']}"
    for n, line in enumerate(lines):
        if line == head:
            end = n + 1
            while end < len(lines) and lines[end].startswith("    "):
                end += 1
            return n, end, 4
    return 0, 0, 4


def parse_values(src):
    """({name: 문자열값}, {name: 줄번호}) — 스칼라만(색·참조 {..}는 제외)."""
    asset = Path(src["asset"])
    if not asset.exists():
        return {}, {}
    lines = _lines(asset)
    start, end, indent = _span(src, lines)

    pat = re.compile(r"^ {%d}(\w+): (.*)$" % indent)
    vals, at = {}, {}
    for n in range(start, min(end, len(lines))):
        line = lines[n]
        if src["kind"] == "unit" and line.startswith("  - id: "):
            vals["id"] = line.split(": ", 1)[1]
            continue
        m = pat.match(line)
        if not m:
            continue
        v = m.group(2).strip()
        if v.startswith("{") or v == "":      # 색·오브젝트 참조·중첩 블록은 건너뜀
            continue
        vals[m.group(1)] = v
        at[m.group(1)] = n
    return vals, at


def load(source_key="gametuning"):
    """메타 + 현재값을 합친 목록. GUI가 이걸 그대로 표시한다."""
    src = find_source(source_key)
    fields = parse_fields(src["cs"])
    vals, _ = parse_values(src)
    for f in fields:
        raw = vals.get(f["name"])
        f["missing"] = raw is None            # 에셋에 아직 안 써진 필드(= 기본값 사용 중)
        if raw is None:
            f["value"] = f["default"]
        elif f["type"] == "bool":
            f["value"] = 1 if raw in ("1", "true", "True") else 0
        else:
            f["value"] = _num(raw)
        f["changed"] = (f["value"] != f["default"])
    return fields


def fmt(field, value):
    """Unity YAML 표기 — int/bool은 정수, float는 불필요한 소수 제거."""
    if field["type"] in ("int", "bool"):
        return str(int(round(float(value))))
    v = float(value)
    return str(int(v)) if v == int(v) else repr(round(v, 6))


def save(source_key, changes):
    """{name: value} 반영. 해당 줄만 치환하고, 없던 필드는 구간 끝에 추가.
    반환: (적용된 항목, 경고 목록)"""
    src = find_source(source_key)
    asset = Path(src["asset"])
    if not asset.exists():
        return [], [f"에셋 없음: {asset}"]

    fields = {f["name"]: f for f in parse_fields(src["cs"])}
    lines, newline = _read(asset)

    # 파일 끝 빈 줄은 떼어냈다가 마지막에 개행 하나로 복원한다.
    # (그냥 append하면 빈 줄이 끼고 마지막 개행이 사라져 Unity 서식이 깨진다)
    trailing = 0
    while lines and lines[-1].strip() == "":
        lines.pop()
        trailing += 1

    _, at = parse_values(src)
    _, end, indent = _span(src, lines)
    applied, warns, added = [], [], []

    for name, value in changes.items():
        f = fields.get(name)
        if f is None:
            warns.append(f"{name}: {Path(src['cs']).name}에 없는 필드 — 건너뜀")
            continue
        s = fmt(f, value)
        if name in at:
            lines[at[name]] = f"{' ' * indent}{name}: {s}"
        else:
            added.append(f"{' ' * indent}{name}: {s}")
        applied.append(f"{name} = {s}")

    if added:
        # 파일 끝이 아니라 **이 소스 구간의 끝**에 넣는다 — 남의 블록으로 새지 않게
        pos = min(end, len(lines)) if end else max(at.values()) + 1 if at else len(lines)
        lines[pos:pos] = added

    # newline="" 필수 — 기본 텍스트 모드는 윈도우에서 \n을 전부 \r\n으로 바꿔버린다.
    # StatDB.asset은 LF라 그대로 쓰면 265줄 전체가 바뀐 것처럼 보인다.
    with open(asset, "w", encoding="utf-8", newline="") as fp:
        fp.write(newline.join(lines) + newline)
    return applied, warns


if __name__ == "__main__":
    import sys
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    for src in list_sources():
        fs = load(src["key"])
        vals, _ = parse_values(src)
        hit = [f for f in fs if not f["missing"]]
        print(f"\n═══ {src['label']}  ({len(hit)}/{len(fs)} 필드가 에셋에 존재) ═══")
        g = None
        for f in hit[:12]:
            if f["group"] != g:
                g = f["group"]
                print(f"  ── {g} ──")
            rng = f"[{f['min']:g}~{f['max']:g}]" if f["min"] is not None else ""
            mark = "*" if f["changed"] else " "
            print(f"  {mark}{f['name']:<26}{str(f['value']):>9}  기본 {str(f['default']):>8} {rng}")
        if len(hit) > 12:
            print(f"     … 외 {len(hit) - 12}개")
