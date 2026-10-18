# Bedroom switch and daily action animations

## Project and source control

- Working Unity project: `E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career`.
- Main scene: `Assets/JayACareer/Scenes/JayACareer_Realism.unity`.
- Separate Git checkout: `..\Jay A Career GitHub`.
- Verified remote: <https://github.com/amrelkotit/Jay-A-Career/tree/assignment-2>, commit `173624a4138616e39618c75bdb139640f79148e3`.
- The working project contains newer local changes. This update has not been committed or pushed.
- Originals are preserved in `Deliverables/Backups/LightsActions-Authorized-20261018-062434`.

## Requested changes

| Requirement | Result | How to use |
| --- | --- | --- |
| Street lamps turn on automatically in the evening | Automatic beams retained for both lamps; fixed the glass/bulb material so it also visibly glows after dusk; morning switches both effects off | Wait for the day/night cycle |
| Bedroom light switch | Added a labeled wall switch beside the bedroom entrance; it controls only the bedroom light | Look at the switch and press O |
| Preparation animation | Jay reaches toward a preview parcel at the preparation bench; the usable delivery package appears when the animation finishes | Complete the briefing, then press O at the bench |
| Cleaning animation | Jay wipes the sink worktop with a handheld cloth; the dirty state clears when the animation finishes | Finish the delivery/return step, then press C at the sink |
| Sleeping animation | Jay lies on his back with his head at the pillow; the next day starts when the animation finishes | Finish cleaning, then press O at the bed |

Each action uses a Cinemachine shot, blocks overlapping actions and movement, supports pausing with Escape, and restores the previous first/third-person view. Animation clips use the existing humanoid avatar and hand IK reaches the parcel/worktop. NPC controllers remain separate from the player's new controller.

The original street material was saved with black emission, which let URP remove its emission keyword. The replacement `EveningLampBulbActive.mat` keeps the emission variant available in builds; the day/night controller still sets the actual visible emission through per-renderer property blocks. The original material is preserved.

## Assignment brief read

Source: `E:\El Gam3a\Semester 9\Video Game Design\Assignment 2 (Assignment ).pdf`, three pages.

The brief requires a world/mechanics write-up (including at least 30 objects and 20 mechanics) and a prototype with at least five player actions, three Cinemachine camera behaviors, and three lighting behaviors. This update covers the requested lighting and animations; it is not a complete audit of the written submission or every assignment criterion.

## Validation

The saved scene passed 27 play-mode integration checks in `LightsActions-Editor-Final-20261018/review-result.json`, covering lighting independence, animation state and motion, action prerequisites, duplicate requests, pause, deferred world changes, delivery payment, camera restoration, and the next morning. Captured views were inspected and the action poses refined.

The final Windows output is `Deliverables/Windows Lights Actions 20261018/Jay A Career.exe`. The build succeeded with zero errors (249,617,354 bytes); `lights-actions-build-20261018.json` records the result. Its one build warning is that the optional Pipeline runtime tooling is disabled because no runtime config is supplied.

The final standalone game passed all 32 checks in `LightsActions-Player-Final-20261018/review-result.json`, including emission variants and night emission for all four lamp renderers. The night capture was inspected and confirms the lantern glass visibly glows. The standalone log contains no gameplay exceptions. URP reports reducing shadow-map resolution to fit its existing atlas.

Final captures are in `LightsActions-Player-Final-20261018/`: street lights at night, bedroom switch on/off, preparation, cleaning, sleeping, and the next morning. The build retains its accompanying `_Data`, `MonoBleedingEdge`, and player-library folders; launch the EXE from this complete output folder.
