# -*- coding: utf-8 -*-
"""다녀올게(BRB) 세계관·스토리 정리 — 내부 공유용 PDF (풀 스포일러).
맑은 고딕 등록 + reportlab Platypus. 출력: docs/다녀올게-세계관-정리.pdf
"""
import os
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table,
                                TableStyle, HRFlowable, KeepTogether)

# ---- 폰트 ----
pdfmetrics.registerFont(TTFont("Malgun", r"C:\Windows\Fonts\malgun.ttf"))
pdfmetrics.registerFont(TTFont("MalgunBd", r"C:\Windows\Fonts\malgunbd.ttf"))
pdfmetrics.registerFontFamily("Malgun", normal="Malgun", bold="MalgunBd",
                              italic="Malgun", boldItalic="MalgunBd")

INK = colors.HexColor("#1a1a1f")
MUTE = colors.HexColor("#6b6b75")
AMBER = colors.HexColor("#c8862a")
RED = colors.HexColor("#a83232")
LINE = colors.HexColor("#d9d9e0")
PANEL = colors.HexColor("#f4f1ea")
HEAD = colors.HexColor("#2b2b33")

ss = getSampleStyleSheet()
def st(name, **kw):
    base = dict(fontName="Malgun", textColor=INK, leading=15.5, fontSize=10)
    base.update(kw)
    return ParagraphStyle(name, parent=ss["Normal"], **base)

TITLE = st("t", fontName="MalgunBd", fontSize=23, leading=28, textColor=INK)
SUB = st("s", fontName="Malgun", fontSize=11, leading=15, textColor=MUTE)
H1 = st("h1", fontName="MalgunBd", fontSize=15, leading=19, textColor=HEAD,
        spaceBefore=16, spaceAfter=5)
H2 = st("h2", fontName="MalgunBd", fontSize=11.5, leading=15, textColor=AMBER,
        spaceBefore=9, spaceAfter=2)
BODY = st("b", fontSize=10, leading=15.5, spaceAfter=3)
BULLET = st("bu", fontSize=10, leading=15, leftIndent=10, spaceAfter=1.5,
            bulletIndent=0)
CELL = st("c", fontSize=9, leading=13)
CELLB = st("cb", fontName="MalgunBd", fontSize=9, leading=13, textColor=HEAD)
CAP = st("cap", fontSize=8.5, leading=12, textColor=MUTE)

def P(t, s=BODY): return Paragraph(t, s)
def B(t): return Paragraph("• " + t, BULLET)
def rule(): return HRFlowable(width="100%", thickness=0.6, color=LINE,
                              spaceBefore=4, spaceAfter=8)

def tbl(data, widths, header=True):
    rows = []
    for r, row in enumerate(data):
        cells = []
        for c in row:
            if r == 0 and header:
                cells.append(Paragraph(c, CELLB))
            else:
                cells.append(Paragraph(c, CELL))
        rows.append(cells)
    t = Table(rows, colWidths=widths, repeatRows=1 if header else 0)
    style = [
        ("FONTNAME", (0,0), (-1,-1), "Malgun"),
        ("VALIGN", (0,0), (-1,-1), "TOP"),
        ("TOPPADDING", (0,0), (-1,-1), 5),
        ("BOTTOMPADDING", (0,0), (-1,-1), 5),
        ("LEFTPADDING", (0,0), (-1,-1), 7),
        ("RIGHTPADDING", (0,0), (-1,-1), 7),
        ("LINEBELOW", (0,0), (-1,-1), 0.4, LINE),
        ("LINEAFTER", (0,0), (-2,-1), 0.4, LINE),
        ("BOX", (0,0), (-1,-1), 0.6, LINE),
    ]
    if header:
        style += [("BACKGROUND", (0,0), (-1,0), PANEL),
                  ("LINEBELOW", (0,0), (-1,0), 0.8, MUTE)]
    t.setStyle(TableStyle(style))
    return t

CW = A4[0] - 36*mm  # content width
story = []

# ===== 표지 =====
story += [Spacer(1, 8*mm),
          P("다녀올게 <font size=14>(Be Right Back)</font>", TITLE),
          Spacer(1, 2*mm),
          P("세계관 · 스토리 정리 — <b>내부 공유용 · 풀 스포일러</b>", SUB),
          P("Unity 6 · 탑다운 2D 근접 생존 루팅 액션 (프로젝트: demo13-flashlight)", CAP),
          P("기준: 2026-06-17 캐논 (SSOT = gdd-core §5 / story.md)", CAP),
          rule()]

