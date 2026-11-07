"""Generate a portable Japanese audition sheet using the actual shipped WAV files."""
from pathlib import Path
from html import escape
import json

GROUPS = {
    'メニュー・参加': {'UiNavigate':'項目を選ぶ','UiConfirm':'決定','UiBack':'戻る','UiInvalid':'選べない操作','UiOpen':'画面を開く','PlayerJoin':'機器で参加','PlayerReady':'準備完了','PlayerLeave':'参加を取り消す','Disconnect':'接続が切れた'},
    '盤・対局': {'BoardCursor':'マスを移動','BoardSelect':'駒を選ぶ','BoardCancel':'駒の選択を解除','BoardMove':'駒を指す','BoardDrop':'持ち駒を打つ','Promote':'成る','Countdown':'戦闘前の秒読み','DuelStart':'戦闘開始','DuelWin':'戦闘の決着','DuelDraw':'引き分け','MatchWin':'対局の決着'},
    '武器・構え': {'SpearSwing':'歩｜槍で突く','ShortGun':'香｜短銃を撃つ','SwordSwing':'桂｜刀を振る','NaginataSwing':'銀・金｜薙刀を振る','LongGun':'角｜長銃を撃つ','BowRelease':'飛｜弓を放つ','BowNock':'飛｜次の矢をつがえる','KnifeSwing':'王｜短刀を振る','GuardRaise':'防御する','GuardEnd':'構えを解く','AimIn':'照準を合わせる','AimOut':'照準を戻す'},
    'スキル': {'Leap':'歩｜跳び込む','Throw':'香｜閃光弾を投げる','FlashBurst':'香｜閃光弾が破裂','Dash':'桂｜踏み込む','DashStrike':'桂｜踏み込み後の斬撃','GinShield':'銀｜防御強化','KinShield':'金｜防御強化','BuffEnd':'防御強化が解ける','Ascend':'角｜上昇','FieldCast':'飛｜減速領域を出す','FieldEnter':'減速がかかる','FieldExit':'減速が解ける','Command':'王｜軍配で指示','TurretSpawn':'砲台を設置','TurretShot':'砲台が射撃','TurretEnd':'砲台が消える','SkillReady':'スキルが再使用可能'},
    '足音・着弾': {'Hit':'被弾','Block':'攻撃を防ぐ','Down':'倒される','ArrowImpact':'矢が物に刺さる','BulletImpact':'弾が物に当たる','FootstepSoft':'畳を歩く','FootstepWood':'木の上を歩く','Land':'着地','Climb':'よじ登る'},
}

def write(root,catalog):
    by_id={c['id']:c for c in catalog}
    assert set(by_id)==set().union(*[set(v) for v in GROUPS.values()])
    sections=[]
    for group,labels in GROUPS.items():
        rows=[]
        for cue,label in labels.items():
            c=by_id[cue];urls=['../Assets/App/Resources/'+p+'.wav' for p in c['clips']]
            options=''.join(f'<option value="{escape(url)}">音 {i+1}</option>' for i,url in enumerate(urls))
            variants=f'<select aria-label="{escape(label)}の音を選ぶ">{options}</select>' if len(urls)>1 else '<small>1種類</small>'
            rows.append(f'<article><span>{escape(label)}</span><audio controls preload="none" aria-label="{escape(label)}" src="{escape(urls[0])}" data-gain="{c["gain"]*(.52 if c["ui"] else .68):.4f}"></audio>{variants}</article>')
        sections.append(f'<section data-group="{group}"><h2>{group}</h2>'+''.join(rows)+'</section>')
    nav='<button aria-pressed="true" data-filter="">すべて</button>'+''.join(f'<button aria-pressed="false" data-filter="{g}">{g}</button>' for g in GROUPS)
    page='''<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>FPS将棋｜効果音</title>
<style>*{box-sizing:border-box}body{margin:0;background:#201f1b;color:#f3eee3;font:16px/1.5 system-ui,sans-serif}header{border-bottom:1px solid #5c5240;padding:23px max(24px,5vw) 18px}h1{font:500 29px Georgia,serif;margin:0}p{color:#c4beaf;margin:6px 0}main{display:flex;gap:42px;max-width:1120px;margin:26px auto;padding:0 22px}nav{width:166px;flex-shrink:0;position:sticky;top:12px;align-self:flex-start}button{display:block;background:none;border:0;border-left:3px solid transparent;text-align:left;color:#c9c3b7;cursor:pointer;font:inherit;padding:10px 12px;width:100%}button[aria-pressed=true]{border-color:#ddb978;color:#fff1d5;background:#302c24}h2{font-size:19px;color:#e3cc9f;margin:16px 0 5px}#sounds{flex:1}article{display:grid;grid-template-columns:minmax(180px,1fr) minmax(170px,300px) 76px;gap:14px;align-items:center;border-bottom:1px solid #443f34;padding:11px 0}audio{width:100%;height:31px}small,select,a{color:#c5baa5}select{font:inherit;background:#312e27;border:1px solid #655b49;padding:3px}footer{max-width:1120px;margin:12px auto 36px;padding:8px 22px}button:focus-visible,select:focus-visible{outline:2px solid #e4be78}@media(max-width:760px){main{display:block}nav{position:static;width:auto;display:flex;overflow:auto}button{white-space:nowrap;width:auto}article{grid-template-columns:1fr 75px}article span{grid-column:1/-1}}</style>
<header><h1>FPS将棋　効果音</h1><p>駒や操作ごとに試聴できます。複数ある音は右の欄で切り替えられます。</p></header><main><nav aria-label="分類">'''+nav+'''</nav><div id="sounds">'''+''.join(sections)+'''</div></main><footer><a href="../THIRD_PARTY_AUDIO.md">素材とライセンス</a></footer><script>
const all=s=>[...document.querySelectorAll(s)];all('audio').forEach(a=>{a.volume=Number(a.dataset.gain);a.onplay=()=>all('audio').forEach(b=>{if(a!==b)b.pause()})});all('select').forEach(s=>s.onchange=()=>{let a=s.parentElement.querySelector('audio');a.src=s.value;a.play()});all('button').forEach(b=>b.onclick=()=>{all('button').forEach(x=>x.setAttribute('aria-pressed',String(x===b)));all('section').forEach(s=>s.hidden=!!b.dataset.filter&&s.dataset.group!==b.dataset.filter);all('audio').forEach(a=>a.pause())});</script></html>'''
    (root/'AudioReview').mkdir(exist_ok=True)
    (root/'AudioReview/index.html').write_text(page,encoding='utf-8')

if __name__=='__main__':
    root=Path(__file__).resolve().parents[1]
    write(root,json.loads((root/'Assets/App/Resources/Audio/catalog.json').read_text(encoding='utf-8'))['cues'])
