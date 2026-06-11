const pptxgen = require("pptxgenjs");
const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.33 x 7.5
pres.author = "BRB";
pres.title = "다녀올게 — 세계관 브리핑";

// ---- palette ----
const BG     = "0A0D14"; // near-black navy
const PANEL  = "141A26"; // card
const PANEL2 = "1B2433"; // card alt
const LINE   = "2A3344"; // hairline border
const TXT    = "EAEEF5"; // near white
const MUTE   = "8C96A8"; // muted
const AMBER  = "F2A33C"; // 루디 glow (primary accent)
const AMBERD = "B9772A"; // amber deep
const COLD   = "6FA8C7"; // 현상 cold
const VIOLET = "8E7CE0"; // 시간/anomaly
const RED     = "D9636B"; // 위험/불
const F = "Malgun Gothic";

const W = 13.33, H = 7.5;

function slideBg(s, color){ s.background = { color: color || BG }; }
const sh = () => ({ type:"outer", color:"000000", blur:9, offset:3, angle:90, opacity:0.45 });

// soft glow dot
function glow(s, x, y, d, color, alpha){
  s.addShape(pres.shapes.OVAL, { x:x-d/2, y:y-d/2, w:d, h:d, fill:{ color, transparency:alpha??70 }, line:{ type:"none" } });
}
// eyebrow with amber tick
function eyebrow(s, x, y, txt){
  s.addShape(pres.shapes.RECTANGLE, { x, y:y+0.045, w:0.16, h:0.16, fill:{color:AMBER}, line:{type:"none"} });
  s.addText(txt, { x:x+0.28, y, w:9, h:0.3, fontFace:F, fontSize:12, color:AMBER, bold:true, charSpacing:3, align:"left", valign:"middle", margin:0 });
}
function title(s, x, y, txt, size){
  s.addText(txt, { x, y, w:12, h:0.9, fontFace:F, fontSize:size||32, color:TXT, bold:true, align:"left", valign:"middle", margin:0 });
}
function card(s, x, y, w, h, fill){
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius:0.08, fill:{color:fill||PANEL}, line:{color:LINE, width:1}, shadow:sh() });
}

// ============================================================ 1. TITLE
let s = pres.addSlide(); slideBg(s);
glow(s, 11.0, 1.6, 6.5, AMBER, 88);
glow(s, 11.4, 1.4, 3.0, AMBER, 72);
glow(s, 1.2, 7.2, 5.0, COLD, 90);
eyebrow(s, 0.9, 1.05, "WORLD SETTING · 세계관 브리핑");
s.addText("다녀올게", { x:0.85, y:1.7, w:9, h:1.5, fontFace:F, fontSize:84, color:TXT, bold:true, align:"left", valign:"middle", margin:0 });
s.addText([{text:"Be Right Back", options:{italic:true}}], { x:0.92, y:3.15, w:9, h:0.6, fontFace:F, fontSize:24, color:AMBER, align:"left", valign:"middle", margin:0 });
s.addText("정부가 봉쇄한 짙은 현상 도시 — 그 봉쇄선 바깥 무법지대에서 살아남으며,\n안으로 들어가 미래 자원 '루디'를 회수하는 생존 루팅 액션.",
  { x:0.92, y:4.15, w:8.6, h:1.3, fontFace:F, fontSize:17, color:MUTE, align:"left", valign:"top", lineSpacingMultiple:1.25, margin:0 });

// ============================================================ 2. ONE SENTENCE + 3 callouts
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "한눈에");
s.addText([
  {text:"기억을 잃은 회수꾼이, ", options:{color:TXT}},
  {text:"봉쇄된 어둠의 도시", options:{color:AMBER, bold:true}},
  {text:"에 들어가\n", options:{color:TXT}},
  {text:"루디", options:{color:AMBER, bold:true}},
  {text:"를 캐 나오며, ", options:{color:TXT}},
  {text:"잃어버린 동생의 진실", options:{color:VIOLET, bold:true}},
  {text:"을 쫓는다.", options:{color:TXT}},
], { x:0.9, y:1.35, w:11.5, h:1.9, fontFace:F, fontSize:33, bold:true, align:"left", valign:"top", lineSpacingMultiple:1.18, margin:0 });

