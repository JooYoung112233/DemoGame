#!/usr/bin/env python3
"""Extract S-000~S-018 dialogue from story-script.md → HTML dialogue book."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "story-script.md"
OUT = ROOT / "dialogue-book-S000-S018.html"

text = SRC.read_text(encoding="utf-8")

scene_pat = re.compile(r"^## ((?:S-\d{3}|T-\d{3})\.[^\n]+)", re.M)
scenes = []
for m in scene_pat.finditer(text):
    sid = re.match(r"(S-\d{3}|T-\d{3})", m.group(1)).group(1)
    num = int(sid.split("-")[1])
    if sid.startswith("T-") or num <= 18:
        scenes.append((m.start(), sid, m.group(1).strip()))
scenes.append((len(text), None, None))

SPEAKER_RE = re.compile(
    r"^([^\[\n:]{1,40}?)\s*:\s*(?:\([^)]*\))?\s*$"
)
QUOTE_RE = re.compile(r'^\s+"(.+)"\s*$')
CHOICE_RE = re.compile(r'[①②③④⑤]\s*"?([^"\n]+)"?')


def extract_dialogue(start: int, end: int) -> list[tuple[str, str]]:
    block = text[start:end]
    code = re.search(r"```\n(.*?)\n```", block, re.S)
    if not code:
        return []
    body = code.group(1)
    out: list[tuple[str, str]] = []
    speaker: str | None = None
    in_quest = False
    skip_tags = {"조건", "기술", "게임", "UI", "SE", "BGM", "앰비언트", "화면"}

    for line in body.split("\n"):
        stripped = line.strip()
        if stripped == "퀘스트내용" or stripped.startswith("퀘스트내용"):
            in_quest = True
            speaker = "【의뢰서】"
            continue
        if in_quest:
            if stripped.startswith("→") or stripped.startswith("——"):
                in_quest = False
                speaker = None
            elif stripped.startswith("[UI]") or stripped.startswith("[게임]") or stripped.startswith("[기술]"):
                in_quest = False
                speaker = None
            elif stripped.startswith("보상") or stripped.startswith("- 익명") or stripped.startswith("- 수아"):
                out.append((speaker or "【의뢰서】", stripped))
                if stripped.startswith("- 익명"):
                    in_quest = False
                    speaker = None
                continue
            elif stripped and not stripped.startswith(">"):
                # [의뢰 제목] 형태도 본문으로 수록
                out.append((speaker or "【의뢰서】", stripped))
                continue
        if stripped.startswith("["):
            if stripped.startswith("[말풍선]"):
                speaker = "【말풍선】"
            continue
        sm = SPEAKER_RE.match(line)
        if sm and not stripped.startswith("→"):
            name = sm.group(1).strip()
            if name in skip_tags:
                continue
            speaker = name
            continue
        qm = QUOTE_RE.match(line)
        if qm and speaker:
            out.append((speaker, qm.group(1)))
            continue
        if "①" in line or "②" in line or "③" in line or "④" in line:
            cm = CHOICE_RE.search(line)
            if cm:
                out.append(("【선택지】", cm.group(1).strip()))
    return out


entries = []
for i in range(len(scenes) - 1):
    start, sid, title = scenes[i]
    end = scenes[i + 1][0]
    entries.append((sid, title, extract_dialogue(start, end)))

# Merge ko.json overrides for implemented scenes
ko_path = ROOT.parent / "Assets/Resources/Story/Locale/ko.json"
if ko_path.exists():
    import json
    ko = {e["key"]: e["value"] for e in json.loads(ko_path.read_text(encoding="utf-8"))["entries"]}

html_parts = ["""<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>다녀올게 (BRB) — 1지역 대사집 S-000~S-018</title>
<style>
  @page { margin: 18mm 16mm; }
  * { box-sizing: border-box; }
  body {
    font-family: "Malgun Gothic", "Apple SD Gothic Neo", sans-serif;
    font-size: 11pt;
    line-height: 1.55;
    color: #1a1a1a;
    max-width: 210mm;
    margin: 0 auto;
    padding: 12mm 14mm;
    background: #fff;
  }
  .cover {
    text-align: center;
    padding: 28mm 0 20mm;
    border-bottom: 2px solid #222;
    margin-bottom: 10mm;
    page-break-after: always;
  }
  .cover h1 { font-size: 22pt; font-weight: 700; margin: 0 0 6mm; letter-spacing: -0.02em; }
  .cover .sub { font-size: 12pt; color: #444; margin: 2mm 0; }
  .cover .meta { font-size: 9.5pt; color: #666; margin-top: 10mm; }
  .toc { page-break-after: always; }
  .toc h2 { font-size: 14pt; border-bottom: 1px solid #ccc; padding-bottom: 3mm; }
  .toc ol { padding-left: 6mm; }
  .toc li { margin: 2mm 0; }
  .toc a { color: #222; text-decoration: none; }
  .scene {
    margin-bottom: 8mm;
    page-break-inside: avoid;
  }
  .scene-header {
    background: #f0f0f0;
    border-left: 4px solid #333;
    padding: 3mm 4mm;
    margin-bottom: 4mm;
  }
  .scene-id { font-size: 9pt; color: #555; font-weight: 600; }
  .scene-title { font-size: 13pt; font-weight: 700; margin: 1mm 0 0; }
  .scene-note { font-size: 8.5pt; color: #666; margin-top: 2mm; }
  .line { display: flex; gap: 4mm; margin: 2mm 0; padding: 1.5mm 0; border-bottom: 1px dotted #e8e8e8; }
  .speaker {
    flex: 0 0 22mm;
    font-weight: 700;
    font-size: 9.5pt;
    color: #333;
  }
  .speaker.player { color: #1a5a8a; }
  .speaker.choice { color: #6a4a00; font-style: italic; }
  .speaker.unknown { color: #555; }
  .text { flex: 1; }
  .no-dialogue { font-size: 9pt; color: #888; font-style: italic; padding: 2mm 0; }
  .footer {
    margin-top: 12mm;
    padding-top: 4mm;
    border-top: 1px solid #ddd;
    font-size: 8pt;
    color: #888;
    text-align: center;
  }
  @media print {
    body { padding: 0; }
    .scene { page-break-inside: auto; }
    .scene-header { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  }
</style>
</head>
<body>
<div class="cover">
  <h1>다녀올게 (BRB)</h1>
  <p class="sub">1지역 본선 대사집</p>
  <p class="sub">S-000 ~ S-018</p>
  <p class="meta">출처: docs/story-script.md · 폐상가 교역 지구 프롤로그<br>
  NPC/주인공 대사 · 선택지 · 의뢰서 본문 수록 (연출·시스템 태그 제외)</p>
</div>
<div class="toc">
  <h2>목차</h2>
  <ol>
"""]

for sid, title, _ in entries:
    anchor = sid.replace("-", "")
    html_parts.append(f'    <li><a href="#{anchor}">{sid} — {title.split(". ", 1)[-1]}</a></li>\n')

html_parts.append("  </ol>\n</div>\n")

SPEAKER_CLASS = {
    "주인공": "player",
    "새벽": "player",
    "【선택지】": "choice",
    "【의뢰서】": "choice",
    "【말풍선】": "choice",
    "???": "unknown",
}

SCENE_NOTES = {
    "S-000": "대사 없음 (연출만)",
    "S-001": "대사 없음 (튜토리얼·이동)",
    "S-005": "레이드 구간 — 말풍선·내레이션 위주, 본문 대사 최소",
    "S-006": "대사 없음 (탈출)",
    "S-014": "대사 없음 (출격 확인·연출)",
    "S-015": "레이드 통합 — NPC 대사 없음 (튜토리얼 UI·시계 떡밥)",
}

for sid, title, dial in entries:
    anchor = sid.replace("-", "")
    note = SCENE_NOTES.get(sid, "")
    html_parts.append(f'<section class="scene" id="{anchor}">\n')
    html_parts.append('  <div class="scene-header">\n')
    html_parts.append(f'    <div class="scene-id">{sid}</div>\n')
    html_parts.append(f'    <div class="scene-title">{title.split(". ", 1)[-1]}</div>\n')
    if note:
        html_parts.append(f'    <div class="scene-note">{note}</div>\n')
    html_parts.append("  </div>\n")
    if not dial:
        html_parts.append('  <p class="no-dialogue">(이 씬에 추출된 NPC/주인공 대사 없음)</p>\n')
    else:
        for speaker, line in dial:
            cls = SPEAKER_CLASS.get(speaker, "")
            if speaker.startswith("?"):
                cls = "unknown"
            html_parts.append(f'  <div class="line"><span class="speaker {cls}">{speaker}</span><span class="text">{line}</span></div>\n')
    html_parts.append("</section>\n")

html_parts.append("""
<div class="footer">
  다녀올게 (BRB) · 1지역 스토리 대사집 · 자동 생성 · 브라우저 인쇄(Ctrl+P)로 PDF 저장 가능
</div>
</body>
</html>
""")

OUT.write_text("".join(html_parts), encoding="utf-8")
print(f"Wrote {OUT} ({len(entries)} scenes, {sum(len(d) for _,_,d in entries)} lines)")
