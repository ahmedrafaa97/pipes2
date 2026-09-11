"""Local JSON-RPC client for the project's Unity MCP HTTP bridge."""
import json
import sys
import urllib.request
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
SESSION = BASE / 'Logs' / 'mcp-session.json'
URL = 'http://127.0.0.1:8080/mcp'

def request(method, params=None, session=None, notification=False):
    payload = {'jsonrpc': '2.0', 'method': method}
    if not notification:
        payload['id'] = 1
    if params is not None:
        payload['params'] = params
    headers = {'Content-Type': 'application/json', 'Accept': 'application/json, text/event-stream', 'MCP-Protocol-Version': '2024-11-05'}
    if session:
        headers['Mcp-Session-Id'] = session
    req = urllib.request.Request(URL, data=json.dumps(payload).encode(), headers=headers)
    with urllib.request.urlopen(req, timeout=120) as response:
        body = response.read().decode()
        sid = response.headers.get('Mcp-Session-Id', session)
        if not body.strip():
            return {}, sid
        data_lines = [line[5:].strip() for line in body.splitlines() if line.startswith('data:')]
        if data_lines:
            messages = [json.loads(line) for line in data_lines]
            replies = [message for message in messages if message.get('id') == 1]
            return (replies or messages)[-1], sid
        return json.loads(body), sid

def connect():
    result, sid = request('initialize', {'protocolVersion': '2024-11-05', 'capabilities': {}, 'clientInfo': {'name': 'AquaPath-Codex', 'version': '1.0'}})
    request('notifications/initialized', session=sid, notification=True)
    SESSION.write_text(json.dumps({'session': sid}))
    return sid, result

if __name__ == '__main__':
    method = sys.argv[1] if len(sys.argv) > 1 else 'initialize'
    if method == 'initialize':
        sid, result = connect()
    else:
        sid = json.loads(SESSION.read_text())['session'] if SESSION.exists() else connect()[0]
        params = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
        if method == 'call':
            method = 'tools/call'
        result, sid = request(method, params, sid)
    print(json.dumps(result, ensure_ascii=True, indent=2))