const cards2 = [
  ["장르", "생존 루팅 액션", "탐색 · 근접 전투 · 추출(extraction)"],
  ["시점", "탑다운 2D", "어둠 · 시야 · 손전등"],
  ["핵심 감정", "“한 집만 더 털까?”", "욕심 vs 생존의 줄타기"],
];
let cx = 0.9, cw = 3.78, gap = 0.49, cyy = 4.0, chh = 2.5;
cards2.forEach((c,i)=>{
  const x = cx + i*(cw+gap);
  card(s, x, cyy, cw, chh);
  s.addShape(pres.shapes.RECTANGLE, { x:x+0.45, y:cyy+0.5, w:0.32, h:0.05, fill:{color:AMBER}, line:{type:"none"} });
  s.addText(c[0], { x:x+0.45, y:cyy+0.62, w:cw-0.9, h:0.35, fontFace:F, fontSize:13, color:AMBER, bold:true, charSpacing:2, margin:0 });
  s.addText(c[1], { x:x+0.45, y:cyy+1.02, w:cw-0.9, h:0.7, fontFace:F, fontSize:22, color:TXT, bold:true, valign:"top", margin:0, lineSpacingMultiple:1.0 });
  s.addText(c[2], { x:x+0.45, y:cyy+1.78, w:cw-0.9, h:0.55, fontFace:F, fontSize:13, color:MUTE, valign:"top", margin:0, lineSpacingMultiple:1.1 });
});

// ============================================================ 3. 시대와 무대 (diagram)
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "시대와 무대");
title(s, 0.9, 1.12, "아포칼립스가 아니다 — 무너진 건 이 도시 하나", 29);
s.addText("세계도 정부도 멀쩡하다. 정부는 짙은 현상이 발생한 도시 하나를 봉쇄하고 존재를 은폐했다.",
  { x:0.9, y:1.95, w:11.5, h:0.5, fontFace:F, fontSize:15, color:MUTE, margin:0 });

// two zones
const dy = 2.85, dh = 3.65;
// 봉쇄선 밖 (left)
card(s, 0.9, dy, 5.55, dh, PANEL);
s.addText("봉쇄선 밖 · 무법지대", { x:1.25, y:dy+0.35, w:4.9, h:0.4, fontFace:F, fontSize:18, color:COLD, bold:true, margin:0 });
s.addText("완충지대 / 거점 · 허브", { x:1.25, y:dy+0.78, w:4.9, h:0.3, fontFace:F, fontSize:12, color:MUTE, charSpacing:1, margin:0 });
s.addText([
  {text:"타운 · 전당포 · 컨테이너 안전가옥", options:{bullet:true, breakLine:true}},
  {text:"회수꾼 · 밴딧 · 떠돌이 상인 · 생존자", options:{bullet:true, breakLine:true}},
  {text:"정부 통제 밖 — 치안은 없지만 종말은 아님", options:{bullet:true}},
], { x:1.3, y:dy+1.35, w:4.85, h:2.0, fontFace:F, fontSize:14.5, color:TXT, valign:"top", paraSpaceAfter:9, margin:0 });

// 봉쇄선 안 (right)
card(s, 6.88, dy, 5.55, dh, PANEL2);
s.addText("봉쇄선 안 · 봉쇄구역", { x:7.23, y:dy+0.35, w:4.9, h:0.4, fontFace:F, fontSize:18, color:AMBER, bold:true, margin:0 });
s.addText("짙은 현상 / 레이드 무대", { x:7.23, y:dy+0.78, w:4.9, h:0.3, fontFace:F, fontSize:12, color:MUTE, charSpacing:1, margin:0 });
s.addText([
  {text:"전기 끊긴 폐도시 — 낮에도 밤처럼 어둡다", options:{bullet:true, breakLine:true}},
  {text:"괴물 · 밴딧 · 이상현상 · 미래 자원 루디", options:{bullet:true, breakLine:true}},
  {text:"15분 안에 회수하고 탈출해야 한다", options:{bullet:true}},
], { x:7.28, y:dy+1.35, w:4.85, h:2.0, fontFace:F, fontSize:14.5, color:TXT, valign:"top", paraSpaceAfter:9, margin:0 });

