import bpy,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Tools/MPFB/mpfb2-2.0.18/src'))
import mpfb
original=bpy.utils.extension_path_user
bpy.utils.extension_path_user=lambda package,**kwargs:str(root/'Tools/MPFB/UserData') if package=='mpfb' else original(package,**kwargs)
mpfb.get_preference=lambda name: str(root/'Tools/MPFB/UserData') if name=='mpfb_user_data' else False if name in ['mh_auto_user_data'] else None
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
macro=TargetService.get_default_macro_info_dict();macro['gender']=1
human=HumanService.create_human(macro_detail_dict=macro)
rig=HumanService.add_builtin_rig(human,'default')
print('BONES',[b.name for b in rig.data.bones])
print('DIMENSIONS',list(human.dimensions))
