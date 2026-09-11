"""Commit this integration to the art branch without switching the shared checkout."""
import os, subprocess, tempfile
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
branch='refs/heads/codex/hideout-reference-art-pass'
def git(*args,env=None):
    return subprocess.check_output(['git',*args],cwd=repo,env=env).decode().strip()
parent=git('rev-parse',branch)
checkout=git('symbolic-ref','HEAD')
original_index=git('diff','--cached','--binary')
fd,name=tempfile.mkstemp(prefix='story-npc-integration-index-');os.close(fd);Path(name).unlink()
env={**os.environ,'GIT_INDEX_FILE':name}
try:
    git('read-tree',parent,env=env)
    paths=['ArtWork/StoryNPC62','demo13-flashlight/Assets/ChibiSurvivor/NPC.meta','demo13-flashlight/Assets/ChibiSurvivor/NPC','demo13-flashlight/Assets/Scenes/Safehouse.unity','demo13-flashlight/docs/char-art.md']
    paths += [f'demo13-flashlight/Assets/Resources/NPC/{identity}.prefab' for identity in ('pawnshop','veteran_scavenger','district_warden','wandering_merchant')]
    git('-c','core.autocrlf=true','-c','core.safecrlf=false','add','--',*paths,env=env)
    tree=git('write-tree',env=env)
    # Unity serializes empty YAML string values with a trailing space.
    git('-c','core.whitespace=-blank-at-eol','diff','--check',parent,tree,env=env)
    print(git('diff','--stat',parent,tree),flush=True)
    commit=git('commit-tree',tree,'-p',parent,'-m','Apply four story NPCs to Safehouse with looping idles and restore roof links',env=env)
    git('update-ref',branch,commit,parent,env=env)
    assert git('symbolic-ref','HEAD')==checkout
    assert git('diff','--cached','--binary')==original_index
    print('LOCAL_COMMIT',commit,'CHECKOUT_PRESERVED',checkout,flush=True)
finally:
    Path(name).unlink(missing_ok=True)