// 봉쇄선 divider + arrow
s.addShape(pres.shapes.LINE, { x:6.665, y:dy+0.1, w:0, h:dh-0.2, line:{color:AMBER, width:2, dashType:"dash"} });
s.addText("봉쇄선", { x:6.06, y:dy-0.42, w:1.2, h:0.3, fontFace:F, fontSize:11, color:AMBER, bold:true, align:"center", margin:0 });
s.addShape(pres.shapes.LINE, { x:5.0, y:dy+dh+0.28, w:3.3, h:0, line:{color:MUTE, width:1.5, endArrowType:"triangle"} });
s.addText("바깥 거점 → 봉쇄선 넘어 침투 → 회수 후 귀환", { x:0.9, y:dy+dh+0.4, w:11.5, h:0.35, fontFace:F, fontSize:12.5, color:MUTE, align:"center", italic:true, margin:0 });

// ============================================================ 4. 짙은 현상 (3 cards)
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "핵심 장치 ①");
title(s, 0.9, 1.12, "짙은 현상 — 빛과 공간, 그리고 시간을 침식한다", 29);
s.addText("시간(밤)과 무관하다. 현상이 내려앉은 곳은 한낮에도 밤처럼 어두워진다. “밤이라 어두운 게 아니라, 현상이 어둠을 만든다.”",
  { x:0.9, y:1.95, w:11.5, h:0.55, fontFace:F, fontSize:15, color:MUTE, margin:0 });

const anom = [
  [COLD,   "빛의 침식", "멀리부터 색과 윤곽이 흐려지고, 짙은 곳은 바로 앞도 보이지 않는다."],
  [AMBER,  "공간의 왜곡", "길이 바뀌고, 없던 문·골목이 생기고, 같은 자리가 반복되고, 탈출구가 옮겨진다."],
  [VIOLET, "시간의 교란", "과거와 현재가 겹친다. 잔상이 재생되고, 사라진 자가 그 안에선 멈춰 있다."],
];
cx=0.9; cw=3.78; gap=0.49; cyy=2.95; chh=3.35;
anom.forEach((c,i)=>{
  const x = cx + i*(cw+gap);
  card(s, x, cyy, cw, chh);
  glow(s, x+0.78, cyy+0.95, 1.5, c[0], 72);
  s.addShape(pres.shapes.OVAL, { x:x+0.45, y:cyy+0.55, w:0.62, h:0.62, fill:{color:c[0]}, line:{type:"none"} });
  s.addText(String(i+1), { x:x+0.45, y:cyy+0.55, w:0.62, h:0.62, fontFace:F, fontSize:22, color:BG, bold:true, align:"center", valign:"middle", margin:0 });
  s.addText(c[1], { x:x+0.45, y:cyy+1.45, w:cw-0.9, h:0.5, fontFace:F, fontSize:20, color:TXT, bold:true, valign:"top", margin:0 });
  s.addText(c[2], { x:x+0.45, y:cyy+2.05, w:cw-0.85, h:1.1, fontFace:F, fontSize:14, color:MUTE, valign:"top", lineSpacingMultiple:1.2, margin:0 });
});

// ============================================================ 5. 불의 역설 vs 루디
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "핵심 장치 ②");
title(s, 0.9, 1.12, "불의 역설, 그리고 루디", 29);
s.addText("전기는 죽었다. 빛이 필요하지만 — 빛을 잘못 쓰면 어둠이 더 짙어진다.",
  { x:0.9, y:1.95, w:11.5, h:0.5, fontFace:F, fontSize:15, color:MUTE, margin:0 });

const colY=2.9, colH=3.7, colW=5.55;
// 불 (left, danger)
card(s, 0.9, colY, colW, colH, PANEL);
s.addShape(pres.shapes.RECTANGLE, { x:0.9, y:colY, w:0.1, h:colH, fill:{color:RED}, line:{type:"none"} });
s.addText("일반 불 (횃불·모닥불)", { x:1.3, y:colY+0.4, w:colW-0.7, h:0.45, fontFace:F, fontSize:20, color:RED, bold:true, margin:0 });
s.addText("위험한 빛", { x:1.3, y:colY+0.9, w:colW-0.7, h:0.3, fontFace:F, fontSize:12, color:MUTE, charSpacing:2, margin:0 });
s.addText([
  {text:"열·연기·흔들리는 불빛이 현상 잔재를 자극", options:{bullet:true, breakLine:true}},
  {text:"같은 곳에서 반복하면 빛이 약해지고…", options:{bullet:true, breakLine:true}},
  {text:"낮에도 밤 같은 짙은 구역으로 침식 가속", options:{bullet:true, breakLine:true}},
  {text:"밝힐수록, 진짜 어둠이 가까워진다", options:{bullet:true, color:RED, bold:true}},
], { x:1.35, y:colY+1.45, w:colW-0.85, h:2.0, fontFace:F, fontSize:14.5, color:TXT, valign:"top", paraSpaceAfter:9, margin:0 });

