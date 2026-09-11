#!/usr/bin/env python3
"""
QA 마더 (GUI) — 차일드 관리 콘솔.

  마더(이 창) ──명령파일──▶ 차일드 N개 (Unity 에디터 / 빌드 exe)
              ◀─상태·결과─┘   (qa-status.json / qa-result-*.json / qa-report-*.txt)

사람이 로그를 옮겨 붙일 필요가 없다. 마더가 1초마다 차일드가 떨군 파일을
직접 읽어서 목록·판정·이상·원본 로그까지 전부 화면에 띄운다.

의존성 없음(파이썬 표준 tkinter). qa_orchestrator.py의 함수를 그대로 쓴다.
실행: tools/QA-Mother.bat  또는  py tools/qa_mother_gui.py
"""

import json
import os
import queue
import shutil
import subprocess
import sys
import threading
import time
from datetime import datetime
from pathlib import Path

import tkinter as tk
from tkinter import ttk, messagebox, filedialog

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import qa_orchestrator as orch  # noqa: E402
import qa_tuning  # noqa: E402

REPO = HERE.parent
AGENT_REQUEST = HERE / "qa-agent-request.md"
LOG_FILE = HERE / "qa-mother.log"      # 마더가 무엇을 지시했는지 남는 감사 기록

POLL_SEC = 1.0
FONT_UI = ("맑은 고딕", 9)
FONT_UI_B = ("맑은 고딕", 9, "bold")
FONT_MONO = ("Consolas", 9)          # ASCII 히트맵 정렬용(한글은 폰트 링크로 대체됨)
CLAUDE_REPORT = HERE / "qa-report-for-claude.md"

# ── 다크 팔레트 ──────────────────────────────────────────────
BG = "#1b1e24"        # 창 배경
BG2 = "#22262e"       # 패널
BG3 = "#2b303a"       # 입력·선택
BG_SEL = "#33507a"    # 선택 행
BORDER = "#39404d"
FG = "#d6dae2"
FG_DIM = "#8a919e"
ACCENT = "#5aa2ff"

# 상태별 색 (어두운 배경 위에서 읽히도록 밝은 톤)
C_RUN = "#4ade80"
C_PASS = "#4ade80"
C_DEAD = "#6b7280"
C_FAIL = "#f87171"
C_WARN = "#fbbf24"
C_BLOCK = "#fbbf24"
C_EDIT = "#c084fc"    # 저장 대기 중인 밸런스 수정


# ─────────────────────────────────────────────────────────────
#  감시 스레드 — 파일만 읽는다(게임에 아무 영향 없음)
# ─────────────────────────────────────────────────────────────

class Watcher(threading.Thread):
    def __init__(self, q: queue.Queue):
        super().__init__(daemon=True)
        self.q = q
        self.stop_flag = threading.Event()
        self.paused = threading.Event()
        self._result_sig = None

    def run(self):
        while not self.stop_flag.is_set():
            if not self.paused.is_set():
                try:
                    self.tick()
                except Exception as e:  # 감시는 절대 죽으면 안 된다
                    self.q.put(("error", f"{type(e).__name__}: {e}"))
            self.stop_flag.wait(POLL_SEC)

    def tick(self):
        cfg = orch.load_instances()
        self.q.put(("cfg", cfg))
        rows = orch.status_rows(cfg)
        self.q.put(("status", rows))

        # 결과 파일이 바뀌었을 때만 무거운 집계를 돈다
        d = orch.data_dir(cfg)

        # 명령 파일이 사라졌다 = 차일드가 집어갔다 (지시가 먹혔는지 확인용)
        pending = {r["name"]: (d / orch.fname(r["name"], "qa-command.json")).exists()
                   for r in rows}
        self.q.put(("cmdfiles", pending))
        sig = None
        if d.exists():
            files = list(d.glob("*qa-result-*.json"))
            sig = (len(files), max((f.stat().st_mtime for f in files), default=0))
        if sig != self._result_sig:
            self._result_sig = sig
            self.q.put(("results", orch.collect_summary(cfg, limit=30)))
            self.q.put(("reports", list_reports(d)))


def list_reports(d: Path):
    """차일드가 떨군 사람이 읽는 리포트(전체 로그) 목록 — 최신순."""
    if not d.exists():
        return []
    out = []
    for p in sorted(d.glob("*qa-report-*.txt"), key=lambda x: x.stat().st_mtime, reverse=True)[:40]:
        out.append({
            "file": str(p),
            "name": p.name,
            "at": datetime.fromtimestamp(p.stat().st_mtime).strftime("%m-%d %H:%M:%S"),
            "sizeKB": round(p.stat().st_size / 1024, 1),
        })
    return out


def save_instances(cfg):
    orch.INSTANCES_FILE.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding="utf-8")


def list_agents():
    """.claude/agents/*.md — QA 관련을 앞에 둔다."""
    d = REPO / ".claude" / "agents"
    names = sorted(p.stem for p in d.glob("*.md")) if d.exists() else []
    head = [n for n in ("dev-fixer", "qa-runner") if n in names]
    return head + [n for n in names if n not in head] or ["dev-fixer"]


def num(v):
    """None(파싱 실패)도 안전하게 표시."""
    return "?" if v is None else f"{v:g}"


def claude_exe():
    """윈도우에서는 claude.cmd — which가 PATHEXT까지 훑어 잡아준다."""
    return shutil.which("claude") or shutil.which("claude.cmd")


# ─────────────────────────────────────────────────────────────
#  차일드 편집 대화상자
# ─────────────────────────────────────────────────────────────

