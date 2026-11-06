"""Build contact sheets from Unity's captured frames, and optional 30fps videos."""
from pathlib import Path
import sys
import subprocess
import html

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.utmp/motion-repair/python'))
from PIL import Image, ImageDraw, ImageFont

before = ROOT / 'VisualReview/Motion-Red-Stable'
after = ROOT / sys.argv[1] if len(sys.argv) > 1 else ROOT / 'VisualReview/Motion-Verified'
video_source = ROOT / sys.argv[2] if len(sys.argv) > 2 else after
video_override = ROOT / sys.argv[3] if len(sys.argv) > 3 else None
out = after / 'Gallery'
out.mkdir(exist_ok=True, parents=True)
names = dict(Fuhyo='歩', Kyosya='香車', Keima='桂馬', Gin='銀', Kin='金', Kakugyo='角', Hisya='飛車', King='王')
font = ImageFont.truetype('C:/Windows/Fonts/meiryo.ttc', 20)
phases = ['00-idle', '01-windup', '03-strike', '04-follow-through', '06-idle']
entries = []
for key, name in names.items():
    sheet = Image.new('RGB', (1280, 640), '#1b211e')
    draw = ImageDraw.Draw(sheet)
    for row, (folder, label) in enumerate([(before, '修正前'), (after, '修正後')]):
        draw.text((12, row * 320 + 4), f'{name}　{label}　構え / 振り出し / 攻撃 / 振り抜き / 復帰', fill='white', font=font)
        for col, phase in enumerate(phases):
            path = folder / f'{key}-{phase}.png'
            if not path.exists():
                continue
            with Image.open(path) as frame:
                # Inspect 1P; the original full split-screen screenshots are retained.
                tile = frame.crop((0, 0, frame.width // 2, frame.height)).resize((256, 288))
                sheet.paste(tile, (col * 256, row * 320 + 32))
    sheet.save(out / f'{key}-comparison.jpg', quality=92)
    entries.append(f'<section><h2>{name}</h2><img src="{key}-comparison.jpg" alt="{name}のモーション比較">')
    frame_root = video_source / 'VideoFrames'
    sequence = Image.new('RGB', (1280, 960), '#1b211e')
    sequence_draw = ImageDraw.Draw(sequence)
    has_sequence = False
    for row, action in enumerate(['attack', 'secondary', 'skill']):
        frames = frame_root / f'{key}-{action}'
        last_frame = '0089.jpg' if action == 'skill' else '0059.jpg'
        if video_override is not None:
            replacement = video_override / 'VideoFrames' / f'{key}-{action}'
            if (replacement / last_frame).exists():
                frames = replacement
        if not (frames / last_frame).exists():
            continue
        import imageio_ffmpeg
        movie = out / f'{key}-{action}.mp4'
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-loglevel', 'error', '-framerate', '30', '-i', str(frames / '%04d.jpg'), '-vf', 'scale=1280:720', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '21', '-movflags', '+faststart', str(movie)], check=True)
        entries.append(f'<video controls loop preload="metadata" src="{movie.name}"></video>')
        label = dict(attack='攻撃', secondary='副操作', skill='スキル')[action]
        sequence_draw.text((12, row * 320 + 4), f'{name}　{label}　連続フレーム', fill='white', font=font)
        samples = [9, 12, 16, 20, 30] if action == 'attack' else [9, 13, 18, 27, 50]
        for col, index in enumerate(samples):
            with Image.open(frames / f'{index:04d}.jpg') as frame:
                tile = frame.crop((0, 0, frame.width // 2, frame.height)).resize((256, 288))
                sequence.paste(tile, (col * 256, row * 320 + 32))
        has_sequence = True
    if has_sequence:
        sequence.save(out / f'{key}-sequence.jpg', quality=92)
        entries.append(f'<details><summary>連続画像</summary><img src="{key}-sequence.jpg" alt="{name}の連続フレーム"></details>')
    entries.append('</section>')
(out / 'index.html').write_text('<!doctype html><html lang="ja"><meta charset="utf-8"><title>FPS将棋 モーション確認</title><style>body{background:#151b18;color:#eceee6;font:16px sans-serif;max-width:1280px;margin:32px auto}img{width:100%}video{width:32%;margin-right:1%}section{margin:40px 0}a{color:#bed7a3}</style><h1>FPS将棋 モーション確認</h1><p>各駒の通常攻撃を同じ時刻で比較。上段が修正前、下段が修正後です。動画は左から攻撃・副操作・スキル。Unityで仮想コントローラーの入力を送り、1/30秒ごとに記録しています。</p>'+''.join(entries), encoding='utf-8')
print(out / 'index.html')
