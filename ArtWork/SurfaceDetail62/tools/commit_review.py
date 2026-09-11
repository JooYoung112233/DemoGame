"""Commit only this pass; preserve the concurrent shared checkout and index."""
import os, subprocess, tempfile, json
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
stage=repo/'ArtWork/SurfaceDetail62'
branch='refs/heads/codex/hideout-reference-art-pass'
def g(*args,env=None,data=None):
    return subprocess.check_output(['git',*args],cwd=repo,env=env,input=data).decode('utf8').strip()
parent=g('rev-parse',branch)
assert parent=='c65b5eeaa6a497eb01e36a48eaf12cabbc0c75a5', 'Art branch changed; review before proceeding'
head=g('symbolic-ref','HEAD'); shared=g('diff','--cached','--binary')
entries=json.loads((stage/'AppliedMaterials.json').read_text('utf8'))
paths=['ArtWork/SurfaceDetail62','demo13-flashlight/Assets/Art/SurfaceDetail62.meta','demo13-flashlight/Assets/Art/SurfaceDetail62']
paths+=['demo13-flashlight/'+e['path'] for e in entries]
# Reject pre-existing concurrent material changes before staging whole files.
for e in entries:
    path='demo13-flashlight/'+e['path']
    prior=subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8').replace('\r\n','\n')
    backup=(stage/'BeforeFiles'/Path(path).name).read_text('utf8')
    assert prior==backup, 'Concurrent material edits need isolated hunks: '+path
fd,index=tempfile.mkstemp(prefix='surface-detail-index-');os.close(fd);Path(index).unlink()
env={**os.environ,'GIT_INDEX_FILE':index}
try:
    g('read-tree',parent,env=env)
    g('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--',*paths,env=env)
    path='demo13-flashlight/docs/rendering.md'
    prior=subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8').replace('\r\n','\n')
    current=(repo/path).read_text('utf8')
    start='## 2026-09-11 — 원본 표면 질감 보완 착수\n'
    end='## 2026-09-11 — 마을 바닥·프랍 작업 전 셰이더 우선순위 재검토\n'
    assert start not in prior and end in prior
    section=start+current.split(start,1)[1].split(end,1)[0]
    prior=prior.replace(end,section+end,1)
    log=next(line for line in current.splitlines() if line.startswith('- 2026-09-11: 후속 표면 질감 승인에 따라'))
    prior=prior.replace('## 변경 로그\n','## 변경 로그\n\n'+log+'\n',1)
    blob=g('hash-object','-w','--stdin',data=prior.encode('utf8'))
    g('update-index','--cacheinfo','100644',blob,path,env=env)
    tree=g('write-tree',env=env)
    g('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree)
    print(g('diff','--shortstat',parent,tree),flush=True)
    print(g('diff',parent,tree,'--',*paths[3:]),flush=True)
    commit=g('commit-tree',tree,'-p',parent,'-m','Add reviewed wood steel and warden base-color detail')
    g('update-ref',branch,commit,parent)
    assert head==g('symbolic-ref','HEAD') and shared==g('diff','--cached','--binary')
    print('LOCAL_COMMIT',commit,'SHARED_CHECKOUT_PRESERVED',head,flush=True)
finally:
    Path(index).unlink(missing_ok=True)
