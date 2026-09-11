"""Call the Unity CLI's official MCP stdio server, without desktop/UI automation.
Usage: python unity_mcp_call.py [tool-name [arguments.json]]
No tool name lists the connected server's tools. Project is pinned to this repository.
"""
import json,subprocess,sys,threading,queue
from pathlib import Path
sys.stdout.reconfigure(encoding='utf8')

project=Path(__file__).resolve().parents[1]
proc=subprocess.Popen(['unity','mcp','--project-path',str(project)],stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,encoding='utf8',
    creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
messages=queue.Queue()
def read():
    for line in proc.stdout:
        try:messages.put(json.loads(line))
        except json.JSONDecodeError:pass
threading.Thread(target=read,daemon=True).start()
def send(message):proc.stdin.write(json.dumps(message)+'\n');proc.stdin.flush()
def request(identity,method,params):
    send({'jsonrpc':'2.0','id':identity,'method':method,'params':params})
    while True:
        response=messages.get(timeout=55)
        if response.get('id')==identity:return response
try:
    init=request(1,'initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'HideoutAssetReview','version':'1.0'}})
    if 'error' in init:raise RuntimeError(init['error'])
    send({'jsonrpc':'2.0','method':'notifications/initialized'})
    if len(sys.argv)==1:
        result=request(2,'tools/list',{})
        entries=result.get('result',{}).get('tools',[])
        if not entries:raise RuntimeError('Unity MCP returned no tools; check editor discovery for this project.')
        result={'names':[t['name'] for t in entries],'schemas':[t for t in entries if t['name'] in ['eval_file','eval','editor_state','console_get_logs']]}
    else:
        arguments=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else {}
        result=request(2,'tools/call',{'name':sys.argv[1],'arguments':arguments})
    print(json.dumps(result,ensure_ascii=False))
    if 'error' in result or result.get('result',{}).get('isError'):sys.exit(1)
finally:
    proc.terminate()
    try:proc.wait(timeout=3)
    except subprocess.TimeoutExpired:proc.kill()
