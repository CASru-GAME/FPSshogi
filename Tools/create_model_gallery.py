"""Create a compact review gallery from Unity's isolated model captures (Pillow)."""
import html
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
# Optional repository-local dependency install used by the verification workstation.
sys.path.insert(0, str(root / '.utmp/motion-repair/python'))
from PIL import Image

source = root / 'VisualReview/Models-Final'
before = root / 'VisualReview/Models-Baseline'
out = source / 'Gallery'
out.mkdir(parents=True, exist_ok=True)
names = {'Bow':'弓','Gun_long':'長銃','Gun_short':'短銃','Gunbai':'軍配','Katana':'刀',
         'KingTurret':'王の砲台','knife':'短刀','Naginata_blue':'薙刀・青','Naginata_red':'薙刀・赤','Spear':'槍',
         'fuhyo':'歩','kyosya':'香車','keima':'桂馬','gin':'銀','kin':'金','kakugyo':'角','hisya':'飛車','ou':'王','gyoku':'玉','kakugyo_black':'黒い角（復元元）'}
angles = ['正面','側面','背面','斜め','上面','下面']

def copy_image(path, name):
    if not path.exists():
        return None
    with Image.open(path) as im:
        im.convert('RGB').save(out / name, quality=87, optimize=True)
    return name

sections = []
for prefix, title in [('display','武器'),('piece','駒'),('projectile','投射物'),('room','和室')]:
    cards = []
    for path in sorted(source.glob(prefix + '-*-3.png')):
        base = path.stem[:-2]
        label = names.get(base[len(prefix)+1:], base[len(prefix)+1:])
        files = []
        for i in range(6):
            files.append(copy_image(source / f'{base}-{i}.png', f'{base}-{i}.jpg'))
        buttons = ''.join(f'<button data-file="{html.escape(f)}">{angles[i]}</button>' for i,f in enumerate(files) if f)
        cards.append(f'<article><h3>{html.escape(label)}</h3><a href="{html.escape(files[3])}" target="_blank"><img loading="lazy" src="{html.escape(files[3])}" alt="{html.escape(label)}"></a><div class="angles">{buttons}</div></article>')
    sections.append(f'<section id="{prefix}"><h2>{title}</h2><div class="grid">'+''.join(cards)+'</div></section>')

comparisons = []
for name, angle, description in [('Bow',1,'弓と弦の平面・先端の接続'),('Bow',0,'二重になっていた弦の解消'),('Katana',0,'軽量化で崩れた刃の縁')]:
    stem = f'display-{name}-{angle}'
    old = copy_image(before / (stem+'.png'), stem+'-before.jpg')
    new = stem+'.jpg'
    comparisons.append(f'<article class="comparison"><h3>{description}</h3><div class="pair"><figure><figcaption>修正前</figcaption><img src="{old}"></figure><figure><figcaption>修正後</figcaption><img src="{new}"></figure></div></article>')

runtime = []
for name in ['Hisya-00-idle','Hisya-01-windup','Hisya-04-recover','Hisya-05-return','Hisya-07-secondary','Keima-00-idle','Kyosya-01-windup','King-08-skill-start']:
    path=root / 'VisualReview/Motion-Models-Verified' / (name+'.png')
    file=copy_image(path,'runtime-'+name+'.jpg')
    if file:runtime.append(f'<figure><figcaption>{html.escape(name)}</figcaption><a href="{file}" target="_blank"><img loading="lazy" src="{file}"></a></figure>')
for path in sorted((root/'VisualReview/Models-Verified/Native-Visible').glob('*.png')):
    if path.stem in ('title','board') or path.stem.startswith('weapon-') or path.stem.startswith('bow-'):
        file=copy_image(path,'native-'+path.stem+'.jpg')
        runtime.append(f'<figure><figcaption>Windows版 / {html.escape(path.stem)}</figcaption><a href="{file}" target="_blank"><img loading="lazy" src="{file}"></a></figure>')

document='''<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>FPS将棋 モデル確認</title>
<style>*{box-sizing:border-box}body{margin:0;background:#202529;color:#eee;font:15px/1.65 system-ui,sans-serif}nav{position:fixed;width:160px;padding:24px 18px;height:100vh;background:#15191c}nav a{display:block;color:#d4bd8d;padding:9px 0;text-decoration:none}main{margin-left:160px;max-width:1500px;padding:24px 32px}h1{font-size:25px}h2{font-size:21px;border-bottom:1px solid #596065;padding:8px 0}h3{font-size:16px;font-weight:500;margin:8px 0}section{scroll-margin:20px;margin:45px 0}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(290px,1fr));gap:26px}img{display:block;width:100%;height:auto;background:#272d31}article{min-width:0}.angles{display:flex;gap:5px;flex-wrap:wrap;margin-top:6px}button{background:#343c42;color:#eee;border:1px solid #646b70;padding:5px 9px;cursor:pointer}button:hover,button.active{border-color:#d4bd8d;color:#e7ca91}figure{margin:0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}.comparison{margin:25px 0}.comparison img{max-height:540px;object-fit:contain}figcaption{color:#c7cace;font-size:13px}p{max-width:850px;color:#c7cace}.runtime{display:grid;grid-template-columns:1fr 1fr;gap:20px}@media(max-width:700px){nav{position:static;width:auto;height:auto;display:flex;gap:15px;flex-wrap:wrap}main{margin:0;padding:18px}.runtime{grid-template-columns:1fr}}</style>
<nav><strong>モデル確認</strong><a href="#changes">修正前後</a><a href="#display">武器</a><a href="#piece">駒</a><a href="#projectile">投射物</a><a href="#room">和室</a><a href="#runtime">ゲーム内</a></nav><main><h1>FPS将棋 モデル確認</h1><p>Unityで実際のモデルを撮影。各モデルのボタンで視点を切り替えられます。画像をクリックすると拡大表示します。</p><section id="changes"><h2>修正前後</h2>'''+''.join(comparisons)+'</section>'+''.join(sections)+'<section id="runtime"><h2>ゲーム内</h2><div class="runtime">'+''.join(runtime)+'''</div></section></main>
<script>document.querySelectorAll('.angles button').forEach(b=>b.addEventListener('click',()=>{const a=b.closest('article');a.querySelector('img').src=b.dataset.file;a.querySelector('a').href=b.dataset.file;a.querySelectorAll('button').forEach(x=>x.classList.toggle('active',x===b));}));</script></html>'''
(out/'index.html').write_text(document,encoding='utf-8')
print(out/'index.html')
print(f'{len(list(out.glob("*.jpg")))} images, {sum(f.stat().st_size for f in out.iterdir())/1024/1024:.2f} MiB')
