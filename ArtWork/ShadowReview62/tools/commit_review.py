"""Commit only this shadow adjustment through an isolated index; preserve shared work."""
import os,re,subprocess,tempfile
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
branch='refs/heads/codex/hideout-reference-art-pass'
def git(*args,env=None,data=None):
 return subprocess.check_output(['git',*args],cwd=repo,env=env,input=data).decode('utf8').strip()
parent=git('rev-parse',branch);checkout=git('symbolic-ref','HEAD');shared_index=git('diff','--cached','--binary')
def base(path):return subprocess.check_output(['git','show',parent+':'+path],cwd=repo).decode('utf8').replace('\r\n','\n')
def current(path):return (repo/path).read_text(encoding='utf8')
fd,name=tempfile.mkstemp(prefix='shadow-review-index-');os.close(fd);Path(name).unlink();env={**os.environ,'GIT_INDEX_FILE':name}
try:
 git('read-tree',parent,env=env)
 git('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--','ArtWork/ShadowReview62',env=env)
 def put(path,text):
  blob=git('hash-object','-w','--stdin',data=text.encode('utf8'));git('update-index','--cacheinfo','100644',blob,path,env=env)
 for path,keys in {
  'demo13-flashlight/Assets/Resources/Data/WeatherData.asset':['daySunAngle'],
  'demo13-flashlight/Assets/Settings/URP-3D.asset':['m_ShadowCascadeCount','m_Cascade4Split','m_ShadowDepthBias','m_ShadowNormalBias']
 }.items():
  text=base(path);now=current(path)
  for key in keys:
   pattern=rf'(?m)^  {key}: .*';replacement=re.search(pattern,now).group();text,count=re.subn(pattern,lambda _:replacement,text);assert count==1
  put(path,text)
 path='demo13-flashlight/docs/rendering.md';text=base(path);now=current(path)
 start='## 2026-09-11 — NPC 그림자 피드백\n';end='## 2026-09-11 — NPC 적용 후 표현 작업 우선순위 검토\n'
 section=start+now.split(start,1)[1].split(end,1)[0];assert start not in text
 text=text.replace(end,section+end,1)
 old='밤 WeatherData와 태양 각도는 그대로다.';new='밤 WeatherData는 그대로이며, 낮 태양 각도는 후속 그림자 피드백에 따라 위의 58° 기준을 적용한다.'
 assert old in text;text=text.replace(old,new,1)
 log=next(line for line in now.splitlines() if line.startswith('- 2026-09-11: “그림자도 이상하네”'))
 text=text.replace('## 변경 로그\n','## 변경 로그\n\n'+log+'\n',1);put(path,text)
 tree=git('write-tree',env=env);git('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree)
 print(git('diff','--stat',parent,tree),flush=True)
 commit=git('commit-tree',tree,'-p',parent,'-m','Shorten daytime shadows and refine top-down cascade quality')
 git('update-ref',branch,commit,parent)
 assert git('symbolic-ref','HEAD')==checkout and git('diff','--cached','--binary')==shared_index
 print('LOCAL_COMMIT',commit,'SHARED_CHECKOUT_PRESERVED',checkout,flush=True)
finally:Path(name).unlink(missing_ok=True)
