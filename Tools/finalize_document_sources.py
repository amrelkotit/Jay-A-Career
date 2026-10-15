from pathlib import Path
import json
r=Path(__file__).resolve().parents[1]
p=r/'Tools/credits.py';s=p.read_text(encoding='utf8')
s=s.replace('Original Blender modeling and synthesized WAV audio','Original Blender modeling, synthesized WAV audio, Unity-authored tamper strip, irregular residue, utility crossarm and exterior backdrop')
s=s.replace('1K textures; FBX conversion, scale/pivot adjustment and mesh simplification where required','1K textures; FBX conversion, scale/pivot adjustment and mesh simplification where required; fence wire opacity packed into diffuse alpha and alpha-tested in URP')
p.write_text(s,encoding='utf8')
p=r/'Tools/create_gdd.py';s=p.read_text(encoding='utf8')
checks=json.loads((r/'Deliverables/Evidence/input-physics-checks.json').read_text(encoding='utf8'))
if checks['passed']:
    anchor="See scene-checks.json."
    text=f" Input and physics checks passed {checks['checks']} assertions using synthetic keyboard events and headless world-input invocation. They cover contextual keys, carrying/placement, recovery, door obstruction, arrangement and lighting; see input-physics-checks.json. Mouse capture and subjective camera feel require interactive review."
    if text not in s:s=s.replace(anchor,anchor+text)
p.write_text(s,encoding='utf8')
print('Document sources updated')
