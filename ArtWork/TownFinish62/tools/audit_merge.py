from pathlib import Path
import json,subprocess
work=Path('D:/DemoArtMerge');source=Path('D:/Demo')
paths=subprocess.check_output(['git','ls-files','demo13-flashlight/Assets','demo13-flashlight/Packages/manifest.json'],cwd=work).decode().splitlines()
code=[p for p in paths if p.endswith(('.cs','.hlsl','.shader','.asmdef')) or p.endswith('/manifest.json')]
different=[]
for p in code:
    a=work/p;b=source/p
    if not b.exists() or a.read_bytes().replace(b'\r\n',b'\n')!=b.read_bytes().replace(b'\r\n',b'\n'):different.append(p)
deps=json.loads((source/'ArtWork/TownFinish62/Dependencies.json').read_text('utf8'))
missing=[p for p in deps if p.startswith('Assets/') and not (work/'demo13-flashlight'/p).exists()]
missing_meta=[p for p in deps if p.startswith('Assets/') and (work/'demo13-flashlight'/p).is_file() and not (work/'demo13-flashlight'/(p+'.meta')).exists()]
large=sorted(((work/p).stat().st_size,p) for p in paths if (work/p).is_file())[-5:]
report={'sourceFilesCompared':len(code),'compiledSourceDifferences':different,'sceneDependencies':len(deps),'missingDependencies':missing,'missingMetadata':missing_meta,'largestAssets':large}
(source/'ArtWork/TownFinish62/MergeValidation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
assert not different and not missing and not missing_meta
