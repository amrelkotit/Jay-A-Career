from pathlib import Path
import hashlib,json
r=Path(__file__).resolve().parents[1]
b=Path(r'C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
differences=[];counts={}
for group in ['Assets','Packages','ProjectSettings']:
 count=0
 for p in (r/group).rglob('*'):
  if not p.is_file():continue
  rel=p.relative_to(r)
  # Editor-only helper metadata has no runtime references; Unity generated it separately.
  if rel.as_posix()=='Assets/JayACareer/Editor/ResponseFileFullBuild.cs.meta':continue
  # Performance package generates/removes these empty test-run metadata files at build time.
  # They are absent from the successful build copy and not used by game scripts/scenes.
  if rel.as_posix() in ['Assets/Resources/PerformanceTestRunInfo.json','Assets/Resources/PerformanceTestRunInfo.json.meta','Assets/Resources/PerformanceTestRunSettings.json','Assets/Resources/PerformanceTestRunSettings.json.meta']:continue
  if group=='Assets' and (rel.parts[1]=='Deliverables' or rel.name=='Deliverables.meta'):continue
  count+=1
  if not (b/rel).exists() or sha(p)!=sha(b/rel):differences.append(rel.as_posix())
 counts[group]=count
report={'built_project':str(b),'working_project':str(r),'compared_files':counts,'differences':differences,'excluded':'Editor-only ResponseFileFullBuild.cs.meta; Assets/Deliverables evidence output; four generated performance-test metadata files (empty run info/settings and .meta), absent after build and not used by gameplay','runtime_and_visual_sources_match':not any(x.startswith('Assets/') for x in differences),'packages_and_settings_match':not any(x.startswith(('Packages/','ProjectSettings/')) for x in differences)}
(r/'Deliverables/Evidence/fresh-source-comparison.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
assert not differences,'Source differences require review'
