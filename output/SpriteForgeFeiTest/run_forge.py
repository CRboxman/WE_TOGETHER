"""Run the installed Forge tools on frames rendered from a copied Fei PSB."""
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SKILLS = Path('C:/Users/15245/.codex/skills')

def run(skill, tool, *args):
    subprocess.run([sys.executable, '-X', 'utf8', str(SKILLS / skill / 'scripts' / (tool + '.py')),
                    *map(str, args)], check=True)

run('video2dsprite', 'finish_frames', 'hd', '--frames', ROOT / 'raw', '--output-dir', ROOT / 'finished',
    '--target-height', 256, '--role', 'hero', '--character', 'Fei')
finish = json.loads((ROOT / 'finished/finish.json').read_text(encoding='utf-8'))
files = sorted((ROOT / 'finished/frames').glob('*.png'))
manifest = {
    'schema': 'generate2dsprite.animation_clips.v2',
    'frames': [p.relative_to(ROOT).as_posix() for p in files],
    'anchor_px': [round(v) for v in finish['anchor']],
    'clips': {'walk': {'frames': list(range(len(files))), 'ticks': 3, 'loop_policy': 'cycle',
                       'entry_frame': 0, 'role': 'player'}},
    'states': {'moving': 'walk'}, 'sampling': 'linear', 'pixel_art': False,
    'art_source': 'existing', 'placeholder': False,
}
(ROOT / 'clips.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
run('generate2dsprite', 'build_animation_clips', '--manifest', ROOT / 'clips.json', '--output-dir', ROOT / 'clips')
run('generate2dsprite', 'export_engine', '--clips', ROOT / 'clips/animation-clips.json',
    '--target', 'aseprite-json', '--output-dir', ROOT / 'engine', '--name', 'fei-walk', '--ppu', 100)
run('video2dsprite', 'engine_export', 'package', '--clean-dir', ROOT / 'finished/frames',
    '--output-dir', ROOT / 'package', '--name', 'fei-walk', '--fps', 20, '--loop', '--key', 'none',
    '--source-size', ','.join(map(str, finish['size'])),
    '--source-anchor', ','.join(map(str, manifest['anchor_px'])),
    '--formats', 'png,webm,packed', '--art-source', 'existing', '--cadence-ms', 1200)
run('video2dsprite', 'engine_export', 'verify', '--package', ROOT / 'package')
run('video2dsprite', 'validate_animation', ROOT / 'package', '--require-states', 'fei-walk', '--require-verify')