# ===== 0. 개요 =====
story += [P("0. 한 줄 정의 · 핵심 루프", H1)]
story += [P("기억을 잃은 회수꾼이 <b>봉쇄된 짙은 현상 도시</b>를 파고들며, 잃어버린 '동생'과 자신의 과거, "
            "도시를 묻은 정부의 진실을 추적한다. 무심히 반복하던 <b>“다녀올게”</b>가 끝내 가장 무거운 약속이 된다.")]
story += [P("핵심 루프: <b>안전가옥(무법지대 거점)</b> → 지역 선택 → <b>봉쇄선 안 레이드</b>(파밍·전투·루디 회수, 15~20분) "
            "→ 탈출 → 정산 → 거점 성장.")]

# ===== 1. 세계관 =====
story += [P("1. 세계관 (SSOT = gdd-core §5)", H1)]

story += [P("1.1 시대와 무대 — 봉쇄된 도시, 무법의 변두리", H2)]
story += [B("<b>아포칼립스가 아니다.</b> 세계·정부·사회는 정상. 무너진 건 <b>이 도시 하나</b>."),
          B("정부가 짙은 현상 도시를 <b>봉쇄 + 은폐</b>. 봉쇄선 <b>안</b>=봉쇄구역(폐도시·전기 끊김·레이드 무대), "
            "<b>밖</b>=무법지대(타운·전당포·컨테이너 거점, 회수꾼·밴딧·상인)."),
          B("봉쇄는 단순 격리가 아니라 <b>루디 자원작전</b> — 정부가 미래 자원을 독점 채굴, 은폐는 그 명분. "
            "정부는 무대에 흔적만(채굴 장비 잔해 등).")]

story += [P("1.2 짙은 현상", H2)]
story += [B("빛·공간·<b>시간</b>을 침식하는 현상. <b>낮/밤과 무관</b> — 현상이 발생해 한낮에도 밤처럼 어두워진다."),
          B("색·윤곽이 흐려지고, 공간이 어긋난다(반복되는 골목·없던 문·가짜 탈출구). 몬스터·루디가 나타난다.")]

story += [P("1.3 빛의 역설 — 빛이 어둠을 부른다", H2)]
story += [B("<b>루디를 제외한 모든 빛</b>(손전등·플레어·전등, 그중 <font color='#a83232'>불=최악</font>)이 현상을 "
            "자극·유인 → 침식 가속. 밝게·반복하면 <b>영구 짙은 구역</b>으로 굳는다."),
          B("핵심 한 줄: <b>“빛으로 밀어낼수록, 어둠이 가까워진다.”</b> → 정부조차 대규모 조명 채굴이 불가 → "
            "어둠을 누비는 <b>인간 회수꾼</b>이 필요한 근거.")]

story += [P("1.4 루디 — 현상이 굳힌 빛 (활성 자원)", H2)]
story += [B("현상이 <b>시간·기억을 갈아낸 부산물</b>이 응결한 결정. 불꽃 없는 안정광 = <b>유일하게 안전한 빛·동력</b>."),
          B("<b>충전·방전·마모가 있는 활성 자원</b>: 쓰면 방전→「죽은 루디」, 현상 노드/거점에서 <b>부분 충전</b>, "
            "반복 충전 시 최대 용량 마모→결국 영구 사망(→ 신선한 루디 회수가 계속 필요)."),
          B("순도가 높을수록 용량↑ + <b>기억 잔향</b>(만지면 장면·소리가 스침 = 단서). 모든 세력이 원함(정부 독점 vs 암시장).")]

story += [P("1.5 시간 왜곡 — 믿을 수 없는 시계", H2)]
story += [B("현상 안에서 시간은 비선형 — 가속·역행하고 과거가 현재에 겹친다(잔영 재생·반복 복도). "
            "<b>동생 생존·기억 재현</b>의 근거."),
          B("주인공의 손목시계 = <b>현상 측정기</b>로 비선형 시간에 동조. 게임에선 현상 구역 타이머가 출렁이는 "
            "“믿을 수 없는 시계”로 체험된다.")]