class ChildDialog(tk.Toplevel):
    def __init__(self, master, inst=None, taken=()):
        super().__init__(master)
        self.title("차일드 편집" if inst else "차일드 추가")
        self.transient(master)
        self.grab_set()
        self.resizable(False, False)
        self.result = None
        inst = inst or {}

        # 이름 자동 제안 — 이미 쓰는 접두는 피한다
        suggest = inst.get("name", "")
        if not suggest and not inst:
            for ch in "ABCDEFGHIJKLMNOP":
                if ch not in taken:
                    suggest = ch
                    break

        f = ttk.Frame(self, padding=12)
        f.pack(fill="both", expand=True)

        self.v_name = tk.StringVar(value=suggest)
        self.v_kind = tk.StringVar(value=inst.get("kind", "build"))
        self.v_label = tk.StringVar(value=inst.get("label", ""))
        self.v_exe = tk.StringVar(value=inst.get("exePath", ""))
        self.v_args = tk.StringVar(value=" ".join(inst.get("extraArgs", ["-qa-serve"])))

        r = 0
        ttk.Label(f, text="이름(파일 접두)", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        ttk.Entry(f, textvariable=self.v_name, width=12, font=FONT_UI).grid(row=r, column=1, sticky="w")
        ttk.Label(f, text="A → A-qa-command.json (비우면 에디터 기본)",
                  font=FONT_UI, foreground=FG_DIM).grid(row=r, column=2, sticky="w", padx=6)
        r += 1

        ttk.Label(f, text="종류", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        cb = ttk.Combobox(f, textvariable=self.v_kind, values=["build", "editor"],
                          width=10, state="readonly", font=FONT_UI)
        cb.grid(row=r, column=1, sticky="w")
        ttk.Label(f, text="build=마더가 직접 실행 / editor=사람이 F9",
                  font=FONT_UI, foreground=FG_DIM).grid(row=r, column=2, sticky="w", padx=6)
        r += 1

        ttk.Label(f, text="표시 이름", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        ttk.Entry(f, textvariable=self.v_label, width=38, font=FONT_UI).grid(
            row=r, column=1, columnspan=2, sticky="w")
        r += 1

        ttk.Label(f, text="빌드 exe", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        ttk.Entry(f, textvariable=self.v_exe, width=48, font=FONT_UI).grid(row=r, column=1, columnspan=2, sticky="w")
        ttk.Button(f, text="찾기", width=6, command=self.browse).grid(row=r, column=3, padx=4)
        r += 1

        ttk.Label(f, text="실행 인자", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        ttk.Entry(f, textvariable=self.v_args, width=48, font=FONT_UI).grid(
            row=r, column=1, columnspan=2, sticky="w")
        r += 1

        ttk.Label(f, text="-qa-serve = 명령 대기 모드(마더가 시나리오를 밀어 넣을 수 있음)",
                  font=FONT_UI, foreground=FG_DIM).grid(row=r, column=1, columnspan=3, sticky="w", pady=(0, 8))
        r += 1

        bar = ttk.Frame(f)
        bar.grid(row=r, column=0, columnspan=4, sticky="e", pady=(8, 0))
        ttk.Button(bar, text="확인", command=self.ok).pack(side="left", padx=3)
        ttk.Button(bar, text="취소", command=self.destroy).pack(side="left")

        self.bind("<Return>", lambda e: self.ok())
        self.bind("<Escape>", lambda e: self.destroy())

    def browse(self):
        p = filedialog.askopenfilename(title="빌드 exe 선택", filetypes=[("실행 파일", "*.exe")])
        if p:
            self.v_exe.set(p)
            if not self.v_label.get():
                self.v_label.set(f"빌드 {self.v_name.get()}")

    def ok(self):
        self.result = {
            "name": self.v_name.get().strip(),
            "kind": self.v_kind.get(),
            "label": self.v_label.get().strip() or f"차일드 {self.v_name.get().strip() or '(기본)'}",
            "exePath": self.v_exe.get().strip(),
            "extraArgs": [a for a in self.v_args.get().split() if a],
        }
        self.destroy()


# ─────────────────────────────────────────────────────────────
#  마더 창
# ─────────────────────────────────────────────────────────────

class MotherApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("QA 마더 — 차일드 관리 콘솔")
        self.geometry("1440x940")
        self.minsize(1100, 720)
        self._set_icon()

        self.cfg = orch.load_instances()
        self.rows = []
        self.summary = {}
        self.reports = []
        self.feed = {}          # 인스턴스 → 이벤트 줄 리스트
        self.seen = {}          # 인스턴스 → 이미 본 줄
        self.last_verdict_seen = None

        self.q = queue.Queue()
        self.watcher = Watcher(self.q)

        self._style()
        self._menubar()
        self._header()
        self._toolbar()
        self._banner()
        self._body()
        self._logpane()
        self._statusbar()
        self.log("시작", f"마더 기동 — 데이터 {orch.data_dir(self.cfg)}")

        self.watcher.start()
        self.after(200, self._drain)
        self.protocol("WM_DELETE_WINDOW", self._quit)

    # ── 외형 ────────────────────────────────────────────────
    def _set_icon(self):
        """작업표시줄·타이틀바 아이콘 — 마더(큰 점)와 차일드(작은 점) 도식."""
        try:
            n = 32
            img = tk.PhotoImage(width=n, height=n)
            img.put(BG2, to=(0, 0, n, n))
            cx, cy = 11, 16
            for y in range(n):
                for x in range(n):
                    if (x - cx) ** 2 + (y - cy) ** 2 <= 36:          # 마더
                        img.put(ACCENT, to=(x, y, x + 1, y + 1))
                    elif (x - 24) ** 2 + (y - 9) ** 2 <= 9:           # 차일드 2개
                        img.put(C_RUN, to=(x, y, x + 1, y + 1))
                    elif (x - 24) ** 2 + (y - 23) ** 2 <= 9:
                        img.put(C_RUN, to=(x, y, x + 1, y + 1))
            self._icon = img          # GC 방지 — 참조를 들고 있어야 아이콘이 유지된다
            self.iconphoto(True, img)
        except Exception:
            pass

    def _style(self):
        self.configure(bg=BG)
        s = ttk.Style(self)
        try:
            s.theme_use("clam")   # clam만이 색을 제대로 먹는다(vista/xpnative는 무시)
        except tk.TclError:
            pass

        s.configure(".", background=BG, foreground=FG, fieldbackground=BG3,
                    bordercolor=BORDER, font=FONT_UI)
        s.configure("TFrame", background=BG)
        s.configure("TLabel", background=BG, foreground=FG, font=FONT_UI)
        s.configure("TLabelframe", background=BG, foreground=FG_DIM, bordercolor=BORDER)
        s.configure("TLabelframe.Label", background=BG, foreground=FG_DIM, font=FONT_UI)
        s.configure("TCheckbutton", background=BG, foreground=FG, font=FONT_UI)
        s.map("TCheckbutton", background=[("active", BG)], foreground=[("active", FG)])
        s.configure("TPanedwindow", background=BG)
        s.configure("TNotebook", background=BG, bordercolor=BORDER, tabmargins=(2, 4, 2, 0))
        s.configure("TNotebook.Tab", background=BG2, foreground=FG_DIM,
                    padding=(14, 6), font=FONT_UI)
        s.map("TNotebook.Tab", background=[("selected", BG3)],
              foreground=[("selected", FG)], expand=[("selected", (0, 0, 0, 1))])

        s.configure("TButton", background=BG3, foreground=FG, font=FONT_UI,
                    padding=3, bordercolor=BORDER, focuscolor=BG3)
        s.map("TButton", background=[("active", BG_SEL), ("pressed", BG_SEL)],
              foreground=[("disabled", FG_DIM)])
        s.configure("Big.TButton", font=FONT_UI_B, padding=4, background=BG_SEL)
        s.map("Big.TButton", background=[("active", ACCENT)])

        s.configure("TEntry", fieldbackground=BG3, foreground=FG, insertcolor=FG,
                    bordercolor=BORDER, lightcolor=BORDER, darkcolor=BORDER)
        s.configure("TSpinbox", fieldbackground=BG3, foreground=FG, insertcolor=FG,
                    background=BG3, bordercolor=BORDER, arrowcolor=FG)
        s.configure("TCombobox", fieldbackground=BG3, background=BG3, foreground=FG,
                    bordercolor=BORDER, arrowcolor=FG)
        s.map("TCombobox", fieldbackground=[("readonly", BG3)],
              foreground=[("readonly", FG)], background=[("readonly", BG3)])
        # 콤보 팝업 리스트는 ttk가 아니라 tk 위젯이라 option_add로만 색이 먹는다
        self.option_add("*TCombobox*Listbox.background", BG3)
        self.option_add("*TCombobox*Listbox.foreground", FG)
        self.option_add("*TCombobox*Listbox.selectBackground", BG_SEL)
        self.option_add("*TCombobox*Listbox.selectForeground", FG)

        s.configure("TScale", background=BG, troughcolor=BG3, bordercolor=BORDER)
        s.configure("Vertical.TScrollbar", background=BG3, troughcolor=BG,
                    bordercolor=BORDER, arrowcolor=FG_DIM)
        s.configure("Horizontal.TScrollbar", background=BG3, troughcolor=BG,
                    bordercolor=BORDER, arrowcolor=FG_DIM)

        s.configure("Treeview", background=BG2, fieldbackground=BG2, foreground=FG,
                    font=FONT_UI, rowheight=23, bordercolor=BORDER)
        s.map("Treeview", background=[("selected", BG_SEL)], foreground=[("selected", FG)])
        s.configure("Treeview.Heading", background=BG3, foreground=FG_DIM,
                    font=FONT_UI_B, relief="flat")
        s.map("Treeview.Heading", background=[("active", BG_SEL)])

    def _menubar(self):
        m = tk.Menu(self, tearoff=0, bg=BG2, fg=FG, activebackground=BG_SEL,
                    activeforeground=FG, borderwidth=0)

        def sub():
            return tk.Menu(m, tearoff=0, bg=BG2, fg=FG, activebackground=BG_SEL,
                           activeforeground=FG, borderwidth=1, activeborderwidth=0)

        f = sub()
        f.add_command(label="데이터 폴더 열기", command=self.open_data_dir)
        f.add_command(label="데이터 폴더 변경…", command=self.change_data_dir)
        f.add_separator()
        f.add_command(label="Claude 보고서 생성", command=self.make_claude_report)
        f.add_command(label="활동 로그 열기", command=lambda: self.open_path(LOG_FILE))
        f.add_command(label="활동 로그 화면 지우기", command=self.clear_log)
        f.add_command(label="활동 로그 파일 비우기…", command=self.purge_log_file)
        f.add_separator()
        f.add_command(label="끝내기", command=self._quit)
        m.add_cascade(label="파일", menu=f)

        c = sub()
        c.add_command(label="차일드 추가…", command=self.add_child)
        c.add_command(label="편집…", command=self.edit_child)
        c.add_command(label="복제…", command=self.dup_child)
        c.add_command(label="삭제", command=self.del_child)
        c.add_separator()
        c.add_command(label="레지스트리 파일 열기", command=lambda: self.open_path(orch.INSTANCES_FILE))
        m.add_cascade(label="차일드", menu=c)

        r = sub()
        r.add_command(label="기동", command=self.launch)
        r.add_command(label="종료", command=self.kill)
        r.add_command(label="잔재 정리", command=self.clear_stale)
        r.add_separator()
        r.add_command(label="선택에 투입", command=lambda: self.dispatch(False))
        r.add_command(label="전체에 투입", command=lambda: self.dispatch(True))
        r.add_separator()
        r.add_command(label="막힘 응답 — 재시도", command=lambda: self.resume("retry"))
        r.add_command(label="막힘 응답 — 건너뛰기", command=lambda: self.resume("skip"))
        r.add_command(label="막힘 응답 — 중단", command=lambda: self.resume("abort"))
        m.add_cascade(label="실행", menu=r)

        t = sub()
        t.add_command(label="밸런스 저장", command=self.save_tuning)
        t.add_command(label="밸런스 다시 읽기", command=self.load_tuning)
        t.add_separator()
        t.add_command(label="문제 요약 → 지시문", command=lambda: self.gen_prompt("fix"))
        t.add_command(label="에이전트 실행", command=self.run_agent)
        t.add_command(label="에이전트 중단", command=self.stop_agent)
        t.add_separator()
        t.add_command(label="웹 대시보드 열기", command=self.open_dashboard)
        m.add_cascade(label="도구", menu=t)

        h = sub()
        h.add_command(label="QA 문서(docs/qa.md)",
                      command=lambda: self.open_path(REPO / "demo13-flashlight" / "docs" / "qa.md"))
        h.add_command(label="정보", command=lambda: messagebox.showinfo(
            "QA 마더", "QA 마더 — 차일드 관리 콘솔\n\n"
                      "차일드(Unity 에디터 / 빌드 exe)가 떨군 파일을 1초마다 읽어\n"
                      "상태·판정·이상·원본 로그를 보여주고, 시나리오를 투입한다.\n\n"
                      f"레지스트리: {orch.INSTANCES_FILE}\n활동 로그: {LOG_FILE}"))
        m.add_cascade(label="도움말", menu=h)

        self.config(menu=m)

    def _logpane(self):
        """아래 도킹된 활동 로그 — 마더가 무슨 지시를 언제 했는지 그대로 남는다."""
        wrap = ttk.Frame(self)
        wrap.pack(fill="x", padx=10, pady=(0, 2))

        bar = ttk.Frame(wrap)
        bar.pack(fill="x")
        self.v_log_open = tk.BooleanVar(value=True)
        ttk.Checkbutton(bar, text="활동 로그", variable=self.v_log_open,
                        command=self._toggle_log).pack(side="left")
        ttk.Label(bar, text=f"— 모든 지시가 {LOG_FILE.name} 에도 기록됨",
                  foreground=FG_DIM).pack(side="left", padx=6)
        ttk.Button(bar, text="화면 지우기", width=11, command=self.clear_log).pack(side="right")
        ttk.Button(bar, text="파일 열기", width=9,
                   command=lambda: self.open_path(LOG_FILE)).pack(side="right", padx=4)

        self.log_body = ttk.Frame(wrap)
        self.log_body.pack(fill="x", pady=(2, 0))
        self.tx_log = tk.Text(self.log_body, height=7, font=FONT_MONO, wrap="none",
                              bg="#15181d", fg=FG, insertbackground=FG,
                              selectbackground=BG_SEL, relief="flat", borderwidth=0,
                              highlightthickness=1, highlightbackground=BORDER)
        ys = ttk.Scrollbar(self.log_body, orient="vertical", command=self.tx_log.yview)
        self.tx_log.configure(yscrollcommand=ys.set)
        self.tx_log.pack(side="left", fill="both", expand=True)
        ys.pack(side="right", fill="y")
        self.tx_log.tag_configure("time", foreground=FG_DIM)
        self.tx_log.tag_configure("cmd", foreground=ACCENT, font=("Consolas", 9, "bold"))
        self.tx_log.tag_configure("ok", foreground=C_PASS)
        self.tx_log.tag_configure("warn", foreground=C_WARN)
        self.tx_log.tag_configure("err", foreground=C_FAIL)

    def _toggle_log(self):
        if self.v_log_open.get():
            self.log_body.pack(fill="x", pady=(2, 0))
        else:
            self.log_body.pack_forget()

    def log(self, kind, text, level=""):
        """활동 1건 — 화면 + 파일 양쪽에 남긴다."""
        now = datetime.now()
        self.tx_log.insert("end", f"[{now:%H:%M:%S}] ", "time")
        self.tx_log.insert("end", f"{kind:<8}", "cmd")
        self.tx_log.insert("end", f" {text}\n", level)
        self.tx_log.see("end")
        try:
            with open(LOG_FILE, "a", encoding="utf-8") as fp:
                fp.write(f"{now:%Y-%m-%d %H:%M:%S}\t{kind}\t{text}\n")
        except Exception:
            pass

    def clear_log(self):
        """화면만 지운다 — 지웠다는 사실도 로그로 남겨 기록에 구멍이 없게."""
        n = int(self.tx_log.index("end-1c").split(".")[0]) - 1
        if n <= 0:
            return
        if not messagebox.askyesno("활동 로그",
                                   f"화면의 로그 {n}줄을 지웁니다.\n"
                                   f"파일({LOG_FILE.name})의 기록은 그대로 남습니다.\n\n계속할까요?"):
            return
        self.tx_log.delete("1.0", "end")
        self.log("로그", f"화면 로그 {n}줄 지움 — 파일 기록은 유지", "warn")

    def purge_log_file(self):
        """파일까지 비운다 — 비웠다는 표시를 첫 줄에 남긴다."""
        size = LOG_FILE.stat().st_size if LOG_FILE.exists() else 0
        if not messagebox.askyesno("활동 로그 파일 비우기",
                                   f"{LOG_FILE}\n({size:,} 바이트)\n\n"
                                   "지금까지의 기록이 사라집니다. 비운 사실만 첫 줄에 남습니다.\n\n"
                                   "정말 비울까요?"):
            return
        try:
            with open(LOG_FILE, "w", encoding="utf-8") as fp:
                fp.write(f"{datetime.now():%Y-%m-%d %H:%M:%S}\t로그\t"
                         f"이전 기록({size:,} 바이트)을 사용자가 비움\n")
            self.log("로그", f"파일 기록 비움 ({size:,} 바이트) — 비운 사실은 남김", "warn")
        except Exception as e:
            messagebox.showerror("활동 로그", f"비우기 실패: {e}")

    def open_path(self, p):
        p = Path(p)
        if not p.exists():
            if p == LOG_FILE:
                p.write_text("", encoding="utf-8")
            else:
                messagebox.showinfo("열기", f"파일이 없습니다: {p}")
                return
        os.startfile(str(p))

    def open_dashboard(self):
        ps = HERE / "qa-dashboard.ps1"
        if not ps.exists():
            messagebox.showinfo("대시보드", f"{ps} 없음")
            return
        subprocess.Popen(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(ps)])
        self.log("대시보드", "웹 대시보드 기동 → http://localhost:8787")

    # ── git 변경 추적 — 에이전트가 실제로 뭘 고쳤는지 ────────
    def git_state(self):
        """{경로: (추가줄, 삭제줄)} — HEAD 대비 변경 + 미추적 파일."""
        state = {}
        try:
            r = subprocess.run(["git", "diff", "--numstat", "HEAD"], cwd=str(REPO),
                               capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=30)
            for line in r.stdout.splitlines():
                parts = line.split("\t")
                if len(parts) == 3:
                    state[parts[2]] = (parts[0], parts[1])
            r2 = subprocess.run(["git", "ls-files", "--others", "--exclude-standard"],
                                cwd=str(REPO), capture_output=True, text=True,
                                encoding="utf-8", errors="replace", timeout=30)
            for p in r2.stdout.splitlines():
                if p.strip():
                    state.setdefault(p.strip(), ("신규", "-"))
        except Exception:
            pass
        return state

    def _header(self):
        h = ttk.Frame(self, padding=(10, 8, 10, 4))
        h.pack(fill="x")
        ttk.Label(h, text="QA 마더", font=("맑은 고딕", 13, "bold")).pack(side="left")
        self.lb_watch = ttk.Label(h, text="● 감시중", foreground=C_RUN, font=FONT_UI_B)
        self.lb_watch.pack(side="left", padx=(10, 0))

        ttk.Button(h, text="폴더 열기", width=10, command=self.open_data_dir).pack(side="right")
        ttk.Button(h, text="변경", width=6, command=self.change_data_dir).pack(side="right", padx=4)
        self.lb_dir = ttk.Label(h, text=str(orch.data_dir(self.cfg)), foreground=FG_DIM)
        self.lb_dir.pack(side="right", padx=6)
        ttk.Label(h, text="데이터:", foreground=FG_DIM).pack(side="right")

    def _toolbar(self):
        t = ttk.Frame(self, padding=(10, 2, 10, 6))
        t.pack(fill="x")

        g1 = ttk.LabelFrame(t, text=" 차일드 ", padding=5)
        g1.pack(side="left", padx=(0, 8))
        ttk.Button(g1, text="＋ 추가", width=8, command=self.add_child).pack(side="left", padx=2)
        ttk.Button(g1, text="편집", width=6, command=self.edit_child).pack(side="left", padx=2)
        ttk.Button(g1, text="복제", width=6, command=self.dup_child).pack(side="left", padx=2)
        ttk.Button(g1, text="삭제", width=6, command=self.del_child).pack(side="left", padx=2)

        g2 = ttk.LabelFrame(t, text=" 프로세스 ", padding=5)
        g2.pack(side="left", padx=(0, 8))
        ttk.Button(g2, text="▶ 기동", width=8, command=self.launch).pack(side="left", padx=2)
        ttk.Button(g2, text="■ 종료", width=8, command=self.kill).pack(side="left", padx=2)
        ttk.Button(g2, text="잔재 정리", width=9, command=self.clear_stale).pack(side="left", padx=2)

        g3 = ttk.LabelFrame(t, text=" 시나리오 투입 ", padding=5)
        g3.pack(side="left", fill="x", expand=True)
        self.v_scen = tk.StringVar()
        self.cb_scen = ttk.Combobox(g3, textvariable=self.v_scen, width=26, state="readonly", font=FONT_UI)
        self.cb_scen.pack(side="left", padx=2)
        ttk.Label(g3, text="사이클").pack(side="left", padx=(8, 2))
        self.v_cycles = tk.StringVar(value="0")
        ttk.Spinbox(g3, from_=0, to=99, width=4, textvariable=self.v_cycles, font=FONT_UI).pack(side="left")
        ttk.Label(g3, text="(0=시나리오 기본)", foreground=FG_DIM).pack(side="left", padx=(3, 8))
        self.v_seed = tk.BooleanVar(value=True)
        ttk.Checkbutton(g3, text="차일드별 시드 분산", variable=self.v_seed).pack(side="left", padx=4)
        ttk.Button(g3, text="▶ 선택에 투입", style="Big.TButton",
                   command=lambda: self.dispatch(False)).pack(side="left", padx=(10, 2))
        ttk.Button(g3, text="▶▶ 전체 투입", style="Big.TButton",
                   command=lambda: self.dispatch(True)).pack(side="left", padx=2)

        self.reload_scenarios()

    def _banner(self):
        """막힘 배너 — 차일드가 '못 하겠다'고 물어오면 여기서 답한다."""
        self.banner = tk.Frame(self, bg="#3a2f18", highlightthickness=1, highlightbackground=C_BLOCK)
        self.lb_banner = tk.Label(self.banner, text="", bg="#3a2f18", fg="#f6d68a",
                                  font=FONT_UI_B, anchor="w", justify="left")
        self.lb_banner.pack(side="left", padx=10, pady=6, fill="x", expand=True)

        def bbtn(txt, cmd, w=8, fg=FG):
            b = tk.Button(self.banner, text=txt, font=FONT_UI, width=w, command=cmd,
                          bg=BG3, fg=fg, activebackground=BG_SEL, activeforeground=FG,
                          relief="flat", borderwidth=0, highlightthickness=0, cursor="hand2")
            b.pack(side="right", padx=4, pady=5)
            return b

        for txt, act in (("중단", "abort"), ("건너뛰기", "skip"), ("재시도", "retry")):
            bbtn(txt, lambda a=act: self.resume(a))
        bbtn("🔧 에이전트에 넘기기", lambda: self.agent_from_blocked(), 18, ACCENT)
        self.banner_shown = False

    def _body(self):
        self.pw = pw = ttk.PanedWindow(self, orient="horizontal")
        pw.pack(fill="both", expand=True, padx=10, pady=(0, 6))

        # ── 왼쪽: 차일드 목록
        left = ttk.Frame(pw)
        pw.add(left, weight=1)
        ttk.Label(left, text="차일드", font=FONT_UI_B).pack(anchor="w", pady=(0, 3))

        cols = ("kind", "state", "cycle", "step", "scene", "elapsed", "ew", "note")
        self.tree = ttk.Treeview(left, columns=cols, show="tree headings", height=12, selectmode="extended")
        self.tree.heading("#0", text="차일드")
        self.tree.column("#0", width=170, anchor="w")
        for c, txt, w, a in (("kind", "종류", 56, "center"), ("state", "상태", 66, "center"),
                             ("cycle", "사이클", 50, "center"), ("step", "단계", 90, "w"),
                             ("scene", "씬", 90, "w"), ("elapsed", "경과", 60, "e"),
                             ("ew", "오류/경고", 68, "center"), ("note", "비고", 120, "w")):
            self.tree.heading(c, text=txt)
            self.tree.column(c, width=w, anchor=a)
        self.tree.tag_configure("run", foreground=C_RUN)
        self.tree.tag_configure("dead", foreground=C_DEAD)
        self.tree.tag_configure("blocked", foreground=C_BLOCK, background="#fff4e5")
        self.tree.pack(fill="both", expand=True)
        self.tree.bind("<<TreeviewSelect>>", lambda e: self.refresh_live())
        self.tree.bind("<Double-1>", lambda e: self.edit_child())

        # ── 오른쪽: 탭
        right = ttk.Frame(pw)
        pw.add(right, weight=3)
        self.nb = ttk.Notebook(right)
        self.nb.pack(fill="both", expand=True)

        self.tab_live = self._tab_live()
        self.tab_runs = self._tab_runs()
        self.tab_prob = self._tab_problems()
        self.tab_bal = self._tab_balance()
        self.tab_agent = self._tab_agent()
        self.tab_cov = self._tab_coverage()
        self.tab_raw = self._tab_raw()

    def _mono_text(self, parent):
        fr = ttk.Frame(parent)
        txt = tk.Text(fr, font=FONT_MONO, wrap="none", bg=BG2, fg=FG,
                      insertbackground=FG, selectbackground=BG_SEL, selectforeground=FG,
                      relief="flat", borderwidth=0, highlightthickness=1,
                      highlightbackground=BORDER, highlightcolor=BORDER)
        ys = ttk.Scrollbar(fr, orient="vertical", command=txt.yview)
        xs = ttk.Scrollbar(fr, orient="horizontal", command=txt.xview)
        txt.configure(yscrollcommand=ys.set, xscrollcommand=xs.set)
        txt.grid(row=0, column=0, sticky="nsew")
        ys.grid(row=0, column=1, sticky="ns")
        xs.grid(row=1, column=0, sticky="ew")
        fr.rowconfigure(0, weight=1)
        fr.columnconfigure(0, weight=1)
        txt.tag_configure("err", foreground=C_FAIL)
        txt.tag_configure("warn", foreground=C_WARN)
        txt.tag_configure("ok", foreground=C_PASS)
        txt.tag_configure("head", font=("맑은 고딕", 10, "bold"), foreground=ACCENT)
        txt.tag_configure("dim", foreground=FG_DIM)
        return fr, txt

    def _tab_live(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  실황  ")
        fr, self.tx_live = self._mono_text(f)
        fr.pack(fill="both", expand=True)
        return f

    def _tab_runs(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  결과  ")
        cols = ("at", "inst", "verdict", "cycles", "ew", "dur", "reason")
        self.tv_runs = ttk.Treeview(f, columns=cols, show="headings", height=9)
        for c, txt, w, a in (("at", "시각", 100, "center"), ("inst", "차일드", 70, "center"),
                             ("verdict", "판정", 60, "center"), ("cycles", "사이클", 60, "center"),
                             ("ew", "오류/경고", 70, "center"), ("dur", "소요", 60, "e"),
                             ("reason", "사유", 520, "w")):
            self.tv_runs.heading(c, text=txt)
            self.tv_runs.column(c, width=w, anchor=a)
        self.tv_runs.tag_configure("PASS", foreground=C_PASS)
        self.tv_runs.tag_configure("FAIL", foreground=C_FAIL)
        self.tv_runs.pack(fill="x")
        self.tv_runs.bind("<<TreeviewSelect>>", lambda e: self.show_run_detail())

        fr, self.tx_run = self._mono_text(f)
        fr.pack(fill="both", expand=True, pady=(6, 0))
        return f

    def _tab_problems(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  문제  ")
        fr, self.tx_prob = self._mono_text(f)
        fr.pack(fill="both", expand=True)
        return f

    # ── 밸런스 탭 ───────────────────────────────────────────
    def _tab_balance(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  밸런스  ")

        top = ttk.Frame(f)
        top.pack(fill="x")
        ttk.Label(top, text="밸런스", font=FONT_UI_B).pack(side="left")
        ttk.Label(top, text="소스").pack(side="left", padx=(12, 4))
        self.bal_sources = qa_tuning.list_sources()
        self.v_bal_src = tk.StringVar()
        self.cb_bal_src = ttk.Combobox(top, textvariable=self.v_bal_src, width=34,
                                       state="readonly", font=FONT_UI,
                                       values=[s["label"] for s in self.bal_sources])
        self.cb_bal_src.current(0)
        self.cb_bal_src.pack(side="left")
        self.cb_bal_src.bind("<<ComboboxSelected>>", lambda e: self.switch_source())
        self.lb_bal_file = ttk.Label(top, text="", foreground=FG_DIM)
        self.lb_bal_file.pack(side="left", padx=8)
        ttk.Button(top, text="다시 읽기", command=self.load_tuning).pack(side="right")
        ttk.Button(top, text="저장", style="Big.TButton", command=self.save_tuning).pack(side="right", padx=4)
        self.lb_bal = ttk.Label(top, text="", foreground=C_EDIT, font=FONT_UI_B)
        self.lb_bal.pack(side="right", padx=8)

        srow = ttk.Frame(f)
        srow.pack(fill="x", pady=(5, 2))
        ttk.Label(srow, text="검색").pack(side="left")
        self.v_bal_q = tk.StringVar()
        ttk.Entry(srow, textvariable=self.v_bal_q, width=26).pack(side="left", padx=4)
        self.v_bal_q.trace_add("write", lambda *a: self.fill_tuning())
        self.v_bal_diff = tk.BooleanVar(value=False)
        ttk.Checkbutton(srow, text="기본값과 다른 것만", variable=self.v_bal_diff,
                        command=self.fill_tuning).pack(side="left", padx=10)
        ttk.Label(srow, text="값 칸을 더블클릭하면 바로 편집", foreground=FG_DIM).pack(side="left", padx=6)

        pw = ttk.PanedWindow(f, orient="vertical")
        pw.pack(fill="both", expand=True, pady=(4, 0))

        tw = ttk.Frame(pw)
        pw.add(tw, weight=4)
        cols = ("value", "default", "range")
        self.tv_bal = ttk.Treeview(tw, columns=cols, show="tree headings")
        self.tv_bal.heading("#0", text="항목")
        self.tv_bal.column("#0", width=300, anchor="w")
        for c, txt, w, a in (("value", "현재값", 100, "e"), ("default", "기본값", 100, "e"),
                             ("range", "범위", 130, "center")):
            self.tv_bal.heading(c, text=txt)
            self.tv_bal.column(c, width=w, anchor=a)
        self.tv_bal.tag_configure("group", foreground=ACCENT, font=FONT_UI_B)
        self.tv_bal.tag_configure("changed", foreground=C_WARN)
        self.tv_bal.tag_configure("pending", foreground=C_EDIT, font=FONT_UI_B)
        ys = ttk.Scrollbar(tw, orient="vertical", command=self.tv_bal.yview)
        self.tv_bal.configure(yscrollcommand=ys.set)
        self.tv_bal.pack(side="left", fill="both", expand=True)
        ys.pack(side="right", fill="y")
        self.tv_bal.bind("<<TreeviewSelect>>", lambda e: self.on_bal_select())
        self.tv_bal.bind("<Double-1>", lambda e: self.ed_val.focus_set())

        ed = ttk.Frame(pw, padding=(0, 6))
        pw.add(ed, weight=1)
        self.lb_bal_name = ttk.Label(ed, text="항목을 고르세요", font=FONT_UI_B)
        self.lb_bal_name.pack(anchor="w")
        self.lb_bal_tip = ttk.Label(ed, text="", foreground=FG_DIM, wraplength=900, justify="left")
        self.lb_bal_tip.pack(anchor="w", pady=(2, 6))

        erow = ttk.Frame(ed)
        erow.pack(fill="x")
        self.v_bal_val = tk.StringVar()
        self.ed_val = ttk.Entry(erow, textvariable=self.v_bal_val, width=12, font=FONT_UI_B)
        self.ed_val.pack(side="left")
        self.ed_val.bind("<Return>", lambda e: self.commit_bal_entry())
        self.ed_val.bind("<FocusOut>", lambda e: self.commit_bal_entry())
        self.bal_scale = ttk.Scale(erow, from_=0, to=1, orient="horizontal",
                                   command=self.on_bal_scale, length=420)
        self.bal_scale.pack(side="left", padx=10)
        ttk.Button(erow, text="기본값으로", command=self.bal_reset_one).pack(side="left", padx=4)
        ttk.Button(erow, text="변경 전체 취소", command=self.bal_discard).pack(side="left", padx=4)
        self.lb_bal_hint = ttk.Label(ed, text="", foreground=FG_DIM)
        self.lb_bal_hint.pack(anchor="w", pady=(6, 0))

        self._bal_syncing = False
        self.bal_fields = []
        self.bal_pending = {}
        self.load_tuning()
        return f

    # ── 에이전트 탭 ─────────────────────────────────────────
    def _tab_agent(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  에이전트  ")

        top = ttk.Frame(f)
        top.pack(fill="x")
        ttk.Label(top, text="에이전트에 지시", font=FONT_UI_B).pack(side="left")
        ttk.Label(top, text="— 마더가 본 문제를 그대로 넘겨 Claude Code가 진단·수정하게 한다",
                  foreground=FG_DIM).pack(side="left", padx=6)

        row = ttk.Frame(f)
        row.pack(fill="x", pady=(6, 2))
        ttk.Label(row, text="에이전트").pack(side="left")
        self.v_agent = tk.StringVar(value="dev-fixer")
        ttk.Combobox(row, textvariable=self.v_agent, width=18, state="readonly",
                     values=list_agents()).pack(side="left", padx=4)
        ttk.Label(row, text="모델").pack(side="left", padx=(12, 2))
        self.v_model = tk.StringVar(value="opus")
        ttk.Combobox(row, textvariable=self.v_model, width=9, state="readonly",
                     values=["opus", "sonnet", "fable"]).pack(side="left")
        self.v_edit_ok = tk.BooleanVar(value=False)
        ttk.Checkbutton(row, text="파일 수정 허용 (acceptEdits)", variable=self.v_edit_ok).pack(side="left", padx=14)
        ttk.Label(row, text="끄면 진단·제안만 하고 코드는 안 건드림", foreground=FG_DIM).pack(side="left")

        row2 = ttk.Frame(f)
        row2.pack(fill="x", pady=3)
        ttk.Button(row2, text="문제 요약 → 지시문", command=lambda: self.gen_prompt("fix")).pack(side="left")
        ttk.Button(row2, text="진단만", command=lambda: self.gen_prompt("diagnose")).pack(side="left", padx=4)
        ttk.Button(row2, text="▶ 실행", style="Big.TButton", command=self.run_agent).pack(side="left", padx=(14, 4))
        ttk.Button(row2, text="■ 중단", command=self.stop_agent).pack(side="left")
        ttk.Button(row2, text="변경 내용 보기", command=self.show_agent_diff).pack(side="left", padx=6)
        self.lb_agent = ttk.Label(row2, text="대기", foreground=FG_DIM)
        self.lb_agent.pack(side="left", padx=12)

        pw = ttk.PanedWindow(f, orient="vertical")
        pw.pack(fill="both", expand=True, pady=(6, 0))
        p1 = ttk.Frame(pw)
        pw.add(p1, weight=2)
        ttk.Label(p1, text="보낼 지시 (그대로 고쳐도 됨)", foreground=FG_DIM).pack(anchor="w")
        fr, self.tx_prompt = self._mono_text(p1)
        fr.pack(fill="both", expand=True)
        p2 = ttk.Frame(pw)
        pw.add(p2, weight=3)
        ttk.Label(p2, text="에이전트 출력", foreground=FG_DIM).pack(anchor="w")
        fr2, self.tx_agent_out = self._mono_text(p2)
        fr2.pack(fill="both", expand=True)

        self.agent_proc = None
        return f

    def _tab_coverage(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  커버리지  ")
        top = ttk.Frame(f)
        top.pack(fill="x")
        self.lb_cov = ttk.Label(top, text="", font=FONT_UI_B)
        self.lb_cov.pack(side="left")
        ttk.Button(top, text="새로고침", command=self.refresh_coverage).pack(side="right")

        cols = ("id", "system", "op", "status", "note")
        self.tv_cov = ttk.Treeview(f, columns=cols, show="headings")
        for c, txt, w, a in (("id", "#", 34, "center"), ("system", "게임 시스템", 230, "w"),
                             ("op", "검증 op", 240, "w"), ("status", "상태", 80, "center"),
                             ("note", "비고", 220, "w")):
            self.tv_cov.heading(c, text=txt)
            self.tv_cov.column(c, width=w, anchor=a)
        self.tv_cov.tag_configure("passed", foreground=C_PASS)
        self.tv_cov.tag_configure("blind", foreground=C_FAIL)
        self.tv_cov.tag_configure("partial", foreground="#a35c00")
        self.tv_cov.tag_configure("notimpl", foreground=C_DEAD)
        self.tv_cov.pack(fill="both", expand=True, pady=(4, 0))
        self.refresh_coverage()
        return f

    def _tab_raw(self):
        f = ttk.Frame(self.nb, padding=6)
        self.nb.add(f, text="  원본 로그  ")
        top = ttk.Frame(f)
        top.pack(fill="x")
        ttk.Label(top, text="차일드가 떨군 전체 리포트", font=FONT_UI_B).pack(side="left")
        ttk.Label(top, text="(사람이 옮겨 붙일 필요 없음 — 마더가 직접 읽는다)",
                  foreground=FG_DIM).pack(side="left", padx=6)
        self.cb_raw = ttk.Combobox(top, width=46, state="readonly", font=FONT_UI)
        self.cb_raw.pack(side="right")
        self.cb_raw.bind("<<ComboboxSelected>>", lambda e: self.show_raw())

        fr, self.tx_raw = self._mono_text(f)
        fr.pack(fill="both", expand=True, pady=(4, 0))
        return f

    def _statusbar(self):
        tk.Frame(self, height=1, bg=BORDER).pack(fill="x")
        s = ttk.Frame(self, padding=(10, 4))
        s.pack(fill="x")

        self.lb_stat = ttk.Label(s, text="—", font=FONT_UI)
        self.lb_stat.pack(side="left")
        ttk.Label(s, text="│", foreground=BORDER).pack(side="left", padx=8)
        self.lb_stat2 = ttk.Label(s, text="", font=FONT_UI, foreground=FG_DIM)
        self.lb_stat2.pack(side="left")
        ttk.Button(s, text="Claude 보고서 생성", command=self.make_claude_report).pack(side="right")
        ttk.Button(s, text="🔧 에이전트에 넘기기",
                   command=lambda: self.gen_prompt("fix")).pack(side="right", padx=6)
        self.lb_time = ttk.Label(s, text="", foreground=FG_DIM)
        self.lb_time.pack(side="right", padx=8)

    # ── 큐 처리 ─────────────────────────────────────────────
    def _drain(self):
        try:
            while True:
                kind, payload = self.q.get_nowait()
                if kind == "cfg":
                    # 외부에서 qa-instances.json을 고쳐도 따라간다
                    if payload != self.cfg:
                        self.cfg = payload
                        self.lb_dir.config(text=str(orch.data_dir(payload)))
                elif kind == "status":
                    self.on_status(payload)
                elif kind == "results":
                    self.on_results(payload)
                elif kind == "reports":
                    self.on_reports(payload)
                elif kind == "cmdfiles":
                    prev = getattr(self, "_cmd_pending", None)
                    if prev is not None:
                        for name, now in payload.items():
                            if prev.get(name) and not now:
                                self.log("수령", f"[{name or '(기본)'}] 차일드가 명령을 집어감 "
                                                 f"— 실행 시작", "ok")
                    self._cmd_pending = payload
                elif kind == "agent":
                    t = self.tx_agent_out
                    at_bottom = t.yview()[1] > 0.999
                    style = "err" if ("Error" in payload or "실패" in payload) else ""
                    t.insert("end", payload + "\n", style)
                    if at_bottom:
                        t.see("end")
                elif kind == "agent_done":
                    self.on_agent_done(payload)
                elif kind == "error":
                    self.lb_watch.config(text=f"⚠ 감시 오류: {payload[:60]}", foreground=C_FAIL)
        except queue.Empty:
            pass
        self.after(200, self._drain)

    def on_status(self, rows):
        self.rows = rows
        live = set()
        blocked_row = None
        n_run = 0
        for r in rows:
            name = r["name"]
            iid = name or "__default__"
            tag = "blocked" if r["blocked"] else ("run" if r["alive"] else "dead")
            if r["alive"] and r["state"] == "running":
                n_run += 1
            if r["blocked"] and blocked_row is None:
                blocked_row = r

            note = "⛔ 막힘 — 응답 대기" if r["blocked"] else (
                "지난 막힘 잔재" if r["blockedStale"] else
                ("" if r["alive"] else (f"끊김 {r['ageSec']}s 전" if r["ageSec"] else "미기동")))
            mark = "●" if r["alive"] else "○"
            label = r["label"] or (r["instance"])
            text = f"{mark} {r['instance']}  {label}"
            values = (r["kind"], r["state"] or "-", r["cycle"] or "-",
                      r["step"] or "-", r["scene"] or "-",
                      f"{r['elapsedSec']}s" if r["elapsedSec"] else "-",
                      f"{r['errors']}/{r['warns']}" if r["errors"] is not None else "-",
                      note)
            live.add(iid)
            # 지우고 다시 넣으면 1초마다 선택이 풀린다 — 있으면 제자리 갱신
            if self.tree.exists(iid):
                self.tree.item(iid, text=text, values=values, tags=(tag,))
            else:
                self.tree.insert("", "end", iid=iid, text=text, values=values, tags=(tag,))

            # 실황 피드 — 새 줄만 덧붙인다
            seen = self.seen.setdefault(iid, set())
            feed = self.feed.setdefault(iid, [])
            for line in (r.get("recent") or []):
                if line not in seen:
                    seen.add(line)
                    feed.append(f"[{datetime.now():%H:%M:%S}] {line}")
            if len(feed) > 400:
                del feed[:len(feed) - 400]

        for iid in self.tree.get_children():   # 레지스트리에서 빠진 차일드 제거
            if iid not in live:
                self.tree.delete(iid)

        self.show_banner(blocked_row)
        self.refresh_live()
        self.lb_time.config(text=f"갱신 {datetime.now():%H:%M:%S}")
        self.lb_watch.config(text="● 감시중", foreground=C_RUN)

        p = self.summary.get("pass", 0)
        f = self.summary.get("fail", 0)
        blk = sum(1 for r in rows if r["blocked"])
        self.lb_stat.config(text=f"차일드 {len(rows)}  ·  실행중 {n_run}"
                                 + (f"  ·  ⛔막힘 {blk}" if blk else ""),
                            foreground=C_BLOCK if blk else FG)
        self.lb_stat2.config(text=f"통과 {p}  ·  미통과 {f}  ·  런 {self.summary.get('runCount', 0)}건")
        self.title(f"QA 마더 — 차일드 {len(rows)}"
                   + (f" · 실행중 {n_run}" if n_run else "")
                   + (" · ⛔막힘" if blk else ""))

    def on_results(self, summary):
        self.summary = summary
        for iid in self.tv_runs.get_children():
            self.tv_runs.delete(iid)
        for i, r in enumerate(summary.get("runs", [])):
            self.tv_runs.insert("", "end", iid=str(i),
                                values=(r["at"], r["instance"] or "(기본)", r["verdict"],
                                        r["cycles"], f"{r['errors']}/{r['warns']}",
                                        f"{r['durationSec']}s", (r["reason"] or "")[:160]),
                                tags=(r["verdict"],))
        self.refresh_problems()

        # 새 판정이 도착하면 결과 탭으로 끌어올린다
        runs = summary.get("runs", [])
        if runs:
            sig = (runs[0]["at"], runs[0]["instance"], runs[0]["verdict"])
            if self.last_verdict_seen and sig != self.last_verdict_seen:
                self.nb.select(self.tab_runs)
                self.tv_runs.selection_set("0")
                self.show_run_detail()
                r0 = runs[0]
                self.log("결과", f"[{r0['instance'] or '(기본)'}] {r0['verdict']} — {r0['reason']}",
                         "ok" if r0["verdict"] == "PASS" else "err")
            self.last_verdict_seen = sig

    def on_reports(self, reports):
        self.reports = reports
        vals = [f"{r['at']}  {r['name']}  ({r['sizeKB']}KB)" for r in reports]
        cur = self.cb_raw.current()
        self.cb_raw["values"] = vals
        if vals:
            self.cb_raw.current(cur if 0 <= cur < len(vals) else 0)
            self.show_raw()

    # ── 화면 갱신 ───────────────────────────────────────────
    def sel_rows(self):
        sel = self.tree.selection()
        if not sel:
            return self.rows
        want = {("" if s == "__default__" else s) for s in sel}
        return [r for r in self.rows if r["name"] in want]

    def sel_names(self):
        sel = self.tree.selection()
        if not sel:
            return ""
        return ",".join("" if s == "__default__" else s for s in sel)

    def refresh_live(self):
        rows = self.sel_rows()
        t = self.tx_live
        at_bottom = t.yview()[1] > 0.999   # 사용자가 위로 올려 읽는 중이면 끌어내리지 않는다
        t.delete("1.0", "end")
        for r in rows:
            iid = r["name"] or "__default__"
            t.insert("end", f"[{r['instance']}] {r['label']}\n", "head")
            state = r["state"] or "-"
            tag = "ok" if r["alive"] else "dim"
            t.insert("end", f"  상태 {state} · 사이클 {r['cycle'] or '-'} · 스텝 {r['step'] or '-'}"
                            f" · 씬 {r['scene'] or '-'} · 경과 {r['elapsedSec']}s\n", tag)
            if r["errors"] is not None:
                t.insert("end", f"  오류 {r['errors']} · 경고 {r['warns']}\n",
                         "err" if r["errors"] else "dim")
            if r["blockedInfo"]:
                b = r["blockedInfo"]
                head = "⛔ 막힘(응답 대기)" if r["blocked"] else "지난 막힘 잔재 — [잔재 정리]로 지움"
                t.insert("end", f"  {head} [{b.get('kind')}] {b.get('message')}\n",
                         "err" if r["blocked"] else "dim")
                t.insert("end", f"     씬 {b.get('scene')} 위치 {b.get('playerPos')} · 시도: {b.get('tried')}\n",
                         "warn" if r["blocked"] else "dim")
            if r["kind"] == "build" and not r["exePath"]:
                t.insert("end", "  ※ exePath 미설정 — [편집]에서 빌드 경로를 넣어야 기동된다\n", "warn")

            feed = self.feed.get(iid, [])
            t.insert("end", "  ── 실황 ──\n", "dim")
            for line in feed[-60:]:
                style = "err" if ("Error" in line or "오류" in line) else (
                    "warn" if ("Warn" in line or "경고" in line) else "")
                t.insert("end", f"  {line}\n", style)
            if not feed:
                t.insert("end", "  (아직 이벤트 없음 — 차일드가 런을 시작하면 여기에 흐른다)\n", "dim")
            t.insert("end", "\n")
        if at_bottom:
            t.see("end")

    def show_run_detail(self):
        sel = self.tv_runs.selection()
        if not sel:
            return
        r = self.summary.get("runs", [])[int(sel[0])]
        t = self.tx_run
        t.delete("1.0", "end")
        t.insert("end", f"{r['verdict']}  —  {r['reason'] or ''}\n",
                 "ok" if r["verdict"] == "PASS" else "err")
        t.insert("end", f"{r['at']} · 차일드 {r['instance'] or '(기본)'} · 시나리오 {r['scenario']}"
                        f" · 사이클 {r['cycles']} · {r['durationSec']}s\n\n", "dim")

        ng = r.get("navGrids", [])
        if ng:
            t.insert("end", "── 길찾기 격자 (봇이 못 움직일 때 원인을 가르는 값) ──\n", "head")
            for g in ng:
                if not g.get("present"):
                    t.insert("end", f"  ✘ {g.get('scene'):<12} 격자 없음 — 적·NPC·봇 전부 직선 이동\n", "err")
                    continue
                pct = g.get("blockedPct", 0)
                style = "err" if (pct <= 0 or pct >= 70) else "ok"
                note = (" ← 장애물 못 잡음(베이크 시점)" if pct <= 0 else
                        " ← 과다 팽창, 통로 막힘 의심" if pct >= 70 else "")
                t.insert("end", f"  {'✔' if style == 'ok' else '⚠'} {g.get('scene'):<12}"
                                f"{g.get('width')}×{g.get('height')} 셀 {g.get('cellSize', 0):.2f}m"
                                f" · 막힘 {pct:.1f}%{note}\n", style)
            t.insert("end", "\n")

        t.insert("end", "── 판정 항목 ──\n", "head")
        for c in r.get("checks", []):
            mark = "✔" if c.get("passed") else "✘"
            t.insert("end", f"  {mark} {c.get('name',''):<16} {c.get('detail','')}\n",
                     "ok" if c.get("passed") else "err")

        an = r.get("anomalies", [])
        t.insert("end", f"\n── 이상 {len(an)}건 ──\n", "head")
        for a in an:
            style = "err" if a.get("level") == "Error" else "warn"
            t.insert("end", f"  [{a.get('atSec',0):>6.1f}s] {a.get('level','')[:4]:<5}"
                            f"{a.get('step',''):<12}{a.get('kind',''):<16}{a.get('msg','')}\n", style)
            if a.get("playerPos"):
                t.insert("end", f"           위치 {a.get('playerPos')} ({a.get('scene')})\n", "dim")

    def refresh_problems(self):
        t = self.tx_prob
        t.delete("1.0", "end")
        s = self.summary
        if not s:
            return
        t.insert("end", f"최근 런 {s.get('runCount',0)}건  ·  통과 {s.get('pass',0)}  ·  미통과 {s.get('fail',0)}\n\n", "head")

        fc = s.get("failedChecks", {})
        t.insert("end", "── 반복해서 실패하는 판정 항목 ──\n", "head")
        if fc:
            for k, v in fc.items():
                t.insert("end", f"  {k:<20} {v}회\n", "err")
        else:
            t.insert("end", "  없음\n", "dim")

        an = s.get("topAnomalies", {})
        t.insert("end", "\n── 이상 빈도 ──\n", "head")
        if an:
            for k, v in an.items():
                t.insert("end", f"  {k:<24} {v}\n", "warn")
        else:
            t.insert("end", "  없음\n", "dim")

        sp = s.get("problemSpots", [])
        t.insert("end", f"\n── 문제 지점(스턱/길막힘/사망) {len(sp)}곳 ──\n", "head")
        if sp:
            t.insert("end", f"  {'씬':<14}{'좌표':<16}{'스턱':<6}{'막힘':<6}{'사망'}\n", "dim")
            for x in sp:
                t.insert("end", f"  {str(x['scene']):<14}({x['x']:>4},{x['y']:>4}){'':<4}"
                                f"{x['stuck']:<6}{x['unreachable']:<6}{x['deaths']}\n", "warn")
        else:
            t.insert("end", "  없음\n", "dim")

    def refresh_coverage(self):
        cov = orch.coverage_summary()
        for iid in self.tv_cov.get_children():
            self.tv_cov.delete(iid)
        if cov.get("error"):
            self.lb_cov.config(text=cov["error"])
            return
        c = cov["counts"]
        self.lb_cov.config(text=f"게임 시스템 {cov['total']}개  —  " +
                                "  ·  ".join(f"{k} {v}" for k, v in c.items()))
        for r in cov.get("rows", []):
            st = r.get("status", "?")
            self.tv_cov.insert("", "end", values=(r.get("id"), r.get("system"),
                                                  r.get("op") or "— (op 없음)", st, r.get("note", "")),
                               tags=(st,))

    def show_raw(self):
        i = self.cb_raw.current()
        if not (0 <= i < len(self.reports)):
            return
        p = Path(self.reports[i]["file"])
        t = self.tx_raw
        t.delete("1.0", "end")
        try:
            body = p.read_text(encoding="utf-8", errors="replace")
        except Exception as e:
            t.insert("end", f"읽기 실패: {e}", "err")
            return
        for line in body.splitlines():
            style = "err" if " Error " in line else ("warn" if " Warn " in line else "")
            t.insert("end", line + "\n", style)

    def show_banner(self, row):
        if row and not self.banner_shown:
            self.banner.pack(fill="x", padx=10, pady=(0, 4), before=self.pw)
            self.banner_shown = True
        elif not row and self.banner_shown:
            self.banner.pack_forget()
            self.banner_shown = False
        if row:
            b = row["blockedInfo"] or {}
            self.lb_banner.config(
                text=f"⛔ [{row['instance']}] 막힘 — {b.get('kind','?')} · {b.get('message','')}\n"
                     f"    씬 {b.get('scene','?')} 위치 {b.get('playerPos','?')} · 시도: {b.get('tried','')}")

    # ── 동작 ────────────────────────────────────────────────
    def reload_scenarios(self):
        scens = orch.list_scenarios()
        self.scenarios = scens
        vals = [f"{s['name']}  —  {s['label']} ({s['steps']}스텝)" for s in scens]
        self.cb_scen["values"] = vals
        if vals:
            self.cb_scen.current(0)

    def scen_name(self):
        i = self.cb_scen.current()
        return self.scenarios[i]["name"] if 0 <= i < len(self.scenarios) else "default"

    def dispatch(self, everyone):
        names = "" if everyone else self.sel_names()
        if not everyone and not names:
            messagebox.showinfo("투입", "차일드를 고르거나 [▶▶ 전체 투입]을 쓰세요.")
            return
        res = orch.dispatch(self.cfg, self.scen_name(), names,
                            int(self.v_cycles.get() or 0), "", "마더 GUI 투입",
                            self.v_seed.get())
        if res.get("error"):
            messagebox.showerror("투입 실패", res["error"])
            return
        who = ", ".join(d["instance"] or "(기본)" for d in res["dispatched"])
        cyc = int(self.v_cycles.get() or 0)
        for d in res["dispatched"]:
            self.log("투입", f"[{d['instance'] or '(기본)'}] 시나리오 '{res['scenario']}' "
                             f"id={d['id']} seed={d['seed']}"
                             f"{f' 사이클={cyc}' if cyc else ''} → {Path(d['file']).name}", "cmd")
        self.nb.select(self.tab_live)
        messagebox.showinfo("투입 완료",
                            f"시나리오 '{res['scenario']}' → {who}\n\n"
                            "※ 차일드가 -qa-serve 로 떠 있어야 명령을 집어갑니다.\n"
                            "   Unity 에디터는 명령 대기 모드가 아니므로 F9로 직접 돌립니다.")

    def launch(self):
        res = orch.launch_instances(self.cfg, self.sel_names())
        for l in res["launched"]:
            self.log("기동", f"[{l['instance'] or '(기본)'}] pid={l['pid']}", "ok")
        for s in res["skipped"]:
            self.log("기동실패", f"[{s['instance'] or '(기본)'}] {s['why']}", "warn")
        msg = "\n".join(f"기동: {l['instance'] or '(기본)'} pid={l['pid']}" for l in res["launched"])
        msg += "\n" + "\n".join(f"스킵: {s['instance'] or '(기본)'} — {s['why']}" for s in res["skipped"])
        messagebox.showinfo("기동", msg.strip() or "대상 없음")

    def kill(self):
        names = self.sel_names()
        if not messagebox.askyesno("종료", f"{names or '전체'} 차일드 프로세스를 종료할까요?"):
            return
        killed = orch.kill_instances(self.cfg, names)
        self.log("종료", f"차일드 종료: {killed or '대상 없음'}", "warn")
        messagebox.showinfo("종료", f"종료: {killed or '없음'}")

    # ── 밸런스 ──────────────────────────────────────────────
    def bal_src(self):
        i = self.cb_bal_src.current()
        return self.bal_sources[i if 0 <= i < len(self.bal_sources) else 0]

    def switch_source(self):
        if self.bal_pending and not messagebox.askyesno(
                "밸런스", f"저장 안 한 변경 {len(self.bal_pending)}건이 있습니다.\n"
                          "소스를 바꾸면 버려집니다. 계속할까요?"):
            return
        self.load_tuning()

    def load_tuning(self):
        src = self.bal_src()
        try:
            self.bal_fields = qa_tuning.load(src["key"])
        except Exception as e:
            messagebox.showerror("밸런스", f"{src['label']} 읽기 실패: {e}")
            self.bal_fields = []
        self.bal_pending = {}
        self.lb_bal_file.config(text=f"→ {Path(src['asset']).name} · 메타 {Path(src['cs']).name}")
        self.fill_tuning()

    def bal_value(self, f):
        """저장 대기 값이 있으면 그걸, 없으면 에셋 값."""
        return self.bal_pending.get(f["name"], f["value"])

    def fill_tuning(self):
        for iid in self.tv_bal.get_children():
            self.tv_bal.delete(iid)

        q = (self.v_bal_q.get() or "").strip().lower()
        only_diff = self.v_bal_diff.get()
        groups = {}
        for f in self.bal_fields:
            v = self.bal_value(f)
            if only_diff and v == f["default"] and f["name"] not in self.bal_pending:
                continue
            if q and q not in f["name"].lower() and q not in (f["tooltip"] or "").lower() \
                    and q not in f["group"].lower():
                continue
            groups.setdefault(f["group"], []).append(f)

        for g, fields in groups.items():
            gid = self.tv_bal.insert("", "end", text=g, values=("", "", ""),
                                     open=True, tags=("group",))
            for f in fields:
                v = self.bal_value(f)
                rng = f"{f['min']:g} ~ {f['max']:g}" if f["min"] is not None else "-"
                if f["type"] == "bool":
                    show, dflt = ("켜짐" if v else "꺼짐"), ("켜짐" if f["default"] else "꺼짐")
                    rng = "켜기/끄기"
                else:
                    show, dflt = num(v), num(f["default"])
                tag = "pending" if f["name"] in self.bal_pending else (
                    "changed" if v != f["default"] else "")
                self.tv_bal.insert(gid, "end", iid=f["name"], text=f"    {f['name']}",
                                   values=(show, dflt, rng), tags=(tag,) if tag else ())

        n = len(self.bal_pending)
        self.lb_bal.config(text=f"저장 대기 {n}건" if n else "")

    def bal_selected(self):
        sel = self.tv_bal.selection()
        if not sel:
            return None
        for f in self.bal_fields:
            if f["name"] == sel[0]:
                return f
        return None

    def on_bal_select(self):
        f = self.bal_selected()
        if f is None:
            self.lb_bal_name.config(text="항목을 고르세요")
            self.lb_bal_tip.config(text="")
            self.lb_bal_hint.config(text="")
            return
        v = self.bal_value(f)
        self.lb_bal_name.config(text=f"{f['name']}   ({f['type']}, {f['group']})")
        self.lb_bal_tip.config(text=f["tooltip"] or "(설명 없음)")

        self._bal_syncing = True
        self.v_bal_val.set(num(v))
        if f["min"] is not None and v is not None:
            self.bal_scale.configure(from_=f["min"], to=f["max"], state="normal")
            self.bal_scale.set(float(v))
        else:
            self.bal_scale.configure(from_=0, to=1, state="disabled")
        self._bal_syncing = False

        hint = "" if not f["missing"] else "※ 에셋에 아직 없는 필드 — 저장하면 새로 추가된다"
        if f["type"] == "bool":
            hint = "0=끄기 / 1=켜기. " + hint
        self.lb_bal_hint.config(text=hint)

    def set_pending(self, f, value):
        if f["type"] in ("int", "bool"):
            value = int(round(float(value)))
        else:
            value = round(float(value), 6)
        if f["min"] is not None:
            value = max(f["min"], min(f["max"], value))
            if f["type"] in ("int", "bool"):
                value = int(round(value))
        if value == f["value"]:
            self.bal_pending.pop(f["name"], None)
        else:
            self.bal_pending[f["name"]] = value
        self.fill_tuning()
        self.tv_bal.selection_set(f["name"])
        self.tv_bal.see(f["name"])

    def on_bal_scale(self, raw):
        if self._bal_syncing:
            return
        f = self.bal_selected()
        if f is None or f["min"] is None:
            return
        v = float(raw)
        if f["type"] in ("int", "bool"):
            v = int(round(v))
        self._bal_syncing = True
        self.v_bal_val.set(f"{v:g}")
        self._bal_syncing = False
        self.set_pending(f, v)

    def commit_bal_entry(self):
        if self._bal_syncing:
            return
        f = self.bal_selected()
        if f is None:
            return
        try:
            v = float(self.v_bal_val.get())
        except ValueError:
            self.v_bal_val.set(f"{self.bal_value(f):g}")
            return
        self.set_pending(f, v)

    def bal_reset_one(self):
        f = self.bal_selected()
        if f is not None:
            self.set_pending(f, f["default"])
            self.on_bal_select()

    def bal_discard(self):
        if not self.bal_pending:
            return
        if messagebox.askyesno("밸런스", f"저장 안 한 변경 {len(self.bal_pending)}건을 버릴까요?"):
            self.bal_pending = {}
            self.fill_tuning()
            self.on_bal_select()

    def save_tuning(self):
        if not self.bal_pending:
            messagebox.showinfo("밸런스", "바뀐 값이 없습니다.")
            return
        src = self.bal_src()
        lines = "\n".join(f"  {k} = {v}" for k, v in self.bal_pending.items())
        if not messagebox.askyesno("밸런스 저장",
                                   f"{src['label']}\n{Path(src['asset']).name} 에 "
                                   f"{len(self.bal_pending)}건을 씁니다.\n\n{lines}"):
            return
        applied, warns = qa_tuning.save(src["key"], self.bal_pending)
        self.log("밸런스", f"{src['label']} 저장 — {'; '.join(applied)}", "cmd")
        for w in warns:
            self.log("밸런스", w, "warn")
        self.load_tuning()
        msg = "적용:\n" + "\n".join(f"  {a}" for a in applied)
        if warns:
            msg += "\n\n경고:\n" + "\n".join(warns)
        msg += "\n\n※ Unity 창을 한 번 클릭하면 에셋을 다시 읽습니다."
        if src["key"] == "gametuning":
            msg += "\n※ buildingEnterRatio를 바꿨다면 `빌드 ▸ 지역1` 재실행이 필요합니다."
        messagebox.showinfo("밸런스 저장 완료", msg)

    # ── 에이전트 ────────────────────────────────────────────
    def gen_prompt(self, mode, extra=""):
        s = self.summary or {}
        runs = s.get("runs", [])
        blocked = [r for r in self.rows if r["blocked"]]

        L = []
        if mode == "diagnose":
            L.append("QA 마더가 넘긴 결과다. **진단만** 해라 — 코드는 고치지 말고, "
                     "원인 가설과 확인 방법을 우선순위대로 정리해줘.")
        else:
            L.append("QA 마더가 넘긴 결과다. 아래 문제를 **실제로 고쳐라**. "
                     "무엇을 왜 고쳤는지와 재검증 방법을 마지막에 정리해줘.")
        L.append("")
        L.append("게임: demo13-flashlight (Unity 6 탑다운 2D). QA 시스템 설명은 "
                 "`demo13-flashlight/docs/qa.md`, 봇은 `Assets/Scripts/QA/`.")
        L.append("")

        if extra:
            L += ["## 지금 막힌 것", "", extra, ""]

        if blocked:
            L.append("## 차일드가 막혀서 응답을 기다리는 중")
            for r in blocked:
                b = r["blockedInfo"] or {}
                L.append(f"- [{r['instance']}] {b.get('kind')} · {b.get('message')} "
                         f"(씬 {b.get('scene')}, 위치 {b.get('playerPos')}, 시도 {b.get('tried')})")
            L.append("")

        if runs:
            r = runs[0]
            L.append(f"## 최신 런 — {r['verdict']}")
            L.append(f"{r['at']} · 차일드 {r['instance'] or '(기본)'} · 시나리오 {r['scenario']} "
                     f"· 사이클 {r['cycles']} · {r['durationSec']}s")
            L.append(f"판정 사유: {r['reason']}")
            L.append("")
            fails = [c for c in r.get("checks", []) if not c.get("passed")]
            if fails:
                L.append("실패한 판정 항목:")
                L += [f"- {c.get('name')} — {c.get('detail','')}" for c in fails]
                L.append("")
            an = r.get("anomalies", [])
            if an:
                L.append(f"이상 {len(an)}건:")
                for a in an[:40]:
                    L.append(f"- [{a.get('atSec',0):.1f}s] {a.get('level')} {a.get('step')} "
                             f"{a.get('kind')} — {a.get('msg')} @{a.get('playerPos','')} {a.get('scene','')}")
                L.append("")

        if s.get("failedChecks"):
            L.append("## 여러 런에서 반복 실패")
            L += [f"- {k} — {v}회" for k, v in s["failedChecks"].items()]
            L.append("")
        if s.get("problemSpots"):
            L.append("## 문제 좌표(스턱/길막힘/사망)")
            L += [f"- {x['scene']} ({x['x']},{x['y']}) 스턱{x['stuck']} 막힘{x['unreachable']} 사망{x['deaths']}"
                  for x in s["problemSpots"][:20]]
            L.append("")
        if self.reports:
            L.append(f"## 원본 전체 로그\n\n`{self.reports[0]['file']}`")

        text = "\n".join(L)
        self.tx_prompt.delete("1.0", "end")
        self.tx_prompt.insert("1.0", text)
        self.nb.select(self.tab_agent)
        self.log("지시문", f"{'수정' if mode == 'fix' else '진단'} 지시문 생성 — "
                          f"{len(text)}자 (막힌 차일드 {len(blocked)}, 최신런 "
                          f"{runs[0]['verdict'] if runs else '없음'})")
        return text

    def agent_from_blocked(self):
        rows = [r for r in self.rows if r["blocked"]]
        if not rows:
            messagebox.showinfo("에이전트", "막힌 차일드가 없습니다.")
            return
        b = rows[0]["blockedInfo"] or {}
        self.gen_prompt("fix", f"차일드 [{rows[0]['instance']}]가 `{b.get('kind')}`로 멈춰 응답을 기다린다: "
                               f"{b.get('message')} (씬 {b.get('scene')}, 위치 {b.get('playerPos')})")

    def run_agent(self):
        if self.agent_proc is not None and self.agent_proc.poll() is None:
            messagebox.showinfo("에이전트", "이미 실행 중입니다. 먼저 중단하세요.")
            return
        exe = claude_exe()
        if not exe:
            messagebox.showerror("에이전트", "claude CLI를 못 찾았습니다.\nPATH에 claude가 있어야 합니다.")
            return

        prompt = self.tx_prompt.get("1.0", "end").strip()
        if not prompt:
            prompt = self.gen_prompt("fix")

        # 지시문은 파일로 넘긴다 — 인자에 긴 한글을 실으면 셸 인용에서 깨질 수 있다
        AGENT_REQUEST.write_text(prompt, encoding="utf-8")
        cmd = [exe, "-p", f"{AGENT_REQUEST} 파일을 읽고 거기 적힌 QA 결과를 처리해줘.",
               "--agent", self.v_agent.get(), "--model", self.v_model.get()]
        if self.v_edit_ok.get():
            cmd += ["--permission-mode", "acceptEdits"]

        # 실행 전 상태를 찍어둔다 — 끝나고 비교해야 "고쳤는지"를 말할 수 있다
        self.git_before = self.git_state()
        self.agent_started = datetime.now()
        mode = "수정 허용" if self.v_edit_ok.get() else "진단만(수정 금지)"

        self.tx_agent_out.delete("1.0", "end")
        self.tx_agent_out.insert("end", f"$ claude -p … --agent {self.v_agent.get()} "
                                        f"--model {self.v_model.get()}"
                                        f"{' --permission-mode acceptEdits' if self.v_edit_ok.get() else ''}\n"
                                        f"  모드: {mode}\n"
                                        f"  지시문: {AGENT_REQUEST} ({len(prompt)}자)\n"
                                        f"  실행 전 변경된 파일: {len(self.git_before)}개\n\n", "dim")
        self.lb_agent.config(text="● 실행 중", foreground=C_RUN)
        self.nb.select(self.tab_agent)
        self.log("지시", f"에이전트 {self.v_agent.get()} 실행 ({mode}, 모델 {self.v_model.get()}, "
                         f"지시문 {len(prompt)}자)", "cmd")

        def worker():
            try:
                p = subprocess.Popen(cmd, cwd=str(REPO), stdout=subprocess.PIPE,
                                     stderr=subprocess.STDOUT, text=True,
                                     encoding="utf-8", errors="replace", bufsize=1)
                self.agent_proc = p
                for line in p.stdout:
                    self.q.put(("agent", line.rstrip("\n")))
                p.wait()
                self.q.put(("agent_done", p.returncode))
            except Exception as e:
                self.q.put(("agent", f"[실행 실패] {type(e).__name__}: {e}"))
                self.q.put(("agent_done", -1))

        threading.Thread(target=worker, daemon=True).start()

    def on_agent_done(self, code):
        """끝났으면 **무엇이 바뀌었는지**를 파일 단위로 보여준다.
        이게 없으면 '고쳤는지 안 고쳤는지' 알 길이 없다."""
        ok = (code == 0)
        t = self.tx_agent_out
        after = self.git_state()
        before = getattr(self, "git_before", {})

        changed = []
        for path, nums in after.items():
            if before.get(path) != nums:
                changed.append((path, nums, path not in before))

        secs = (datetime.now() - getattr(self, "agent_started", datetime.now())).total_seconds()
        t.insert("end", f"\n{'─' * 60}\n", "dim")
        t.insert("end", f"에이전트 종료 — 코드 {code} · {secs:.0f}초\n", "ok" if ok else "err")

        if changed:
            t.insert("end", f"\n✔ 이 실행으로 바뀐 파일 {len(changed)}개\n", "ok")
            for path, (add, dele), is_new in sorted(changed):
                mark = "신규" if is_new else "수정"
                delta = f"+{add} -{dele}" if add != "신규" else "새 파일"
                t.insert("end", f"    [{mark}] {path}   {delta}\n", "ok")
            t.insert("end", "\n  → 내용 확인:  git diff\n", "dim")
            self.log("수정됨", f"에이전트가 파일 {len(changed)}개 변경 — "
                              + ", ".join(p for p, _, _ in sorted(changed)[:5])
                              + ("…" if len(changed) > 5 else ""), "ok")
        else:
            if self.v_edit_ok.get():
                t.insert("end", "\n✘ 바뀐 파일 없음 — 에이전트가 코드를 고치지 않았다.\n", "warn")
                t.insert("end", "   위 출력에서 이유를 확인하세요(고칠 게 없다고 판단했거나, "
                                "진단만 하고 끝냈을 수 있음).\n", "dim")
                self.log("변경없음", "에이전트가 파일을 고치지 않음 (수정 허용 상태였음)", "warn")
            else:
                t.insert("end", "\n· 바뀐 파일 없음 — '파일 수정 허용'이 꺼져 있어 진단만 했습니다.\n", "dim")
                t.insert("end", "   실제로 고치게 하려면 체크박스를 켜고 다시 실행하세요.\n", "dim")
                self.log("진단만", "수정 허용 꺼짐 — 파일 변경 없음")

        t.see("end")
        self.lb_agent.config(
            text=(f"완료 · 파일 {len(changed)}개 수정" if changed else
                  ("완료 · 변경 없음" if ok else f"실패(코드 {code})")),
            foreground=C_PASS if (ok and changed) else (C_WARN if ok else C_FAIL))

    def show_agent_diff(self):
        try:
            r = subprocess.run(["git", "diff"], cwd=str(REPO), capture_output=True,
                               text=True, encoding="utf-8", errors="replace", timeout=30)
            body = r.stdout or "(변경 없음)"
        except Exception as e:
            body = f"git diff 실패: {e}"
        t = self.tx_agent_out
        t.insert("end", f"\n{'─' * 60}\n── git diff ──\n", "head")
        for line in body.splitlines()[:600]:
            style = "ok" if line.startswith("+") and not line.startswith("+++") else (
                "err" if line.startswith("-") and not line.startswith("---") else
                ("head" if line.startswith("diff --git") else "dim"))
            t.insert("end", line + "\n", style)
        t.see("end")

    def stop_agent(self):
        p = self.agent_proc
        if p is None or p.poll() is not None:
            self.lb_agent.config(text="대기", foreground=FG_DIM)
            return
        try:
            p.terminate()
        except Exception:
            pass
        self.lb_agent.config(text="중단됨", foreground=C_WARN)

    def clear_stale(self):
        removed = orch.clear_stale(self.cfg, self.sel_names())
        if removed:
            self.log("정리", f"잔재 파일 삭제: {', '.join(removed)}", "warn")
        messagebox.showinfo("잔재 정리", "\n".join(removed) if removed else "지울 잔재 없음")

    def resume(self, action):
        rows = [r for r in self.rows if r["blocked"]]
        if not rows:
            self.log("응답", "막힌 차일드가 없어 무시됨", "warn")
            return
        names = ",".join(r["name"] for r in rows)
        orch.write_resume(self.cfg, names, action, "마더 GUI 응답")
        ko = {"retry": "재시도", "skip": "건너뛰기", "abort": "중단"}[action]
        for r in rows:
            b = r["blockedInfo"] or {}
            self.log("응답", f"[{r['instance']}] {b.get('kind','?')} → {ko}", "cmd")

    # ── 차일드 편집 ─────────────────────────────────────────
    def _reload_cfg(self):
        self.cfg = orch.load_instances()

    def add_child(self):
        taken = {i.get("name", "") for i in self.cfg.get("instances", [])}
        d = ChildDialog(self, None, taken)
        self.wait_window(d)
        if not d.result:
            return
        if d.result["name"] in taken:
            messagebox.showerror("추가 실패", f"이름 '{d.result['name']}'은 이미 있습니다.")
            return
        self.cfg.setdefault("instances", []).append(d.result)
        save_instances(self.cfg)
        self.log("차일드", f"추가 — [{d.result['name'] or '(기본)'}] {d.result['label']} "
                          f"({d.result['kind']}) {d.result['exePath'] or '경로 미설정'}", "ok")

    def _sel_one(self):
        sel = self.tree.selection()
        if len(sel) != 1:
            messagebox.showinfo("선택", "차일드 하나만 고르세요.")
            return None
        name = "" if sel[0] == "__default__" else sel[0]
        for i in self.cfg.get("instances", []):
            if i.get("name", "") == name:
                return i
        return None

    def edit_child(self):
        inst = self._sel_one()
        if inst is None:
            return
        taken = {i.get("name", "") for i in self.cfg["instances"]} - {inst.get("name", "")}
        d = ChildDialog(self, inst, taken)
        self.wait_window(d)
        if d.result:
            inst.update(d.result)
            save_instances(self.cfg)
            self.log("차일드", f"편집 — [{d.result['name'] or '(기본)'}] {d.result['label']} "
                              f"({d.result['kind']}) {d.result['exePath'] or '경로 미설정'}")

    def dup_child(self):
        inst = self._sel_one()
        if inst is None:
            return
        taken = {i.get("name", "") for i in self.cfg["instances"]}
        d = ChildDialog(self, None, taken)
        d.v_kind.set(inst.get("kind", "build"))
        d.v_exe.set(inst.get("exePath", ""))
        d.v_args.set(" ".join(inst.get("extraArgs", [])))
        self.wait_window(d)
        if d.result and d.result["name"] not in taken:
            self.cfg["instances"].append(d.result)
            save_instances(self.cfg)

    def del_child(self):
        inst = self._sel_one()
        if inst is None:
            return
        if not messagebox.askyesno("삭제", f"차일드 '{inst.get('name') or '(기본)'}'을 목록에서 지울까요?"):
            return
        self.cfg["instances"].remove(inst)
        save_instances(self.cfg)
        self.log("차일드", f"삭제 — [{inst.get('name') or '(기본)'}] {inst.get('label','')}", "warn")

    # ── 기타 ────────────────────────────────────────────────
    def open_data_dir(self):
        d = orch.data_dir(self.cfg)
        d.mkdir(parents=True, exist_ok=True)
        os.startfile(str(d))

    def change_data_dir(self):
        p = filedialog.askdirectory(title="QA 데이터 폴더 (Application.persistentDataPath)",
                                    initialdir=str(orch.data_dir(self.cfg)))
        if p:
            self.cfg["dataDir"] = p
            save_instances(self.cfg)
            self.lb_dir.config(text=p)

    def make_claude_report(self):
        """Claude가 항상 같은 경로만 읽으면 되게 — 요약 마크다운을 떨군다."""
        s = self.summary or {}
        lines = [f"# QA 결과 보고 ({datetime.now():%Y-%m-%d %H:%M})", "",
                 f"- 데이터: `{s.get('dataDir','')}`",
                 f"- 최근 런 {s.get('runCount',0)}건 · 통과 {s.get('pass',0)} · 미통과 {s.get('fail',0)}", ""]

        lines += ["## 런 목록", "", "| 시각 | 차일드 | 판정 | 사이클 | 오류/경고 | 사유 |", "|---|---|---|---|---|---|"]
        for r in s.get("runs", [])[:12]:
            lines.append(f"| {r['at']} | {r['instance'] or '(기본)'} | {r['verdict']} | {r['cycles']} "
                         f"| {r['errors']}/{r['warns']} | {(r['reason'] or '').replace('|','/')} |")

        if s.get("failedChecks"):
            lines += ["", "## 반복 실패 판정", ""]
            lines += [f"- {k} — {v}회" for k, v in s["failedChecks"].items()]
        if s.get("topAnomalies"):
            lines += ["", "## 이상 빈도", ""]
            lines += [f"- {k} — {v}" for k, v in s["topAnomalies"].items()]
        if s.get("problemSpots"):
            lines += ["", "## 문제 지점", ""]
            lines += [f"- {x['scene']} ({x['x']},{x['y']}) 스턱{x['stuck']} 막힘{x['unreachable']} 사망{x['deaths']}"
                      for x in s["problemSpots"][:20]]

        runs = s.get("runs", [])
        if runs:
            lines += ["", "## 최신 런 상세 (판정 항목·이상)", ""]
            r = runs[0]
            for c in r.get("checks", []):
                lines.append(f"- {'PASS' if c.get('passed') else 'FAIL'} · {c.get('name')} — {c.get('detail','')}")
            lines.append("")
            for a in r.get("anomalies", []):
                lines.append(f"- [{a.get('atSec',0):.1f}s] {a.get('level')} {a.get('step')} "
                             f"{a.get('kind')} — {a.get('msg')} @{a.get('playerPos','')} {a.get('scene','')}")

        if self.reports:
            lines += ["", "## 원본 전체 로그", "", f"`{self.reports[0]['file']}`"]

        CLAUDE_REPORT.write_text("\n".join(lines), encoding="utf-8")
        self.log("보고서", f"Claude 보고서 생성 — {CLAUDE_REPORT.name} "
                          f"(런 {s.get('runCount', 0)}건, 통과 {s.get('pass', 0)}/미통과 {s.get('fail', 0)})")
        messagebox.showinfo("보고서 생성",
                            f"{CLAUDE_REPORT}\n\n이 경로를 Claude가 읽습니다.\n"
                            "채팅에 'QA 결과 봐줘'만 치면 됩니다.")

    def _quit(self):
        self.watcher.stop_flag.set()
        self.destroy()


if __name__ == "__main__":
    MotherApp().mainloop()
