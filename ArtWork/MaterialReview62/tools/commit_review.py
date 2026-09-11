"""Isolate this material pass from concurrent checkout/index changes."""
import os,subprocess,tempfile,json
from pathlib import Path
repo=Path(__file__).resolve().parents[3];stage=repo/'ArtWork/MaterialReview62';branch='refs/heads/codex/hideout-reference-art-pass'
def g(*a,env=None,data=None):return subprocess.check_output(['git',*a],cwd=repo,env=env,input=data).decode('utf8').strip()
parent=g('rev-parse',branch);head=g('symbolic-ref','HEAD');shared=g('diff','--cached','--binary')
paths=['ArtWork/MaterialReview62','demo13-flashlight/Assets/Shaders/GameLit/GameLit.shader','demo13-flashlight/Assets/Editor/GameLitConverter.cs','demo13-flashlight/Assets/Editor/GameLitConverter.cs.meta','demo13-flashlight/Assets/Settings/ForwardRenderer.asset']
paths+=['demo13-flashlight/'+x['path'] for x in json.loads((stage/'AppliedMaterials.json').read_text(encoding='utf8'))]
paths+=['demo13-flashlight/'+x for x in json.loads((stage/'AppliedCharacters.json').read_text(encoding='utf8'))]
fd,name=tempfile.mkstemp(prefix='material-review-index-');os.close(fd);Path(name).unlink();env={**os.environ,'GIT_INDEX_FILE':name}
try:
 g('read-tree',parent,env=env);g('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--',*paths,env=env)
 path='demo13-flashlight/docs/rendering.md';text=subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8').replace('\r\n','\n');now=(repo/path).read_text(encoding='utf8')
 start='## 2026-09-11 — 마을 바닥·프랍 작업 전 셰이더 우선순위 재검토\n';end='## 2026-09-11 — NPC 그림자 피드백\n'
 assert start in text and end in text
 text=text[:text.index(start)]+start+now.split(start,1)[1].split(end,1)[0]+text[text.index(end):]
 log=next(x for x in now.splitlines() if x.startswith('- 2026-09-11: “찰흙 같아서 순서대로 해보자”'))
 text=text.replace('## 변경 로그\n','## 변경 로그\n\n'+log+'\n',1)
 blob=g('hash-object','-w','--stdin',data=text.encode('utf8'));g('update-index','--cacheinfo','100644',blob,path,env=env)
 tree=g('write-tree',env=env);g('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree)
 print(g('diff','--shortstat',parent,tree),flush=True)
 commit=g('commit-tree',tree,'-p',parent,'-m','Restore material response and restrained contact shading for top-down art')
 g('update-ref',branch,commit,parent)
 assert head==g('symbolic-ref','HEAD') and shared==g('diff','--cached','--binary')
 print('LOCAL_COMMIT',commit,'SHARED_CHECKOUT_PRESERVED',head,flush=True)
finally:Path(name).unlink(missing_ok=True)
