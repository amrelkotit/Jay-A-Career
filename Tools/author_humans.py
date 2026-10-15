"""Create three CC0 MPFB humans with original pose clips and a tailored Hale coat."""
import bpy,sys,math,json,shutil,bmesh
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools/MPFB/mpfb2-2.0.18/src'))
import mpfb
original=bpy.utils.extension_path_user
bpy.utils.extension_path_user=lambda package,**kwargs:str(ROOT/'Tools/MPFB/UserData') if package=='mpfb' else original(package,**kwargs)
mpfb.get_preference=lambda name:str(ROOT/'Tools/MPFB/UserData') if name=='mpfb_user_data' else False if name=='mh_auto_user_data' else None
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
ASSETS=ROOT/'Tools/MPFB/Assets'
OUT=ROOT/'Assets/JayACareer/Models/Realism';OUT.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'Assets/JayACareer/Textures/Characters';TEX.mkdir(parents=True,exist_ok=True)
def simple_material(name,color=(1,1,1,1),texture=None,rough=.65):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=rough
    if texture:
        n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(texture),check_existing=True)
        mat.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
    return mat
def asset(human,path,kind):
    p=ASSETS/path
    if not p.exists():raise FileNotFoundError(p)
    return HumanService.add_mhclo_asset(str(p),human,asset_type=kind,subdiv_levels=0,material_type='MAKESKIN')
def coat(human,rig,clothing):
    white=simple_material('HaleLabCoat',(.83,.85,.8,1));pants=simple_material('HaleTrousers',(.07,.09,.1,1))
    clothing.data.materials.clear();clothing.data.materials.append(white);clothing.data.materials.append(pants)
    for poly in clothing.data.polygons:
        z=sum(clothing.data.vertices[i].co.z for i in poly.vertices)/len(poly.vertices)
        poly.material_index=0 if z>1.0 else 1
    # Use the fitted, fully weighted long-sleeve jacket rather than a rigid overlaid shell.
    # Its existing collar, seams and front closure remain part of the shared humanoid rig.
    for poly in clothing.data.polygons:
        poly.material_index=0 if sum(clothing.data.vertices[i].co.z for i in poly.vertices)/len(poly.vertices)>.88 else 1
    # Closed knee-length hem, bound to the hips of stationary Dr. Hale.
    # The fitted long sleeves and collar come from the CC0 garment above.
    vertices=[];faces=[];segments=40
    for z,rx,ry in [(.68,.255,.18),(.86,.25,.175),(1.025,.235,.16)]:
        for i in range(segments):
            angle=math.tau*i/segments;vertices.append((rx*math.cos(angle),ry*math.sin(angle)-.025,z))
    for j in range(2):
        for i in range(segments):
            n=(i+1)%segments;faces.append((j*segments+i,j*segments+n,(j+1)*segments+n,(j+1)*segments+i))
    mesh=bpy.data.meshes.new('CoatHem');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('HaleCoatHem',mesh);bpy.context.collection.objects.link(obj);mesh.materials.append(white);obj.parent=rig
    vg=obj.vertex_groups.new(name='mixamorig:Hips');vg.add(list(range(len(vertices))),1,'REPLACE')
    arm=obj.modifiers.new('Humanoid binding','ARMATURE');arm.object=rig
    solid=obj.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=.004
    for face in mesh.polygons:face.use_smooth=True
def clips(rig):
    bones=rig.pose.bones
    base={}
    for side in ['Left','Right']:
        p=bones['mixamorig:'+side+'Arm'];v=(p.bone.tail_local-p.bone.head_local).normalized()
        target=Vector((.08 if side=='Left' else -.08,0,-1)).normalized()
        delta=v.rotation_difference(target);rest=p.bone.matrix_local.to_quaternion()
        base[p.name]=rest.inverted()@delta@rest
    for name,length in [('Idle',60),('Walk',30),('Talk',60),('Handoff',50)]:
        action=bpy.data.actions.new(name);rig.animation_data_create();rig.animation_data.action=action
        for frame in range(1,length+1):
            phase=(frame-1)/(length-1)*math.tau
            for p in bones:p.rotation_mode='QUATERNION';p.rotation_quaternion=Quaternion((1,0,0,0))
            for key,value in base.items():bones[key].rotation_quaternion=value
            bones['mixamorig:Spine1'].rotation_quaternion=Quaternion((1,0,0),.012*math.sin(phase))
            if name=='Walk':
                for side,sign in [('Left',1),('Right',-1)]:
                    bones['mixamorig:'+side+'UpLeg'].rotation_quaternion=Quaternion((1,0,0),sign*.32*math.sin(phase))
                    bones['mixamorig:'+side+'Leg'].rotation_quaternion=Quaternion((1,0,0),max(0,-sign*math.sin(phase))*.45)
                    key='mixamorig:'+side+'Arm';bones[key].rotation_quaternion=base[key]@Quaternion((1,0,0),-sign*.22*math.sin(phase))
            elif name=='Talk':
                bones['mixamorig:Head'].rotation_quaternion=Quaternion((0,0,1),.06*math.sin(phase))
                bones['mixamorig:RightForeArm'].rotation_quaternion=Quaternion((1,0,0),-.35-.18*math.sin(phase))
            elif name=='Handoff':
                reach=math.sin((frame-1)/(length-1)*math.pi)
                key='mixamorig:RightArm';bones[key].rotation_quaternion=base[key]@Quaternion((1,0,0),-.8*reach)
                bones['mixamorig:RightForeArm'].rotation_quaternion=Quaternion((1,0,0),-.35*reach)
            for p in bones:p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name)
        action.use_fake_user=True
    rig.animation_data.action=bpy.data.actions.get('Idle');bpy.context.scene.frame_set(1)