// 루디 (right, hope)
card(s, 6.88, colY, colW, colH, PANEL2);
s.addShape(pres.shapes.RECTANGLE, { x:6.88, y:colY, w:0.1, h:colH, fill:{color:AMBER}, line:{type:"none"} });
glow(s, 11.7, colY+0.7, 2.0, AMBER, 78);
s.addText("루디 (Rudi)", { x:7.3, y:colY+0.4, w:colW-0.7, h:0.45, fontFace:F, fontSize:20, color:AMBER, bold:true, margin:0 });
s.addText("안전한 빛 · 미래 자원", { x:7.3, y:colY+0.9, w:colW-0.7, h:0.3, fontFace:F, fontSize:12, color:MUTE, charSpacing:2, margin:0 });
s.addText([
  {text:"불꽃 없이 안정된 빛 — 잔재를 자극하지 않음", options:{bullet:true, breakLine:true}},
  {text:"전기 죽은 세계의 유일하게 안전한 동력", options:{bullet:true, breakLine:true}},
  {text:"봉쇄구역 안에서만 발견되는 특수 자원", options:{bullet:true, breakLine:true}},
  {text:"모든 세력이 원한다 — 들어갈 이유", options:{bullet:true, color:AMBER, bold:true}},
], { x:7.35, y:colY+1.45, w:colW-0.85, h:2.0, fontFace:F, fontSize:14.5, color:TXT, valign:"top", paraSpaceAfter:9, margin:0 });

// ============================================================ 6. 정부의 진짜 동기 (flow)
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "세력 구도");
title(s, 0.9, 1.12, "정부의 진짜 동기 — 봉쇄는 곧 자원작전", 29);
s.addText("정부는 무대에 직접 없다(흔적만). 봉쇄는 격리가 아니라, 미래 자원 루디를 독점 채굴하려는 작전이다.",
  { x:0.9, y:1.95, w:11.7, h:0.5, fontFace:F, fontSize:15, color:MUTE, margin:0 });

const flow = [
  [AMBER,  "독점 채굴", "정부가 도시를 봉쇄·은폐하고 루디를 독점하려 한다."],
  [RED,    "불빛의 벽", "하지만 불의 역설 탓에 정부조차 불을 못 쓴다 → 어둠 속 저광량 채굴뿐, 느리고 비효율."],
  [COLD,   "그래서 회수꾼", "어둠을 직접 누비는 인간의 손이 더 유효 → 회수꾼이 필요해진다."],
  [VIOLET, "암시장", "회수꾼·전당포가 캐 온 루디가 독점의 틈을 빼먹는 비공식 공급망으로 흐른다."],
];
const fy=3.0, fw=2.78, fgap=0.41, fh=3.05; let fx=0.9;
flow.forEach((c,i)=>{
  const x = fx + i*(fw+fgap);
  card(s, x, fy, fw, fh);
  s.addShape(pres.shapes.OVAL, { x:x+0.4, y:fy+0.42, w:0.55, h:0.55, fill:{color:c[0]}, line:{type:"none"} });
  s.addText(String(i+1), { x:x+0.4, y:fy+0.42, w:0.55, h:0.55, fontFace:F, fontSize:19, color:BG, bold:true, align:"center", valign:"middle", margin:0 });
  s.addText(c[1], { x:x+0.4, y:fy+1.18, w:fw-0.8, h:0.5, fontFace:F, fontSize:17, color:TXT, bold:true, valign:"top", margin:0 });
  s.addText(c[2], { x:x+0.4, y:fy+1.72, w:fw-0.72, h:1.2, fontFace:F, fontSize:12.5, color:MUTE, valign:"top", lineSpacingMultiple:1.18, margin:0 });
  if(i<flow.length-1){
    s.addText("→", { x:x+fw-0.06, y:fy+0.45, w:0.5, h:0.6, fontFace:F, fontSize:24, color:AMBER, bold:true, align:"center", valign:"middle", margin:0 });
  }
});
s.addText([
  {text:"정부 = 독점·통제   ", options:{color:AMBER, bold:true}},
  {text:"vs   ", options:{color:MUTE}},
  {text:"회수꾼·전당포·블랙마켓 = 비공식 공급망", options:{color:COLD, bold:true}},
], { x:0.9, y:6.45, w:11.7, h:0.45, fontFace:F, fontSize:15, align:"center", valign:"middle", margin:0 });

