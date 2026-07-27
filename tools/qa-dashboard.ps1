<#
  QA 대시보드 — 외부 GUI (인게임 아님)

  게임(QaBot)이 남긴 JSON을 읽어 브라우저에 보여준다.
    • 통과/미통과(PASS/FAIL) 통계 — 런 누적
    • 실행 중 인스턴스 상태(하트비트)
    • 이상 목록 · 사이클 지표 · 공간(파밍효율/스턱) 데이터

  실행:
      powershell -ExecutionPolicy Bypass -File tools\qa-dashboard.ps1
      → http://localhost:8787 열기

  의존성 없음(윈도우 기본 PowerShell). 게임·Unity와 무관하게 따로 뜬다.
#>

param(
  [int]$Port = 8787,
  [string]$DataDir = ""
)

# ── QA 파일 위치 (Unity persistentDataPath) ────────────────────────────
if (-not $DataDir) {
  $company = "Studio Pod Games"
  $low = Join-Path $env:USERPROFILE "AppData\LocalLow\$company"
  if (Test-Path $low) {
    $prod = Get-ChildItem $low -Directory | Select-Object -First 1
    if ($prod) { $DataDir = $prod.FullName }
  }
}
if (-not $DataDir -or -not (Test-Path $DataDir)) {
  Write-Host "[QA] 데이터 폴더를 못 찾음. -DataDir 로 직접 지정하세요." -ForegroundColor Yellow
  Write-Host "     예: -DataDir `"$env:USERPROFILE\AppData\LocalLow\Studio Pod Games\BRB`"" -ForegroundColor Yellow
  if (-not $DataDir) { $DataDir = (Get-Location).Path }
}

Write-Host "[QA] 대시보드 시작" -ForegroundColor Cyan
Write-Host "     데이터: $DataDir"
Write-Host "     주소  : http://localhost:$Port"
Write-Host "     중지  : Ctrl+C"

# ── 데이터 수집 → JSON ─────────────────────────────────────────────────
function Get-QaSnapshot {
  $res = @{ dataDir = $DataDir; runs = @(); status = @(); updated = (Get-Date -Format "HH:mm:ss") }

  # 결과 파일들(런 히스토리) — latest 제외, 최신 40개
  Get-ChildItem -Path $DataDir -Filter "*qa-result-*.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "*latest*" } |
    Sort-Object LastWriteTime -Descending | Select-Object -First 40 | ForEach-Object {
      try {
        $j = Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        $res.runs += [pscustomobject]@{
          file      = $_.Name
          at        = $_.LastWriteTime.ToString("MM-dd HH:mm")
          instance  = $j.instance
          scenario  = $j.scenario
          verdict   = $j.verdict
          reason    = $j.verdictReason
          errors    = $j.errorCount
          warns     = $j.warnCount
          cycles    = "$($j.cyclesCompleted)/$($j.cyclesPlanned)"
          duration  = [math]::Round($j.durationSec,0)
          checks    = $j.checks
          anomalies = $j.anomalies
          cellCount = @($j.cells).Count
          cells     = $j.cells
          metrics   = $j.cycles
        }
      } catch { }
    }

  # 실행 중 인스턴스(하트비트)
  Get-ChildItem -Path $DataDir -Filter "*qa-status.json" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
      $j = Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
      $age = ((Get-Date) - $_.LastWriteTime).TotalSeconds
      $res.status += [pscustomobject]@{
        instance = $(if ($j.instance) { $j.instance } else { "(기본)" })
        state    = $j.state; step = $j.step; cycle = $j.cycle; detail = $j.detail
        scene    = $j.scene; money = $j.money; level = $j.level
        elapsed  = [math]::Round($j.elapsedSec,0)
        errors   = $j.errors; warns = $j.warns
        stale    = ($age -gt 10)
        ageSec   = [math]::Round($age,0)
      }
    } catch { }
  }
  return ($res | ConvertTo-Json -Depth 8 -Compress)
}

# ── HTML ───────────────────────────────────────────────────────────────
$html = @'
<!doctype html><html lang="ko"><head><meta charset="utf-8">
<title>QA 대시보드</title>
<style>
 :root{--bg:#14140f;--panel:#1e1e17;--line:#3a3a2c;--tx:#e8e4d8;--mut:#8a8272;--ok:#4caf50;--ng:#e05c5c;--wr:#d9a441;}
 *{box-sizing:border-box} body{margin:0;background:var(--bg);color:var(--tx);font:13px/1.5 "Malgun Gothic",system-ui,sans-serif}
 header{padding:12px 18px;border-bottom:1px solid var(--line);display:flex;gap:18px;align-items:center;flex-wrap:wrap;position:sticky;top:0;background:var(--bg);z-index:5}
 h1{font-size:15px;margin:0;color:#d8c98a} .mut{color:var(--mut)}
 .wrap{padding:14px 18px;display:grid;gap:14px;grid-template-columns:1fr;max-width:1500px}
 .card{background:var(--panel);border:1px solid var(--line);border-radius:6px;padding:12px 14px}
 .card h2{font-size:13px;margin:0 0 10px;color:#c9bd86;font-weight:600}
 .kpi{display:flex;gap:22px;flex-wrap:wrap} .kpi div{min-width:78px}
 .kpi b{display:block;font-size:22px;line-height:1.2} .kpi span{color:var(--mut);font-size:11px}
 table{width:100%;border-collapse:collapse} th,td{text-align:left;padding:5px 7px;border-bottom:1px solid #2a2a20;vertical-align:top}
 th{color:var(--mut);font-weight:500;font-size:11px} tr:hover td{background:#232319}
 .pass{color:var(--ok);font-weight:700}.fail{color:var(--ng);font-weight:700}
 .pill{display:inline-block;padding:1px 7px;border-radius:9px;font-size:11px;border:1px solid var(--line)}
 .run{color:var(--ok)}.idle{color:var(--mut)}.blocked{color:var(--ng)}.stale{opacity:.45}
 .bar{height:7px;background:#2c2c22;border-radius:4px;overflow:hidden;min-width:110px}
 .bar>i{display:block;height:100%;background:var(--ok)}
 .chk{display:flex;gap:6px;flex-wrap:wrap;margin-top:4px}
 .chk span{font-size:11px;padding:1px 6px;border-radius:3px;border:1px solid var(--line)}
 .chk .y{color:var(--ok);border-color:#2c4a2c}.chk .n{color:var(--ng);border-color:#4a2c2c;background:#2a1a1a}
 code{color:#b8ad7e} .small{font-size:11px}
 details>summary{cursor:pointer;color:var(--mut);font-size:11px;margin-top:5px}
 .grid2{display:grid;grid-template-columns:1fr 1fr;gap:14px}
 @media(max-width:1000px){.grid2{grid-template-columns:1fr}}
 .hm{font:10px/1 ui-monospace,Consolas,monospace;white-space:pre;overflow:auto;background:#111;padding:8px;border-radius:4px}
 .tab{background:#242419;color:var(--mut);border:1px solid var(--line);border-radius:4px;padding:5px 14px;cursor:pointer;font:inherit}
 .tab.on{background:#3a3520;color:#e8d9a0;border-color:#5a5030}
 .st{display:inline-block;padding:1px 7px;border-radius:3px;font-size:11px;font-weight:600}
 .st-passed{color:var(--ok);border:1px solid #2c4a2c}
 .st-untested{color:var(--wr);border:1px solid #4a4229}
 .st-partial{color:#9ac;border:1px solid #2c3a4a}
 .st-blind{color:var(--ng);border:1px solid #4a2c2c;background:#2a1a1a}
 .st-notimpl{color:var(--mut);border:1px solid var(--line)}
</style></head><body>
<header>
  <h1>🎮 QA 대시보드</h1>
  <span class="mut" id="dir"></span>
  <span class="mut">갱신 <b id="upd">-</b></span>
  <label class="mut"><input type="checkbox" id="auto" checked> 자동새로고침(3초)</label>
  <span style="flex:1"></span>
  <button class="tab on" data-t="run">결과</button>
  <button class="tab" data-t="sys">QA 시스템</button>
</header>

<div class="wrap" id="tab-run">
  <div class="card"><h2>종합</h2><div class="kpi" id="kpi"></div></div>
  <div class="card"><h2>실행 중 (QA 프로그램 인스턴스)</h2><div id="live"></div></div>
  <div class="grid2">
    <div class="card"><h2>런 히스토리 — 통과/미통과</h2><div id="runs"></div></div>
    <div class="card"><h2>상세</h2><div id="detail" class="mut">런을 클릭하세요.</div></div>
  </div>
</div>

<div class="wrap" id="tab-sys" style="display:none">
  <div class="card"><h2>시스템 커버리지 — QA가 아는 것 / 모르는 것</h2>
    <div class="kpi" id="covkpi"></div>
    <div id="cov" style="margin-top:10px"></div>
  </div>
  <div class="grid2">
    <div class="card"><h2>봇이 할 수 있는 동작 (op)</h2><div id="ops"></div></div>
    <div class="card">
      <h2>통과 판정 항목</h2><div id="checks"></div>
      <h2 style="margin-top:14px">구조적 한계</h2><ul id="limits" class="small mut"></ul>
    </div>
  </div>
</div>
<script>
let DATA=null, sel=null;
async function load(){
  try{ const r=await fetch('/data?'+Date.now()); DATA=await r.json(); render(); }catch(e){}
}
function render(){
  document.getElementById('dir').textContent=DATA.dataDir;
  document.getElementById('upd').textContent=DATA.updated;
  const runs=DATA.runs||[];
  const pass=runs.filter(r=>r.verdict==='PASS').length, fail=runs.length-pass;
  const rate=runs.length?Math.round(pass*100/runs.length):0;
  document.getElementById('kpi').innerHTML=
    `<div><b>${runs.length}</b><span>런</span></div>
     <div><b class="pass">${pass}</b><span>통과</span></div>
     <div><b class="fail">${fail}</b><span>미통과</span></div>
     <div><b>${rate}%</b><span>통과율</span></div>
     <div style="flex:1;min-width:180px"><span>통과율</span><div class="bar"><i style="width:${rate}%"></i></div></div>`;

  const st=DATA.status||[];
  document.getElementById('live').innerHTML = st.length? `<table><tr><th>인스턴스</th><th>상태</th><th>단계</th><th>사이클</th><th>씬</th><th>소지금</th><th>Lv</th><th>경과</th><th>E/W</th></tr>`+
    st.map(s=>`<tr class="${s.stale?'stale':''}"><td>${s.instance}</td>
      <td class="${s.state==='running'?'run':s.state==='blocked'?'blocked':'idle'}">${s.state}${s.stale?' <span class="small">(끊김 '+s.ageSec+'s)</span>':''}</td>
      <td>${s.step||'-'}<div class="small mut">${s.detail||''}</div></td><td>${s.cycle}</td><td>${s.scene}</td>
      <td>${s.money}</td><td>${s.level}</td><td>${s.elapsed}s</td><td>${s.errors}/${s.warns}</td></tr>`).join('')+`</table>`
    : '<span class="mut">실행 중인 인스턴스 없음 — 게임을 <code>-qa-serve</code>로 띄우세요.</span>';

  document.getElementById('runs').innerHTML = runs.length? `<table><tr><th>시각</th><th>판정</th><th>시나리오</th><th>사이클</th><th>E/W</th><th>시간</th></tr>`+
    runs.map((r,i)=>`<tr onclick="pick(${i})" style="cursor:pointer">
      <td>${r.at}<div class="small mut">${r.instance||''}</div></td>
      <td class="${r.verdict==='PASS'?'pass':'fail'}">${r.verdict}</td>
      <td>${r.scenario||'-'}<div class="small mut">${r.verdict==='FAIL'?(r.reason||''):''}</div></td>
      <td>${r.cycles}</td><td>${r.errors}/${r.warns}</td><td>${r.duration}s</td></tr>`).join('')+`</table>`
    : '<span class="mut">아직 결과 없음.</span>';
  if(sel!==null&&runs[sel]) detail(runs[sel]);
}
function pick(i){ sel=i; detail(DATA.runs[i]); }
function detail(r){
  const chk=(r.checks||[]).map(c=>`<span class="${c.passed?'y':'n'}">${c.passed?'✔':'✘'} ${c.name} <em class="mut">${c.detail||''}</em></span>`).join('');
  const an=(r.anomalies||[]).slice(0,60).map(a=>`<tr><td class="${a.level==='Error'?'fail':''}">${a.level}</td><td><code>${a.kind}</code></td><td>${a.step}</td><td>${a.msg}</td></tr>`).join('');
  const cy=(r.metrics||[]).map(c=>`<tr><td>${c.cycle}</td><td>${c.moneyStart}→${c.moneyEnd}</td><td>${c.levelStart}→${c.levelEnd}</td>
    <td>${c.lootValue}</td><td>${c.cratesOpened}</td><td>${Math.round(c.durationSec)}s</td><td>${c.raidCompleted?'O':'X'}</td></tr>`).join('');
  // 공간: 파밍 효율 최악/최고
  const cells=(r.cells||[]).filter(c=>c.dwellSec>=3);
  cells.sort((a,b)=>a.valuePerMin-b.valuePerMin);
  const worst=cells.slice(0,6).map(c=>`<tr><td>${c.scene}</td><td>(${Math.round(c.x)}, ${Math.round(c.y)})</td><td>${c.valuePerMin.toFixed(1)}/분</td><td>${Math.round(c.dwellSec)}s</td><td>${c.lootValue}</td></tr>`).join('');
  const bad=(r.cells||[]).filter(c=>c.stuck>0||c.unreachable>0||c.deaths>0)
    .map(c=>`<tr><td>${c.scene}</td><td>(${Math.round(c.x)}, ${Math.round(c.y)})</td><td>${c.stuck}</td><td>${c.unreachable}</td><td>${c.deaths}</td></tr>`).join('');
  document.getElementById('detail').innerHTML=`
    <div><b class="${r.verdict==='PASS'?'pass':'fail'}">${r.verdict}</b> <span class="mut">${r.reason||''}</span></div>
    <div class="chk">${chk}</div>
    <details open><summary>사이클 지표</summary><table><tr><th>#</th><th>소지금</th><th>Lv</th><th>루팅</th><th>상자</th><th>시간</th><th>완주</th></tr>${cy||'<tr><td colspan=7 class=mut>없음</td></tr>'}</table></details>
    <details><summary>이상 (${(r.anomalies||[]).length})</summary><table><tr><th>lv</th><th>kind</th><th>step</th><th>내용</th></tr>${an||'<tr><td colspan=4 class=mut>없음</td></tr>'}</table></details>
    <details open><summary>⛔ 문제 지점 (스턱/길막힘/사망)</summary><table><tr><th>씬</th><th>좌표</th><th>스턱</th><th>길막힘</th><th>사망</th></tr>${bad||'<tr><td colspan=5 class=mut>없음</td></tr>'}</table></details>
    <details><summary>💤 파밍 효율 낮은 구역 (재미없는 파밍 후보)</summary><table><tr><th>씬</th><th>좌표</th><th>효율</th><th>체류</th><th>획득</th></tr>${worst||'<tr><td colspan=5 class=mut>표본 부족</td></tr>'}</table></details>`;
}
// ── 탭 ──
document.querySelectorAll('.tab').forEach(b=>b.onclick=()=>{
  document.querySelectorAll('.tab').forEach(x=>x.classList.toggle('on',x===b));
  document.getElementById('tab-run').style.display = b.dataset.t==='run'?'':'none';
  document.getElementById('tab-sys').style.display = b.dataset.t==='sys'?'':'none';
});

// ── QA 시스템 뷰 (tools/qa-manifest.json = 단일 진실원) ──
const LABEL={passed:'검증됨',untested:'미실행',partial:'부분',blind:'사각(op 없음)',notimpl:'게임 미구현'};
async function loadManifest(){
  let M; try{ M=await (await fetch('/manifest?'+Date.now())).json(); }catch(e){ return; }
  if(!M||!M.coverage) return;

  const cov=M.coverage, n=cov.length;
  const cnt=s=>cov.filter(c=>c.status===s).length;
  const covered=cnt('passed')+cnt('untested')+cnt('partial');
  document.getElementById('covkpi').innerHTML=
    `<div><b>${n}</b><span>게임 시스템</span></div>
     <div><b class="pass">${cnt('passed')}</b><span>검증됨</span></div>
     <div><b style="color:#d9a441">${cnt('untested')}</b><span>미실행</span></div>
     <div><b style="color:#9ac">${cnt('partial')}</b><span>부분</span></div>
     <div><b class="fail">${cnt('blind')}</b><span>사각</span></div>
     <div style="flex:1;min-width:180px"><span>커버(op 있음) ${Math.round(covered*100/n)}%</span>
       <div class="bar"><i style="width:${covered*100/n}%"></i></div></div>`;

  document.getElementById('cov').innerHTML=`<table><tr><th>#</th><th>게임 시스템</th><th>검증 수단(op)</th><th>상태</th><th>비고</th></tr>`+
    cov.map(c=>`<tr><td class="mut">${c.id}</td><td>${c.system}</td>
      <td><code>${c.op||'—'}</code></td>
      <td><span class="st st-${c.status}">${LABEL[c.status]||c.status}</span></td>
      <td class="small mut">${c.note||''}</td></tr>`).join('')+`</table>`;

  document.getElementById('ops').innerHTML=`<table><tr><th>op</th><th>분류</th><th>파라미터</th><th>하는 일</th></tr>`+
    (M.ops||[]).map(o=>`<tr><td><code>${o.op}</code></td><td class="mut">${o.group}</td>
      <td class="small mut">${o.params}</td><td class="small">${o.does}</td></tr>`).join('')+`</table>`;

  document.getElementById('checks').innerHTML=`<table><tr><th>판정 항목</th><th>실패 조건</th></tr>`+
    (M.checks||[]).map(k=>`<tr><td>${k.name}</td><td class="small mut">${k.fails}</td></tr>`).join('')+`</table>`;

  document.getElementById('limits').innerHTML=(M.limits||[]).map(l=>`<li>${l}</li>`).join('');
}
loadManifest();

setInterval(()=>{ if(document.getElementById('auto').checked) load(); },3000);
load();
</script></body></html>
'@

# ── HTTP 서버 ──────────────────────────────────────────────────────────
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
try { $listener.Start() } catch {
  Write-Host "[QA] 포트 $Port 사용 불가. -Port 로 다른 값을 주세요." -ForegroundColor Red; exit 1
}

try {
  while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    $path = $ctx.Request.Url.AbsolutePath
    if ($path -eq "/manifest") {
      $mf = Join-Path $PSScriptRoot "qa-manifest.json"
      if (Test-Path $mf) { $body = Get-Content $mf -Raw -Encoding UTF8 } else { $body = "{}" }
      $ctx.Response.ContentType = "application/json; charset=utf-8"
    } elseif ($path -eq "/data") {
      $body = Get-QaSnapshot
      $ctx.Response.ContentType = "application/json; charset=utf-8"
    } else {
      $body = $html
      $ctx.Response.ContentType = "text/html; charset=utf-8"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes($body)
    $ctx.Response.ContentLength64 = $bytes.Length
    $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    $ctx.Response.OutputStream.Close()
  }
} finally { $listener.Stop() }
