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

POLL_SEC = 1.0
FONT_UI = ("맑은 고딕", 9)
FONT_UI_B = ("맑은 고딕", 9, "bold")
FONT_MONO = ("Consolas", 9)          # ASCII 히트맵 정렬용(한글은 폰트 링크로 대체됨)
CLAUDE_REPORT = HERE / "qa-report-for-claude.md"

# 상태별 색 — 트리 태그로 쓴다
C_RUN = "#1a7f37"
C_DEAD = "#8b949e"
C_FAIL = "#c1121f"
C_PASS = "#1a7f37"
C_BLOCK = "#b45309"


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
        self.q.put(("status", orch.status_rows(cfg)))

        # 결과 파일이 바뀌었을 때만 무거운 집계를 돈다
        d = orch.data_dir(cfg)
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
                  font=FONT_UI, foreground="#666").grid(row=r, column=2, sticky="w", padx=6)
        r += 1

        ttk.Label(f, text="종류", font=FONT_UI).grid(row=r, column=0, sticky="w", pady=3)
        cb = ttk.Combobox(f, textvariable=self.v_kind, values=["build", "editor"],
                          width=10, state="readonly", font=FONT_UI)
        cb.grid(row=r, column=1, sticky="w")
        ttk.Label(f, text="build=마더가 직접 실행 / editor=사람이 F9",
                  font=FONT_UI, foreground="#666").grid(row=r, column=2, sticky="w", padx=6)
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
                  font=FONT_UI, foreground="#666").grid(row=r, column=1, columnspan=3, sticky="w", pady=(0, 8))
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
        self.geometry("1360x860")
        self.minsize(1100, 700)

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
        self._header()
        self._toolbar()
        self._banner()
        self._body()
        self._statusbar()

        self.watcher.start()
        self.after(200, self._drain)
        self.protocol("WM_DELETE_WINDOW", self._quit)

    # ── 외형 ────────────────────────────────────────────────
    def _style(self):
        s = ttk.Style(self)
        try:
            s.theme_use("clam")
        except tk.TclError:
            pass
        s.configure("Treeview", font=FONT_UI, rowheight=23)
        s.configure("Treeview.Heading", font=FONT_UI_B)
        s.configure("TButton", font=FONT_UI, padding=3)
        s.configure("TLabel", font=FONT_UI)
        s.configure("TCheckbutton", font=FONT_UI)
        s.configure("Big.TButton", font=FONT_UI_B, padding=4)

    def _header(self):
        h = ttk.Frame(self, padding=(10, 8, 10, 4))
        h.pack(fill="x")
        ttk.Label(h, text="QA 마더", font=("맑은 고딕", 13, "bold")).pack(side="left")
        self.lb_watch = ttk.Label(h, text="● 감시중", foreground=C_RUN, font=FONT_UI_B)
        self.lb_watch.pack(side="left", padx=(10, 0))

        ttk.Button(h, text="폴더 열기", width=10, command=self.open_data_dir).pack(side="right")
        ttk.Button(h, text="변경", width=6, command=self.change_data_dir).pack(side="right", padx=4)
        self.lb_dir = ttk.Label(h, text=str(orch.data_dir(self.cfg)), foreground="#555")
        self.lb_dir.pack(side="right", padx=6)
        ttk.Label(h, text="데이터:", foreground="#555").pack(side="right")

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
        ttk.Label(g3, text="(0=시나리오 기본)", foreground="#666").pack(side="left", padx=(3, 8))
        self.v_seed = tk.BooleanVar(value=True)
        ttk.Checkbutton(g3, text="차일드별 시드 분산", variable=self.v_seed).pack(side="left", padx=4)
        ttk.Button(g3, text="▶ 선택에 투입", style="Big.TButton",
                   command=lambda: self.dispatch(False)).pack(side="left", padx=(10, 2))
        ttk.Button(g3, text="▶▶ 전체 투입", style="Big.TButton",
                   command=lambda: self.dispatch(True)).pack(side="left", padx=2)

        self.reload_scenarios()

    def _banner(self):
        """막힘 배너 — 차일드가 '못 하겠다'고 물어오면 여기서 답한다."""
        self.banner = tk.Frame(self, bg="#fff4e5", highlightthickness=1, highlightbackground="#f0a13a")
        self.lb_banner = tk.Label(self.banner, text="", bg="#fff4e5", fg="#7c4a03",
                                  font=FONT_UI_B, anchor="w", justify="left")
        self.lb_banner.pack(side="left", padx=10, pady=6, fill="x", expand=True)
        for txt, act in (("재시도", "retry"), ("건너뛰기", "skip"), ("중단", "abort")):
            tk.Button(self.banner, text=txt, font=FONT_UI, width=8,
                      command=lambda a=act: self.resume(a)).pack(side="right", padx=4, pady=5)
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
        self.tab_cov = self._tab_coverage()
        self.tab_raw = self._tab_raw()

    def _mono_text(self, parent):
        fr = ttk.Frame(parent)
        txt = tk.Text(fr, font=FONT_MONO, wrap="none", bg="#fbfbfb",
                      relief="flat", borderwidth=1, highlightthickness=1, highlightbackground="#ddd")
        ys = ttk.Scrollbar(fr, orient="vertical", command=txt.yview)
        xs = ttk.Scrollbar(fr, orient="horizontal", command=txt.xview)
        txt.configure(yscrollcommand=ys.set, xscrollcommand=xs.set)
        txt.grid(row=0, column=0, sticky="nsew")
        ys.grid(row=0, column=1, sticky="ns")
        xs.grid(row=1, column=0, sticky="ew")
        fr.rowconfigure(0, weight=1)
        fr.columnconfigure(0, weight=1)
        txt.tag_configure("err", foreground=C_FAIL)
        txt.tag_configure("warn", foreground="#a35c00")
        txt.tag_configure("ok", foreground=C_PASS)
        txt.tag_configure("head", font=("맑은 고딕", 10, "bold"))
        txt.tag_configure("dim", foreground="#777")
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
                  foreground="#666").pack(side="left", padx=6)
        self.cb_raw = ttk.Combobox(top, width=46, state="readonly", font=FONT_UI)
        self.cb_raw.pack(side="right")
        self.cb_raw.bind("<<ComboboxSelected>>", lambda e: self.show_raw())

        fr, self.tx_raw = self._mono_text(f)
        fr.pack(fill="both", expand=True, pady=(4, 0))
        return f

    def _statusbar(self):
        s = ttk.Frame(self, padding=(10, 3))
        s.pack(fill="x")
        self.lb_stat = ttk.Label(s, text="—", font=FONT_UI)
        self.lb_stat.pack(side="left")
        ttk.Button(s, text="Claude 보고서 생성", command=self.make_claude_report).pack(side="right")
        self.lb_time = ttk.Label(s, text="", foreground="#777")
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
        self.lb_stat.config(
            text=f"차일드 {len(rows)}  ·  실행중 {n_run}  ·  통과 {p}  ·  미통과 {f}"
                 + (f"  ·  ⛔막힘 {blk}" if blk else ""))

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
        self.nb.select(self.tab_live)
        messagebox.showinfo("투입 완료",
                            f"시나리오 '{res['scenario']}' → {who}\n\n"
                            "※ 차일드가 -qa-serve 로 떠 있어야 명령을 집어갑니다.\n"
                            "   Unity 에디터는 명령 대기 모드가 아니므로 F9로 직접 돌립니다.")

    def launch(self):
        res = orch.launch_instances(self.cfg, self.sel_names())
        msg = "\n".join(f"기동: {l['instance'] or '(기본)'} pid={l['pid']}" for l in res["launched"])
        msg += "\n" + "\n".join(f"스킵: {s['instance'] or '(기본)'} — {s['why']}" for s in res["skipped"])
        messagebox.showinfo("기동", msg.strip() or "대상 없음")

    def kill(self):
        names = self.sel_names()
        if not messagebox.askyesno("종료", f"{names or '전체'} 차일드 프로세스를 종료할까요?"):
            return
        killed = orch.kill_instances(self.cfg, names)
        messagebox.showinfo("종료", f"종료: {killed or '없음'}")

    def clear_stale(self):
        removed = orch.clear_stale(self.cfg, self.sel_names())
        messagebox.showinfo("잔재 정리", "\n".join(removed) if removed else "지울 잔재 없음")

    def resume(self, action):
        rows = [r for r in self.rows if r["blocked"]]
        names = ",".join(r["name"] for r in rows)
        orch.write_resume(self.cfg, names, action, "마더 GUI 응답")

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
        messagebox.showinfo("보고서 생성",
                            f"{CLAUDE_REPORT}\n\n이 경로를 Claude가 읽습니다.\n"
                            "채팅에 'QA 결과 봐줘'만 치면 됩니다.")

    def _quit(self):
        self.watcher.stop_flag.set()
        self.destroy()


if __name__ == "__main__":
    MotherApp().mainloop()
