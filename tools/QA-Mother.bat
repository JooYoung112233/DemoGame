@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0.."

REM ============================================================
REM  QA 마더 — 차일드 관리 콘솔 (더블클릭 실행)
REM
REM  마더 1개가 차일드(QA 인스턴스) N개를 지휘한다.
REM    • 차일드 = Unity 에디터(F9) 또는 빌드 exe(-qa-serve)
REM    • 빌드는 여러 개 동시 실행 가능 (-qa-instance=A/B/C 로 파일이 갈림)
REM    • 등록/추가/삭제는 마더 창에서 (tools\qa-instances.json)
REM
REM  마더는 차일드가 떨군 파일(qa-status/result/report)을 1초마다 스스로 읽는다.
REM  사람이 로그를 옮겨 붙일 필요 없음.
REM ============================================================

REM 파이썬 찾기 — 윈도우는 py 런처가 정석(python은 Store 스텁일 수 있음)
REM pyw = 콘솔 창 없이 GUI만 뜬다
where pyw >nul 2>&1 && (set "PYW=pyw") || (set "PYW=pythonw")
where py  >nul 2>&1 && (set "PY=py")   || (set "PY=python")

start "" %PYW% "%~dp0qa_mother_gui.py"

REM GUI가 안 뜨면(파이썬/tkinter 문제) 콘솔로 원인을 보여준다
timeout /t 2 >nul
tasklist /FI "IMAGENAME eq pythonw.exe" 2>nul | find /I "pythonw.exe" >nul
if errorlevel 1 (
    echo.
    echo [!] GUI가 뜨지 않았습니다. 아래 오류를 확인하세요.
    echo.
    %PY% "%~dp0qa_mother_gui.py"
    pause
)

endlocal
