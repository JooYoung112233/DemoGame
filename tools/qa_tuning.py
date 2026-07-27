#!/usr/bin/env python3
"""
GameTuning 읽기/쓰기 — 마더 GUI의 밸런스 탭이 쓴다.

두 파일을 짝지어 본다.
  • Assets/Scripts/Systems/GameTuning.cs   — 메타데이터([Header]/[Tooltip]/[Range]/기본값)
  • Assets/Resources/Data/GameTuning.asset — 현재 값(Unity YAML)

쓰기는 **해당 줄의 값만 치환**한다. YAML 전체를 다시 쓰지 않으므로 Unity 메타·서식이
그대로 보존되고, 병렬 세션이 다른 필드를 고쳐도 충돌 범위가 최소가 된다.
"""

import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
CS = REPO / "demo13-flashlight" / "Assets" / "Scripts" / "Systems" / "GameTuning.cs"
ASSET = REPO / "demo13-flashlight" / "Assets" / "Resources" / "Data" / "GameTuning.asset"

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


def parse_fields():
    """GameTuning.cs → [{name,type,group,tooltip,min,max,default}] (선언 순서 유지)"""
    if not CS.exists():
        return []
    lines = CS.read_text(encoding="utf-8", errors="replace").splitlines()

    out = []
    group = "기타"
    tooltip = ""
    rng = None
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
            # 같은 줄에 필드가 이어 붙는 경우가 없으므로 다음 줄로
            i += 1
            continue

        m = _RANGE.search(line)
        if m:
            rng = (_num(m.group(1)), _num(m.group(2)))
            # [Range(...)] public float x = 1f;  — 같은 줄에 필드가 올 수 있다

        m = _FIELD.search(line)
        if m:
            typ, name, dflt = m.group(1), m.group(2), m.group(3).strip()
            if typ == "bool":
                default = 1 if dflt.strip() == "true" else 0
            else:
                default = _num(dflt)
            out.append({
                "name": name, "type": typ, "group": group,
                "tooltip": tooltip, "min": rng[0] if rng else None,
                "max": rng[1] if rng else None, "default": default,
            })
            tooltip, rng = "", None

        i += 1
    return out


def parse_values():
    """GameTuning.asset → {name: 문자열값}, 그리고 {name: 줄번호}"""
    if not ASSET.exists():
        return {}, {}
    lines = ASSET.read_text(encoding="utf-8", errors="replace").splitlines()
    vals, at = {}, {}
    started = False
    for n, line in enumerate(lines):
        if "m_EditorClassIdentifier" in line:
            started = True
            continue
        if not started:
            continue
        m = re.match(r"^  (\w+): (.*)$", line)
        if m:
            vals[m.group(1)] = m.group(2).strip()
            at[m.group(1)] = n
    return vals, at


def load():
    """메타 + 현재값을 합친 목록. GUI가 이걸 그대로 표시한다."""
    fields = parse_fields()
    vals, _ = parse_values()
    for f in fields:
        raw = vals.get(f["name"])
        f["missing"] = raw is None            # 에셋에 아직 안 써진 필드(= 기본값 사용 중)
        if raw is None:
            f["value"] = f["default"]
        elif f["type"] == "bool":
            f["value"] = 1 if raw.strip() in ("1", "true", "True") else 0
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


def save(changes):
    """{name: value} 를 .asset에 반영. 해당 줄만 치환하고 없으면 끝에 추가.
    반환: (적용된 항목, 경고 목록)"""
    if not ASSET.exists():
        return [], [f"에셋 없음: {ASSET}"]

    fields = {f["name"]: f for f in parse_fields()}
    text = ASSET.read_text(encoding="utf-8", errors="replace")
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.replace("\r\n", "\n").split("\n")

    # 파일 끝 빈 줄은 떼어냈다가 마지막에 개행 하나로 복원한다.
    # (그냥 append하면 빈 줄이 끼고 마지막 개행이 사라져 Unity 서식이 깨진다)
    while lines and lines[-1].strip() == "":
        lines.pop()

    _, at = parse_values()
    applied, warns = [], []
    added = []

    for name, value in changes.items():
        f = fields.get(name)
        if f is None:
            warns.append(f"{name}: GameTuning.cs에 없는 필드 — 건너뜀")
            continue
        s = fmt(f, value)
        if name in at:
            lines[at[name]] = f"  {name}: {s}"
        else:
            added.append(f"  {name}: {s}")   # 에셋에 없던 필드(기본값 사용 중이던 것)
        applied.append(f"{name} = {s}")

    if added:
        # 파일 끝이 아니라 마지막 키 바로 뒤에 넣는다 — MonoBehaviour 매핑 안에 남게
        at_line = max(at.values()) if at else len(lines) - 1
        lines[at_line + 1:at_line + 1] = added

    ASSET.write_text(newline.join(lines) + newline, encoding="utf-8")
    return applied, warns


if __name__ == "__main__":
    fs = load()
    print(f"{len(fs)}개 필드 · 에셋 {ASSET.name}")
    g = None
    for f in fs:
        if f["group"] != g:
            g = f["group"]
            print(f"\n── {g} ──")
        rng = f"[{f['min']}~{f['max']}]" if f["min"] is not None else ""
        mark = "*" if f["changed"] else " "
        print(f" {mark}{f['name']:<28}{str(f['value']):>8}  기본 {str(f['default']):>8} {rng}")