// ============================================================ 7. 비선형 시간
s = pres.addSlide(); slideBg(s);
glow(s, 11.6, 5.8, 5.5, VIOLET, 86);
eyebrow(s, 0.9, 0.7, "핵심 장치 ③");
title(s, 0.9, 1.12, "현상 안에선 시간이 다르게 흐른다", 29);
s.addText("선형이 아니다 — 순서가 무너지고, 과거와 현재가 한 공간에 겹친다.",
  { x:0.9, y:1.95, w:11.5, h:0.5, fontFace:F, fontSize:15, color:MUTE, margin:0 });

const tcards = [
  ["과거의 재생", "떠난 자·죽은 자의 잔상이 그 자리를 걷고, 없는 열차 소리가 지나간다."],
  ["비틀린 생존", "바깥에서 사라진 자가 안에선 멈춘 채 살아 있을 수 있다 — 동생 생존의 근거."],
  ["믿을 수 없는 시계", "현상 구역에선 15분 레이드 타이머가 출렁인다. 손목시계만이 진짜 남은 시간을 읽는다."],
];
const ty=2.95, tw=3.78, tgap=0.49, th=3.25; let txx=0.9;
tcards.forEach((c,i)=>{
  const x = txx + i*(tw+tgap);
  card(s, x, ty, tw, th, i===2?PANEL2:PANEL);
  s.addShape(pres.shapes.RECTANGLE, { x:x+0.45, y:ty+0.5, w:0.34, h:0.05, fill:{color:VIOLET}, line:{type:"none"} });
  s.addText(c[0], { x:x+0.45, y:ty+0.65, w:tw-0.9, h:0.55, fontFace:F, fontSize:18.5, color:TXT, bold:true, valign:"top", margin:0 });
  s.addText(c[1], { x:x+0.45, y:ty+1.35, w:tw-0.85, h:1.6, fontFace:F, fontSize:14, color:MUTE, valign:"top", lineSpacingMultiple:1.25, margin:0 });
});
s.addText("→ 추출 텐션을 키우고, 손목시계(현상 측정기)의 본래 용도를 게임으로 선체험하게 한다.",
  { x:0.9, y:6.42, w:11.5, h:0.45, fontFace:F, fontSize:13, color:VIOLET, italic:true, align:"center", margin:0 });

// ============================================================ 8. 플레이어 = 회수꾼 (loop)
s = pres.addSlide(); slideBg(s);
eyebrow(s, 0.9, 0.7, "플레이어");
title(s, 0.9, 1.12, "당신은 회수꾼 — 매일 밤 벽을 넘는다", 29);
s.addText("기억도 이름도 없이 무법지대에서 깨어난 주인공. 가진 건 낡은 손목시계 하나. 그 시계가 무언가를 가리킨다.",
  { x:0.9, y:1.95, w:11.7, h:0.5, fontFace:F, fontSize:15, color:MUTE, margin:0 });