# ===== 2. 기원의 진실 =====
story += [P("2. 기원의 진실  〔최종 스포일러〕", H1)]
story += [P("게임 중심 미스터리의 바닥 진실. 플레이어는 초반에 “잃어버린 동생을 찾는 기억상실 회수꾼”으로만 알고, "
            "후반에 아래가 드러난다.", CAP)]

story += [P("2.1 발단 — 운석과 「알파」", H2)]
story += [B("정부의 비밀 계획 중(또는 그 여파로) <b>하늘에서 운석이 떨어지고, 그 안에서 한 여자아이가 발견된다</b> — 코드명 <b>「알파」</b>."),
          B("정부가 비밀 실험. 알파의 힘 = <b>시간의 세계선을 건드리는 능력</b>(사과를 즉시 썩히거나, 거꾸로 씨앗으로 되돌린다).")]

story += [P("2.2 강무진의 동기 — 되살리고 싶었던 것", H2)]
story += [B("정부가 영입한 최고 과학자. 그러나 진짜 동기는 <b>죽은 자기 딸을 되살리는 것</b>(딸의 유골함으로 은밀히 실험, 비인도적 실험도 마다 않음)."),
          B("그 와중 <b>아내마저 정부 내 이권 다툼에 희생</b>됨을 알게 됨 → 복수심 → 연구는 더 무분별해진다.")]

story += [P("2.3 파국 — 새벽의 죽음과 현상의 탄생", H2)]
story += [B("조수 <b>새벽</b>(주인공)이 환멸 → <b>알파를 데리고 탈출 시도</b> → 경비에 붙잡혀 <b>총에 맞아 사망</b>."),
          B("알파가 새벽을 되살리려다 통제를 잃고 <b>시간이 폭주·가속</b> → 건물 붕괴·사람이 재로 → <b>이것이 「짙은 현상」의 탄생</b>."),
          B("강무진은 <b>알파의 힘에 면역인 시계</b> 덕에 시간 피해는 면했으나, 잔해에 깔려 의식을 잃고 시계를 놓친다.")]

story += [P("2.4 부활 — 시계 · 기억 · 가짜 동생", H2)]
story += [B("알파가 떨어진 <b>시계를 새벽에게 채워</b> 준다: ① 기억의 시간만 되돌려 <b>기억상실 부활</b> "
            "② 시계에 <b>자신의 능력 일부 부여</b>(측정기·나침반) ③ 새벽이 자길 찾아오도록 <b>“동생 노을”이라는 가짜 기억</b>을 심음(오르골도 그 파생)."),
          B("알파는 잔해에 깔린 <b>강무진도 구하고 「속죄」 기억을 삽입</b>(원래의 죄책 위에 얹혀 끝내 희생으로 이끈다)."),
          B("마지막으로 알파는 <b>시간의 중심점</b>을 만들고 사라진다 = 중앙 영야(흑월 첨탑)의 발원지.")]

story += [P("2.5 표층 ↔ 진실 대조", H2)]
story += [tbl([
    ["플레이어가 믿는 표층", "실제 진실"],
    ["잃어버린 <b>동생 노을</b>을 찾는다", "동생은 없다 — <b>알파가 심은 가짜 기억</b>"],
    ["현상에 노출돼 기억을 잃었다", "<b>총에 맞아 죽었고</b>, 알파가 시간을 되돌려 되살림(기억만 비움)"],
    ["시계는 낡은 유품", "알파 힘의 <b>면역 보호구</b> + 알파가 능력을 부여한 측정기/나침반"],
    ["전당포 주인 = 수상한 노인", "<b>강무진</b> — 옛 상관, 딸·아내·복수에 무너진 과학자"],
    ["현상 = 원인 불명의 재앙", "<b>알파가 새벽을 되살리려다 폭주시킨 시간장</b>"],
], [CW*0.40, CW*0.60])]

