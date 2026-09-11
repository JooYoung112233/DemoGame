"""Resolve reviewed art conflicts in the isolated integration checkout."""
from pathlib import Path
import subprocess
work=Path('D:/DemoArtMerge'); source=Path('D:/Demo')
def git(*a):return subprocess.check_output(['git',*a],cwd=work).decode().strip()
conflicts=git('diff','--name-only','--diff-filter=U').splitlines()
combined=['demo13-flashlight/Assets/Resources/Data/WeatherData.asset','demo13-flashlight/Assets/Settings/URP-3D.asset','demo13-flashlight/docs/MASTER.md','demo13-flashlight/docs/rendering.md']
expected=['demo13-flashlight/Assets/ChibiSurvivor/','demo13-flashlight/Assets/Editor/GameLitConverter.cs','demo13-flashlight/Assets/Shaders/GameLit/','demo13-flashlight/Assets/Resources/PlayerRigVolume3D.asset']
for p in conflicts:
    if p in combined:
        (work/p).write_bytes((source/p).read_bytes())
        git('add','--',p)
    else:
        assert any(p.startswith(x) for x in expected),p
        git('restore','--source=HEAD','--staged','--worktree','--',p)
assert not git('diff','--name-only','--diff-filter=U')
print('Resolved',len(conflicts),'conflicts; art material/mask/SSAO values retained; current main night/HDR/shadow distance and WornLamp retained.')
