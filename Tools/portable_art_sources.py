import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
for path in sorted((root/'ArtSource').glob('*.blend')):
 bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False)
 for material in bpy.data.materials:
  if material.name.startswith('modular_chainlink_fence_wire') and material.use_nodes:
   shader=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   if shader:
    node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(root/'Assets/JayACareer/Textures/Realism/modular_chainlink_fence_wire_opacity_1k.png'),check_existing=True)
    material.node_tree.links.new(node.outputs['Color'],shader.inputs['Alpha']);material.surface_render_method='DITHERED'
 bpy.data.orphans_purge(do_recursive=True)
 bpy.ops.file.make_paths_relative()
 bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
 print('PORTABLE SOURCE',path.name,path.stat().st_size,flush=True)