const loop = [
  ["안전가옥", "장비·가방을 꾸리고 봉쇄선으로 향한다."],
  ["봉쇄선 침투", "어둠 속 폐도시 — 탐색·전투·파밍, 15분의 압박."],
  ["루디 회수", "위험을 무릅쓰고 캐낸 뒤, 탈출 지점으로."],
  ["귀환 · 성장", "전당포 거래 → 안전가옥 강화 → 새 정보·지역 해금."],
];
const ly=3.05, lw=2.78, lgap=0.41, lh=2.7; let lxx=0.9;
loop.forEach((c,i)=>{
  const x = lxx + i*(lw+lgap);
  card(s, x, ly, lw, lh);
  s.addShape(pres.shapes.OVAL, { x:x+0.4, y:ly+0.4, w:0.6, h:0.6, fill:{type:"none"}, line:{color:AMBER, width:2} });
  s.addText(String(i+1), { x:x+0.4, y:ly+0.4, w:0.6, h:0.6, fontFace:F, fontSize:20, color:AMBER, bold:true, align:"center", valign:"middle", margin:0 });
  s.addText(c[0], { x:x+1.12, y:ly+0.42, w:lw-1.3, h:0.6, fontFace:F, fontSize:17, color:TXT, bold:true, valign:"middle", margin:0 });
  s.addText(c[1], { x:x+0.4, y:ly+1.2, w:lw-0.75, h:1.3, fontFace:F, fontSize:13, color:MUTE, valign:"top", lineSpacingMultiple:1.2, margin:0 });
  if(i<loop.length-1) s.addText("→", { x:x+lw-0.05, y:ly+0.95, w:0.5, h:0.6, fontFace:F, fontSize:24, color:MUTE, align:"center", valign:"middle", margin:0 });
});
s.addText("반복마다 묻는다:  조금 더 파밍할까, 지금 탈출할까?",
  { x:0.9, y:6.2, w:11.7, h:0.5, fontFace:F, fontSize:16, color:AMBER, bold:true, align:"center", valign:"middle", margin:0 });

// ============================================================ 9. 미스터리 & 다녀올게 (closing)
s = pres.addSlide(); slideBg(s);
glow(s, 2.0, 1.0, 6.0, AMBER, 90);
glow(s, 11.5, 6.8, 6.0, VIOLET, 90);
eyebrow(s, 0.9, 0.75, "그리고 — 왜 들어가는가");
s.addText("시계 · 동생 · 전당포 주인", { x:0.9, y:1.25, w:11.5, h:0.8, fontFace:F, fontSize:34, color:TXT, bold:true, margin:0 });
s.addText("루디를 캐는 진짜 이유는 따로 있다. 멈춘 시계는 어둠의 중심을 가리키고, 그곳엔 시간이 비틀린 채 사라진 동생이 있을지도 모른다. 전당포 주인은 주인공의 과거를 알지만, 정보는 공짜가 아니다.",
  { x:0.9, y:2.15, w:11.0, h:1.2, fontFace:F, fontSize:15.5, color:MUTE, valign:"top", lineSpacingMultiple:1.3, margin:0 });

// "다녀올게" 3중 의미
card(s, 0.9, 3.7, 11.53, 2.05, PANEL);
s.addText("“다녀올게” 의 세 가지 무게", { x:1.25, y:3.95, w:10.8, h:0.4, fontFace:F, fontSize:15, color:AMBER, bold:true, margin:0 });
const three = [
  ["매 출격", "금방 올게 — 무심히 반복되는 인사"],
  ["과거", "동생에게 남기고 돌아오지 못한 말"],
  ["엔딩", "마침내 지켜지는 / 지켜지지 못한 약속"],
];
let mx=1.25, mw=3.62, mgap=0.32;
three.forEach((c,i)=>{
  const x = mx + i*(mw+mgap);
  s.addText(c[0], { x, y:4.5, w:mw, h:0.35, fontFace:F, fontSize:14, color:TXT, bold:true, margin:0 });
  s.addText(c[1], { x, y:4.88, w:mw, h:0.75, fontFace:F, fontSize:12.5, color:MUTE, valign:"top", lineSpacingMultiple:1.15, margin:0 });
  if(i<2) s.addShape(pres.shapes.LINE, { x:x+mw+0.16, y:4.55, w:0, h:0.95, line:{color:LINE, width:1} });
});
s.addText([{text:"“다녀올게.”", options:{italic:true}}], { x:0.9, y:6.25, w:11.5, h:0.7, fontFace:F, fontSize:26, color:AMBER, bold:true, align:"center", margin:0 });

pres.writeFile({ fileName: "다녀올게-세계관.pptx" }).then(f=>console.log("WROTE", f));