story += [P("2.6 심층 — 알파의 규칙 · 새벽의 존재", H2)]
story += [B("<b>알파</b> = 인간이 아닌, ‘저편’(시간이 다르게 흐르는 곳)에서 운석으로 온 <b>시간을 만지는 존재</b>. "
            "인간의 정서(외로움·애착·죄책)는 느낀다 — 자길 사람으로 대한 새벽에게 매달린 게 모든 사건의 엔진."),
          B("<b>능력의 한계</b>: 시간은 국소적으로만 정밀. 새벽을 되살린 <b>대형 역행이 통제를 잃고 힘이 도시로 흩어진 것 = 짙은 현상</b>. "
            "알파는 중심에서 그 폭주를 <b>붙드는 앵커</b>(갇힌 게 아니라 버티는 중)."),
          B("<b>전능 아님</b>: 기억 심기는 불완전(균열이 남는다), 앵커로 고갈, <b>산 사람의 의지는 못 덮어쓴다(밀어줄 뿐)</b> "
            "— 강무진의 희생도 조종이 아니라 그 자신의 선택."),
          B("<b>새벽의 존재</b>: 되감긴 시간 위에 선 부활체. <b>시계가 곧 생명선</b> — 잃으면 시간이 따라잡혀 다시 죽거나 기억이 더 무너진다."),
          B("<b>‘노을’이라는 이름</b> = 알파가 새벽의 잠재 정서에서 길어 올린 것(새벽↔노을, 빛이 오가는 경계). 가짜지만 가장 다정한 거짓.")]

# ===== 3. 인물 =====
story += [P("3. 인물", H1)]
story += [tbl([
    ["인물", "정체 / 아크"],
    ["<b>새벽</b> (주인공)", "기억 0의 회수꾼. 실은 연구 조수 — 한 번 죽었다 알파가 되살림. 시계 소유자. "
     "흔적→정체→본명·자신의 죽음→중심점에서 알파 대면."],
    ["<b>강무진</b> (전당포 주인)", "새벽의 옛 상관. 죽은 딸·아내·정부 복수에 무너진 과학자. 조수 죽음의 죄책 + 알파가 심은 "
     "「속죄」 → 차가운 거래상에서 끝내 <b>희생</b>(“이번엔 안 도망쳐”)."],
    ["<b>알파</b> (=가짜 동생 “노을”)", "운석에서 발견된 소녀, 시간 세계선 조작. 새벽을 되살리고 가짜 동생 기억을 심음. "
     "시간의 중심에 홀로 잠김 = 현상의 발원."],
], [CW*0.26, CW*0.74])]

# ===== 4. 5지역 =====
story += [P("4. 지역 진행 (5지역)", H1)]
story += [P("“동생 노을” 단서는 <b>이중층</b>: 표층=동생 추적 / 진실=알파의 흔적 + 새벽의 심어진 기억이 현상에 투영. "
            "단서들이 점점 모순되는 게 곧 진실의 복선이다.", CAP)]
story += [tbl([
    ["지역", "줄기", "회수하는 진실"],
    ["1 폐상가", "깨어남·회수꾼 생존 루프·짙은 현상 첫 체험", "오르골·시계 두 떡밥, 강무진의 묘한 반응"],
    ["2 침묵 생활", "첫 사람(의사)·폐아파트의 멈춘 일상", "동생 단서 첫 등장(사진=얼굴 균열)·주인공이 현상과 얽힘"],
    ["3 묻힌 정비창", "단일 내부 3층 하강·정부가 묻은 것", "정부 자원작전 물증·시계=연구장비·(이름 가려진) 실험체 중앙 이송 기록·정부 키카드"],
    ["4 기억의 극장", "소형·진실 폭발·엔딩 분수령", "강무진 정체·주인공 본명·동생 정체 균열(생존 반전→실은 알파)·블랙마켓"],
    ["5 중앙 영야", "잔영 3개 풀이→흑월 첨탑(시간의 중심점)", "루디 정체·강무진 희생·알파 대면 → 엔딩"],
], [CW*0.16, CW*0.40, CW*0.44])]

# ===== 5. 엔딩 =====
story += [P("5. 엔딩 (2종 · 둘 다 배드 아님)", H1)]
story += [P("최종 분기는 중앙 영야 「시간의 중심점」에서 알파와의 재회에서 갈린다. "
            "<b>진실 수집률(추적)</b>이 진엔딩 게이트.", CAP)]
