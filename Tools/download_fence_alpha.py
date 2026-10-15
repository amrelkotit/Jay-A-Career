from pathlib import Path
import json,urllib.request,hashlib
from PIL import Image
r=Path.cwd();info=json.loads((r/'Tools/Downloads/PolyHaven/modular_chainlink_fence/source.json').read_text());f=info['files']['wire_opacity']['1k']['png']
path=r/'Assets/JayACareer/Textures/Realism/modular_chainlink_fence_wire_opacity_1k.png'
if not path.exists():
 req=urllib.request.Request(f['url'],headers={'User-Agent':'JayACareerStudentProject/1.0'});path.write_bytes(urllib.request.urlopen(req,timeout=90).read())
assert hashlib.md5(path.read_bytes()).hexdigest()==f['md5']
diff=Image.open(r/'Assets/JayACareer/Textures/Realism/modular_chainlink_fence_wire_diff_1k.jpg').convert('RGBA');diff.putalpha(Image.open(path).convert('L'));diff.save(r/'Assets/JayACareer/Textures/Realism/modular_chainlink_fence_wire_diff_alpha_1k.png')
print('Verified and packed fence opacity:',path.stat().st_size)
