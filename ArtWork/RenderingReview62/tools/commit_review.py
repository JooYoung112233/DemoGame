"""Build an isolated Git commit; never modify the live checkout/index or Unity YAML.

Shared Weather/Volume/docs have unrelated earlier edits. Construct only our changed
fields in Git blobs, preserving the art branch's existing identifiers and other data.
"""
import os,re,subprocess,tempfile
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
branch='refs/heads/codex/hideout-reference-art-pass'
def git(*args,env=None,data=None):
 return subprocess.check_output(['git',*args],cwd=repo,env=env,input=data).decode('utf-8').strip()
parent=git('rev-parse',branch);checkout=git('symbolic-ref','HEAD');original_index=git('diff','--cached','--binary')
def base(path):return subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf-8').replace('\r\n','\n')
def current(path):return (repo/path).read_text(encoding='utf-8')
fd,name=tempfile.mkstemp(prefix='town-rendering-index-');os.close(fd);Path(name).unlink();env={**os.environ,'GIT_INDEX_FILE':name}
try:
 git('read-tree',parent,env=env)
 paths=['ArtWork/RenderingReview62','ArtWork/StoryNPC62/README.md','demo13-flashlight/docs/char-art.md','demo13-flashlight/Assets/Shaders/GameLit.meta','demo13-flashlight/Assets/Shaders/GameLit','demo13-flashlight/Assets/Art/Environments/Town02/Rendering62.meta','demo13-flashlight/Assets/Art/Environments/Town02/Rendering62','demo13-flashlight/Assets/Scenes/Safehouse.unity']
 paths += [f'demo13-flashlight/Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/{n}.mat' for n in ('Pawnshop','VeteranScavenger','DistrictWarden','WanderingMerchant')]
 git('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--',*paths,env=env)
 def put(path,text):
  blob=git('hash-object','-w','--stdin',data=text.encode('utf-8'));git('update-index','--cacheinfo','100644',blob,path,env=env)
 path='demo13-flashlight/Assets/Resources/Data/WeatherData.asset';text=base(path);now=current(path)
 for key in ('dayIntensity','dayLightColor','dayAmbientColor'):
  pattern=rf'(?m)^  {key}: .*';replacement=re.search(pattern,now).group();text,count=re.subn(pattern,lambda _:replacement,text);assert count==1
 put(path,text)
 path='demo13-flashlight/Assets/Resources/PlayerRigVolume3D.asset';parts=re.split(r'(?m)(?=^--- !u!)',base(path));nowparts=re.split(r'(?m)(?=^--- !u!)',current(path))
 for kind,fields in {'ColorAdjustments':['postExposure','contrast','saturation'],'Bloom':['intensity','threshold'],'FilmGrain':['intensity'],'Vignette':['intensity']}.items():
  idx=next(i for i,p in enumerate(parts) if f'  m_Name: {kind}\n' in p);now=next(p for p in nowparts if f'  m_Name: {kind}\n' in p)
  for key in fields:
   pattern=rf'(?m)^  {key}:\n    m_OverrideState: [^\n]+\n    m_Value: [^\n]+'
   replacement=re.search(pattern,now).group();parts[idx],count=re.subn(pattern,lambda _:replacement,parts[idx]);assert count==1
 put(path,''.join(parts))
 path='demo13-flashlight/docs/rendering.md';text=base(path);now=current(path)
 start='## 2026-09-11 — NPC 적용 후 표현 작업 우선순위 검토\n';end='## 2026-09-11 — 하이드아웃'
 section=start+now.split(start,1)[1].split(end,1)[0]
 text=text[:text.index(start)]+section+text[text.index(end):]
 log=next(line for line in now.splitlines() if line.startswith('- 2026-09-11: “진행해보자”'))
 text=text.replace('## 변경 로그\n','## 변경 로그\n\n'+log+'\n',1);put(path,text)
 tree=git('write-tree',env=env);git('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree)
 print(git('diff','--shortstat',parent,tree),flush=True)
 commit=git('commit-tree',tree,'-p',parent,'-m','Refine town lighting and materials with opt-in packed GameLit surface masks')
 git('update-ref',branch,commit,parent)
 assert git('symbolic-ref','HEAD')==checkout and git('diff','--cached','--binary')==original_index
 print('LOCAL_COMMIT',commit,'SHARED_CHECKOUT_PRESERVED',checkout,flush=True)
finally:Path(name).unlink(missing_ok=True)
