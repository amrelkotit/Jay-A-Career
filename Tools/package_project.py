"""Additive project packaging; generated caches and portable tools are excluded."""
from pathlib import Path
import zipfile,json,hashlib,sys,os
root=Path(__file__).resolve().parents[1]
destination=root/'Deliverables/Jay A Career - Realism Unity Project.zip'
def include(path):
 rel=path.relative_to(root);parts=rel.parts
 if rel.as_posix()=='Deliverables/Evidence/project-package-check.json':return False
 if not path.is_file() or path.suffix.lower() in ['.blend1','.blend2','.lnk','.log','.zip','.pyc','.exe']:return False
 if parts[0]=='Assets' and len(parts)>1 and parts[1] in ['Deliverables','Deliverables.meta']:return False
 if parts[0] in ['Assets','Packages','ProjectSettings','ArtSource']:return True
 if parts[0]=='Tools':
  if len(parts)>1 and parts[1]=='Blender':return False
  if len(parts)>1 and parts[1]=='Downloads':return len(parts)>2 and parts[2] in ['PolyHaven','Audio']
  if len(parts)>1 and parts[1]=='MPFB':return len(parts)>2 and parts[2] in ['Assets','mpfb2-2.0.18'] and '__pycache__' not in parts
  return '__pycache__' not in parts and path.name!='build-helper-command.txt'
 if parts[0]=='Deliverables':return len(parts)==2 and path.suffix.lower() in ['.docx','.pdf','.png','.json','.md'] or len(parts)==3 and parts[1]=='Evidence' and path.suffix.lower() in ['.json','.png'] or len(parts)==4 and parts[1:3]==('Evidence','LabelsLighting') and path.suffix.lower()=='.png'
 return len(parts)==1 and path.suffix.lower() in ['.md','.txt']
files=[]
for current,dirs,names in os.walk(root):
 rel=Path(current).relative_to(root)
 if not rel.parts:dirs[:]=[d for d in dirs if d in ['Assets','Packages','ProjectSettings','ArtSource','Tools','Deliverables']]
 elif rel.as_posix()=='Tools':dirs[:]=[d for d in dirs if d in ['Downloads','MPFB']]
 elif rel.as_posix()=='Tools/Downloads':dirs[:]=[d for d in dirs if d in ['PolyHaven','Audio']]
 elif rel.as_posix()=='Tools/MPFB':dirs[:]=[d for d in dirs if d in ['Assets','mpfb2-2.0.18']]
 elif rel.as_posix()=='Deliverables':dirs[:]=[d for d in dirs if d=='Evidence']
 dirs[:]=[d for d in dirs if d not in ['.git','__pycache__']]
 for name in names:
  p=Path(current)/name
  if include(p):files.append(p)
files.sort()
print('Selected',len(files),'files',flush=True)
manifest=[dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files]
destination.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
 for p in files:archive.write(p,p.relative_to(root).as_posix())
 archive.writestr('PACKAGE_MANIFEST.json',json.dumps(manifest,indent=2))
with zipfile.ZipFile(destination) as archive:
 assert archive.testzip() is None
 for entry in manifest:
  assert hashlib.sha256(archive.read(entry['path'])).hexdigest()==entry['sha256'],entry['path']
(root/'Deliverables/Evidence/project-package-check.json').write_text(json.dumps(dict(path=str(destination),files=len(files),bytes=destination.stat().st_size,sha256=hashlib.sha256(destination.read_bytes()).hexdigest(),crc_and_hashes_verified=True),indent=2))
print(destination,'files',len(files),'bytes',destination.stat().st_size)
if '--extract-review' in sys.argv:
 review=Path(sys.argv[sys.argv.index('--review-path')+1]) if '--review-path' in sys.argv else root.parents[3]/'JayCareerReview20261013'
 # Only create a new explicitly scoped review directory. No existing folder is replaced.
 if review.exists():raise RuntimeError('Review directory already exists: '+str(review))
 review.mkdir()
 with zipfile.ZipFile(destination) as archive:archive.extractall(review)
 print('Review copy:',review)

