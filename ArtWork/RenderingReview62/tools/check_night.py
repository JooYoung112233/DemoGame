import subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
wrapper=root.parents[1]/'demo13-flashlight/tools/unity_mcp_call.py'
def run(*args):
 p=subprocess.run([sys.executable,*map(str,args)],timeout=180)
 if p.returncode:raise RuntimeError('MCP review failed')
run(wrapper,'eval',root/'tools/night.json')
try:run(root/'tools/capture_views.py','Night')
finally:run(wrapper,'eval',root/'tools/restore_day.json')
