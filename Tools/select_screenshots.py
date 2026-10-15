from pathlib import Path
from PIL import Image
import json
r=Path(__file__).resolve().parents[1];report=json.loads((r/'Evidence/Demo/route-report.json').read_text(encoding='utf8'));folder=Path(report['directory']);out=r/'Deliverables/Evidence'
selection={'lab-final.png':665,'exterior-final.png':630,'customer-final.png':280,'bedroom-final.png':560,'dialogue-final.png':700,'lights-final.png':435}
for name,index in selection.items():Image.open(folder/f'frame-{index:05d}.jpg').save(out/name)
(out/'screenshot-provenance.json').write_text(json.dumps(dict(environment=report['environment'],rendered_resolution=[1920,1080],route_completed=report['completed'],source_directory=str(folder),frames=selection),indent=2),encoding='utf8')
print('Saved',len(selection),'unaltered scene screenshots')
