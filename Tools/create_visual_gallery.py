from pathlib import Path
import html
root=Path(__file__).resolve().parents[1]
out=root/'VisualReview'
cases=[('背景・槍の構え','Fuhyo-idle'),('短銃・木製銃床','Kyosya-idle'),('刀','Keima-idle'),('赤い薙刀','Gin-idle'),('青い薙刀','Kin-idle'),('長銃','Kakugyo-idle'),('弓と矢','Hisya-idle'),('短刀','King-idle'),('王の軍配と砲台','King-skill'),('飛車の半透明フィールド','Hisya-skill'),('防御の表示','Fuhyo-secondary'),('銀の強化','Gin-skill'),('金の強化','Kin-skill'),('盤面','board'),('全種類・成り駒','board-all-promotions')]
cards=[]
for title,name in cases:
    folder='ArtFinal1080' if name.startswith(('Gin-','Kin-','board')) or name=='King-skill' else 'Fixed'
    image=out/folder/f'{name}.png'
    if image.exists():cards.append(f'<article><h2>{html.escape(title)}</h2><a href="{folder}/{name}.png"><img loading="lazy" src="{folder}/{name}.png" alt="{html.escape(title)}"></a></article>')
results=(out/'Fixed/results.txt').read_text(encoding='utf-8') if (out/'Fixed/results.txt').exists() else '最終検証中'
geometry=(out/'geometry-verification.txt').read_text(encoding='utf-8')
doc='''<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>FPSshogi 修正後の見た目</title><style>body{margin:0;background:#16191b;color:#eee;font:16px/1.6 system-ui,sans-serif}main{max-width:1300px;margin:auto;padding:28px}h1{color:#e6bd74}h2{font-size:20px}article{margin:36px 0}img{width:100%;border:1px solid #585048;border-radius:8px}a{color:#e6bd74}pre{white-space:pre-wrap;background:#24282b;padding:20px;border-radius:8px}summary{cursor:pointer}</style><main><h1>FPSshogi — 修正後の見た目</h1><p>V01〜V11の修正をUnityの実際の対局・FPS画面で確認しています。画像を押すと原寸で開きます。</p><p><a href="../VISUAL-FIXES.txt">修正内容</a> · <a href="../VISUAL-REVIEW.txt">修正前のレビュー</a> · <a href="Fixed/results.txt">検証結果</a></p>'''
doc+='<details><summary>形状データ量の比較</summary><pre>'+html.escape(geometry)+'</pre></details>'
doc+='<details><summary>1080p プレイ検証ログ（仮想ゲームパッド2台）</summary><pre>'+html.escape(results)+'</pre></details>'
doc+='<p>追加確認: <a href="Fixed720/results.txt">720pの全武器検証</a> · <a href="ArtFinal1080/results.txt">補修後のモデル</a> · <a href="UI-After720/regression-results.txt">UIと対局操作</a> · <a href="PlayerSmoke/results.txt">Windows実行ファイル</a> · <a href="mesh-validation.txt">メッシュ検査</a></p>'
doc+=''.join(cards)
for level in (0,2):
    name=f'room-overview-LOD{level}'
    doc+=f'<article><h2>背景の形状確認：LOD {level}</h2><a href="ArtFinal1080/{name}.png"><img loading="lazy" src="ArtFinal1080/{name}.png" alt="軽量化後の家具と座布団"></a></article>'
for title,name in [('Windows版：減速フィールド','slow-field'),('Windows版：発砲','gun-shot'),('Windows版：銀の強化','silver-buff'),('Windows版：軍配','king-fan')]:
    if (out/'PlayerSmoke'/f'{name}.png').exists():doc+=f'<article><h2>{title}</h2><a href="PlayerSmoke/{name}.png"><img loading="lazy" src="PlayerSmoke/{name}.png" alt="{title}"></a></article>'
doc+='<p><a href="verification-summary.txt">最終検証のまとめ</a> · <a href="final-build-results.txt">通常版のビルド結果</a> · <a href="ReleaseLaunch/title.png">通常版の起動画面</a> · <a href="ReleaseLaunch/help.png">遊び方画面</a></p>'
doc+='<p>このPCのWindows / GeForce RTX 4060で検証しました。Unity EditorはDirect3D 11、Windows実行ファイルはDirect3D 12です。実物のゲームパッド、別GPU、FPSの比較測定は対象外です。</p></main></html>'
(out/'fixed.html').write_text(doc,encoding='utf-8')
print(f'Gallery: {len(cards)} images')
