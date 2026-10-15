from pathlib import Path
import hashlib,json,re
root=Path(__file__).resolve().parents[1]
prior=Path('E:/El Gam3a/JayCareerReview20261013')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
differences=[];legacy_alias_changes=[];checked=0
def urp_render_properties(p):
 text=p.read_text(encoding='utf8')
 # URP Lit renders _BaseMap/_BaseColor. Unity synchronizes these unused
 # legacy Standard aliases during build preprocessing.
 text=re.sub(r'    - _MainTex:\n(?:        .*\n)+','',text)
 return re.sub(r'    - _Color:.*\n','',text)
for folder in ['Assets/JayACareer/Scenes','Assets/JayACareer/Models','Assets/JayACareer/Materials','Assets/JayACareer/Prefabs','Assets/JayACareer/Audio','Assets/JayACareer/Textures']:
 for p in (root/folder).rglob('*'):
  if not p.is_file() or p.suffix=='.meta':continue
  other=prior/p.relative_to(root)
  checked+=1
  if not other.exists() or sha(p)!=sha(other):
   if other.exists() and p.suffix=='.mat' and urp_render_properties(p)==urp_render_properties(other):legacy_alias_changes.append(p.relative_to(root).as_posix())
   else:differences.append(p.relative_to(root).as_posix())
final=root/'Deliverables/Windows Final/Jay A Career_Data/Managed/Assembly-CSharp.dll'
compiled=root/'Library/Bee/PlayerScriptAssemblies/Assembly-CSharp.dll'
report={'method':'Mono assembly update on the previously built scene data; not a successful fresh BuildPipeline run','asset_files_compared':checked,'asset_differences_from_prior_build_source':differences,'legacy_urp_alias_only_changes':legacy_alias_changes,'scene_and_visual_assets_match':not differences,'deployed_assembly_matches_latest_Unity_compilation':sha(final)==sha(compiled),'compiled_assembly_sha256':sha(compiled),'runtime_sources':[{ 'path':p.relative_to(root).as_posix(),'sha256':sha(p)} for p in sorted((root/'Assets/JayACareer/Scripts').glob('*.cs'))]}
(root/'Deliverables/Evidence/windows-final-provenance.json').write_text(json.dumps(report,indent=2),encoding='utf8')
assert report['scene_and_visual_assets_match'],differences
assert report['deployed_assembly_matches_latest_Unity_compilation']
print(f'{checked} scene/visual asset files match prior build source; deployed DLL matches latest Unity compilation')