story += [tbl([
    ["엔딩", "내용", "결"],
    ["① 진엔딩 「진짜 모습」", "알파의 진짜 정체(운석 소녀·자신을 되살린 존재)를 알게 되고 받아들임. 가짜 동생 환상은 무너지지만 진짜 관계를 마주함.", "무겁지만 진짜 — 진실 위의 구원"],
    ["② 굿엔딩 「동생과의 일상」", "진실에 다 닿지 못했거나, 알고도 가짜 기억의 평온을 택함. 알파를 동생 노을로 여기며 함께 살아감.", "따뜻하지만 거짓 위 — 달콤씁쓸"],
], [CW*0.22, CW*0.56, CW*0.22])]

story += [P("5.1 엔딩 후 세계 (aftermath)", H2)]
story += [B("<b>진엔딩① 꺼냄</b>: 앵커 풀림 → 현상 걷힘(도시 정화) + 루디 고갈 + 시계 멈춤(새벽 필멸화). 둘의 새 삶. (선택: 정부 폭로)"),
          B("<b>진엔딩① 놓아줌</b>: 알파가 스스로 잠들며 정화 — 가장 조용한 구원, 가장 큰 상실."),
          B("<b>진엔딩① 곁에 남음</b>: 새벽이 중심에 함께 앵커로 → 현상 안정되나 걷히진 않음. 둘은 시간 밖에서."),
          B("<b>굿엔딩②</b>: 동생으로 데리고 나옴 → 앵커 약화 유지 → 현상·봉쇄 지속(묻힌 세계). 거짓 위 평온, ‘모르는 행복’.")]

# ===== 6. 세계 텍스처 =====
story += [P("6. 세계 텍스처", H1)]
story += [P("6.1 무법지대 4세력 · 정부", H2)]
story += [B("<b>회수꾼</b>(플레이어=신참, 게시판 의뢰·암구호) · <b>전당포·상인망</b>(강무진·떠돌이 상인) · "
            "<b>밴딧</b>(캠프 점거, 불 피워 자멸) · <b>블랙마켓/정부 내부자</b>(전직 공무원 = 정부 정보 거래 → 진엔딩 진실)."),
          B("<b>정부</b>: 무대 밖(흔적만). 목적 = 알파·루디 독점 통제, 봉쇄+은폐는 명분. 빛의 역설로 직접 채굴 불가 → 회수꾼 이용. 내부 이권다툼(강무진 아내 희생).")]
story += [P("6.2 현상 몬스터 · 루디 = 기억", H2)]
story += [B("<b>몬스터</b> = 현상이 토해낸 과거의 잔재(재가 된 사람·짐승이 시간장에 붙들림) + 오염 변종. ‘죽인다’기보다 ‘돌려보낸다’."),
          B("<b>루디 = 삼켜진 사람들의 기억</b>의 응결. 회수·판매·소모하는 루디가 누군가의 마지막 기억일 수 있다 — 회수꾼 일의 윤리적 무게.")]

# ===== 7. 모티프 =====
story += [P("7. 핵심 모티프", H1)]
story += [B("<b>오르골</b> — 가짜 동생 기억에서 파생된 곡(실존 단서 아님). 진엔딩=한 음 어긋나며 멈춤(가짜 신호) / 굿엔딩=끝까지 온전히."),
          B("<b>시계</b> — 측정기·나침반·중심 진입 열쇠. 죽으면 시계가 시간을 회수(=새벽 자신의 기원의 메아리)."),
          B("<b>“다녀올게”</b> — 매 출격의 가벼운 인사 / 심어진 약속 / 엔딩에서 정반대 무게로 회수."),
          B("<b>루디</b> — 물질화된 기억. 들어갈 이유이자 진실의 부산물.")]

story += [Spacer(1, 6*mm), rule(),
          P("ⓘ 본 문서는 현재 캐논 요약본입니다. 상세·구현은 docs/gdd-core.md §5 · story.md · story-script.md 참조.", CAP)]

# ===== 빌드 =====
out = os.path.join(os.path.dirname(__file__), "..", "다녀올게-세계관-정리.pdf")
out = os.path.abspath(out)
doc = SimpleDocTemplate(out, pagesize=A4,
                        leftMargin=18*mm, rightMargin=18*mm,
                        topMargin=16*mm, bottomMargin=15*mm,
                        title="다녀올게 세계관·스토리 정리 (내부용)",
                        author="demo13-flashlight")
doc.build(story)
print("OK ->", out)