def export_materials(name):
    rows=[]
    for mat in {m for o in bpy.context.scene.objects if o.type=='MESH' for m in o.data.materials if m}:
        p=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
        row={'name':mat.name,'color':list(p.inputs['Base Color'].default_value) if p else [1,1,1,1],'roughness':.65,'metallic':0,'textures':{}}
        if mat.use_nodes:
            nodes=[n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
            for node in nodes:
                im=node.image;file=Path(bpy.path.abspath(im.filepath));lower=file.name.lower()
                if not file.exists() or any(k in lower for k in ['_ao','specular','bump','roughness','displacement']):continue
                limit=2048 if 'skin' in mat.name.lower() else 1024
                if max(im.size)>limit:im.scale(max(1,int(im.size[0]*limit/max(im.size))),max(1,int(im.size[1]*limit/max(im.size))))
                dest=TEX/(file.stem+'.png');im.filepath_raw=str(dest);im.file_format='PNG';im.save()
                key='Normal' if 'normal' in lower else 'Alpha' if 'alpha' in lower else 'Base Color'
                row['textures'][key]=dest.name
        rows.append(row)
    (OUT/(name+'.materials.json')).write_text(json.dumps(rows,indent=2))
for name,age,race,skin,hair,suit in [
 ('Jay',.27,{'asian':.65,'caucasian':.25,'african':.1},'young_asian_male','short01','male_worksuit01'),
 ('DrHale',.73,{'asian':.1,'caucasian':.8,'african':.1},'old_caucasian_male','short02','male_casualsuit01'),
 ('Customer',.42,{'asian':.1,'caucasian':.1,'african':.8},'middleage_african_male','short04','male_casualsuit01')]:
    if '--hale-only' in sys.argv and name!='DrHale':continue
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    macro=TargetService.get_default_macro_info_dict();macro.update(gender=1,age=age,race=race,weight=.48 if name!='Customer' else .6)
    human=HumanService.create_human(macro_detail_dict=macro);human.name=name+'Skin'
    rig=HumanService.add_builtin_rig(human,'mixamo_unity');rig.name='HumanoidRig'
    skinpath=next((ASSETS/'skins'/skin).glob('*diffuse*.png'))
    human.data.materials.clear();human.data.materials.append(simple_material(name+'Skin',texture=skinpath))
    clothing=asset(human,'clothes/'+suit+'/'+suit+'.mhclo','Clothes');clothing.name=name+'Clothing'
    asset(human,'clothes/shoes01/shoes01.mhclo','Clothes')
    hair_obj=asset(human,'hair/'+hair+'/'+hair+'.mhclo','Hair')
    brows=asset(human,'eyebrows/eyebrow001/eyebrow001.mhclo','Eyebrows')
    eyes=asset(human,'eyes/low-poly/low-poly.mhclo','Eyes');eyes.data.materials.clear();eyes.data.materials.append(simple_material(name+'Eyes',texture=ASSETS/'eyes/materials/brown_eye.png',rough=.18))
    asset(human,'teeth/teeth_base/teeth_base.mhclo','Teeth')
    if name=='DrHale':
        coat(human,rig,clothing)
        for m in hair_obj.data.materials:
            if m and m.use_nodes:
                m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.65,.65,.62,1)
    for obj in list(bpy.context.scene.objects):
        if obj.type=='MESH':
            bpy.context.view_layer.objects.active=obj
            bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
            if obj.data.shape_keys:bpy.ops.object.shape_key_remove(all=True,apply_mix=True)
            for mod in list(obj.modifiers):
                if mod.type=='MASK':bpy.ops.object.modifier_apply(modifier=mod.name)
    clips(rig);export_materials(name)
    bpy.context.scene.render.fps=30
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource'/(name+'.blend')))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_simplify_factor=.5,path_mode='AUTO')
    print('CHARACTER EXPORTED',name,len(human.data.polygons),flush=True)
