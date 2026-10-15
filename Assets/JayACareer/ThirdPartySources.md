# Third-party assets in this project

> Historical prototype notes below describe the earlier main scene. For the authored **JayACareer_Realism** scene, see **REALISM_ASSET_CREDITS.md** and **Deliverables/asset-register.json** at the project root. That scene uses three rigged MPFB humans, complete PBR maps, and the additional industrial props recorded there.

## ModKit v1.2
- Source: https://github.com/JaronKBragg7337/asset-pack-ue-threejs-blender-unity
- License: CC0 1.0 Universal. Original `LICENSE` and `README.md` are included under `Assets/JayACareer/ThirdParty/ModKit`.
- Selected FBX: modular walls, windows, doors, roof pieces, barrels, crates, pallet, pipes, and vent. The meshes are staged as runtime-loadable Unity assets and used by `JayIndustrialAssetPass` for the lab shell and industrial props.

## Poly Haven
- Source: https://polyhaven.com/
- License: CC0 1.0 Universal; see https://polyhaven.com/license.
- Included 1K base-color textures: `asphalt_03`, `concrete_floor`, `sand_03`, and `rusty_painted_metal` from the Poly Haven asset library.
- Runtime use: asphalt on the road, sand on the surrounding ground, concrete on the lab modules, and painted metal on industrial equipment.

## Character model status
- Dr. Hale and the customer still use the existing prototype character visuals. A rigged, realistic human model and animation import remain follow-up work; the GLB importer could not be resolved in the previous editor session.

## Fictional delivery item
- Keep the existing generic sealed package. The game represents delivery and handling abstractly and contains no real-world production instructions.

