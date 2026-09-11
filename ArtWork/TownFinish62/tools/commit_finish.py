import os,subprocess,tempfile,json
from pathlib import Path
repo=Path('D:/Demo');stage=repo/'ArtWork/TownFinish62';branch='refs/heads/codex/hideout-reference-art-pass'
def g(*a,env=None,data=None):return subprocess.check_output(['git',*a],cwd=repo,env=env,input=data).decode('utf8').strip()
parent=g('rev-parse',branch);assert parent=='27650fd49f7f021a39ac1c524de9fe56f59f58e3'
head=g('symbolic-ref','HEAD');shared=g('diff','--cached','--binary')
scene='demo13-flashlight/Assets/Scenes/Safehouse.unity'
assert subprocess.check_output(['git','show',parent+':'+scene],cwd=repo).decode().replace('\r\n','\n')==(stage/'BeforeFiles/Safehouse.unity').read_text('utf8')
fd,name=tempfile.mkstemp(prefix='town-finish-index-');os.close(fd);Path(name).unlink();env={**os.environ,'GIT_INDEX_FILE':name}
def doc(path,start,end,log_prefix=None):
    original=subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8').replace('\r\n','\n');current=(repo/path).read_text('utf8')
    section=start+current.split(start,1)[1].split(end,1)[0]
    assert start not in original
    if end in original:original=original.replace(end,section+end,1)
    else:original=original.split('\n',1)[0]+'\n\n'+section+original.split('\n',1)[1].lstrip('\n')
    if log_prefix:
        log=next(x for x in current.splitlines() if x.startswith(log_prefix));original=original.replace('## 변경 로그\n','## 변경 로그\n\n'+log+'\n',1)
    blob=g('hash-object','-w','--stdin',data=original.encode());g('update-index','--cacheinfo','100644',blob,path,env=env)
try:
    g('read-tree',parent,env=env)
    g('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--','ArtWork/TownFinish62','demo13-flashlight/Assets/Art/Environments/TownFinish62.meta','demo13-flashlight/Assets/Art/Environments/TownFinish62',scene,env=env)
    doc('demo13-flashlight/docs/safehouse.md','## 2026-09-11 — 마을 마감·다른 PC 인계\n','## 2026-09-11 — 그림자 보정 후 다음 아트 단계 검토\n','- 2026-09-11: 끝까지 마감·집에서 인계')
    doc('demo13-flashlight/docs/dev-handoff.md','## 2026-09-11 — 집에서 이어받기: 아트 마감\n','> 다른 PC·새 세션에서 이어서 작업할 때 **여기부터** 읽기.\n')
    doc('demo13-flashlight/docs/dev-roadmap.md','## 2026-09-11 — 아트 마감 인계\n','## 시스템 정리 (2026-09-11~, 진행 중)\n')
    path='demo13-flashlight/docs/MASTER.md';original=subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8');original=original.replace('| 방향 확정 + 확장 기획 |','| 방향 확정 · 전당포 앞 마감/Play 검수 완료 (2026-09-11) |');blob=g('hash-object','-w','--stdin',data=original.encode());g('update-index','--cacheinfo','100644',blob,path,env=env)
    tree=g('write-tree',env=env);g('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree)
    print(g('diff','--shortstat',parent,tree),flush=True)
    commit=g('commit-tree',tree,'-p',parent,'-m','Finish pawnshop forecourt and document home PC handoff');g('update-ref',branch,commit,parent)
    assert head==g('symbolic-ref','HEAD') and shared==g('diff','--cached','--binary');print('LOCAL_COMMIT',commit,flush=True)
finally:Path(name).unlink(missing_ok=True)
