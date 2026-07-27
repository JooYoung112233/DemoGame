@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0.."

REM ============================================================
REM  QA 마더 — 총괄 콘솔 (더블클릭 실행)
REM
REM  마더 1개가 차일드(QA 인스턴스) N개를 지휘한다.
REM    • 차일드 = Unity 에디터(F9) 또는 빌드 exe(-qa-serve)
REM    • 빌드는 여러 개 동시 실행 가능 (-qa-instance=A/B/C 로 파일이 갈림)
REM    • 등록은 tools\qa-instances.json
REM ============================================================

REM 파이썬 찾기 — Windows는 py 런처가 정석(python은 Store 스텁일 수 있음)
where py >nul 2>&1 && (set "PY=py") || (set "PY=python")

:menu
cls
echo ============================================================
echo   QA 마더 - 총괄 콘솔
echo ============================================================
%PY% tools\qa_orchestrator.py status
echo.
echo ------------------------------------------------------------
echo   1) 상태 새로고침
echo   2) 결과 집계 (PASS/FAIL, 문제 좌표)
echo   3) 시나리오 투입 - 표준 순환
echo   4) 시나리오 투입 - 신규 유저(튜토리얼)
echo   5) 끝날 때까지 대기
echo   6) 커버리지(사각지대) 보기
echo   7) 빌드 차일드 기동  /  8) 차일드 종료
echo   9) 대시보드 열기 (브라우저)
echo   0) 종료
echo ------------------------------------------------------------
set /p sel="선택: "

if "%sel%"=="1" goto menu
if "%sel%"=="2" goto collect
if "%sel%"=="3" goto run_default
if "%sel%"=="4" goto run_tutorial
if "%sel%"=="5" goto wait
if "%sel%"=="6" goto coverage
if "%sel%"=="7" goto launch
if "%sel%"=="8" goto kill
if "%sel%"=="9" goto dash
if "%sel%"=="0" goto end
goto menu

:collect
cls & %PY% tools\qa_orchestrator.py collect & pause & goto menu

:run_default
set /p cyc="사이클 수 (엔터=시나리오 기본): "
if "%cyc%"=="" (%PY% tools\qa_orchestrator.py run --scenario default --vary-seed) else (%PY% tools\qa_orchestrator.py run --scenario default --cycles %cyc% --vary-seed)
pause & goto menu

:run_tutorial
%PY% tools\qa_orchestrator.py run --scenario tutorial --vary-seed & pause & goto menu

:wait
%PY% tools\qa_orchestrator.py wait --timeout 900 & pause & goto menu

:coverage
cls & %PY% tools\qa_orchestrator.py coverage & pause & goto menu

:launch
%PY% tools\qa_orchestrator.py launch & pause & goto menu

:kill
%PY% tools\qa_orchestrator.py kill & pause & goto menu

:dash
start "" powershell -ExecutionPolicy Bypass -File "%~dp0qa-dashboard.ps1"
timeout /t 2 >nul
start "" http://localhost:8787
goto menu

:end
endlocal
