# Jay A Career — Realism revision

Open **Assets/JayACareer/Scenes/JayACareer_Realism.unity** using **Unity 6000.3.24f1**.
The earlier JayACareer_Main scene remains available for recovery. Assignment 1 submission files are separate and preserved.

## Play

WASD/mouse move/look. O/Q opens/closes the lab door. T talks to Dr. Hale; T continues his briefing. O at the preparation bench creates one sealed package. L carries/places it; R rotates. Aim at the customer counter and press L to dock it. T talks to the customer, then A accepts $50, C counters $75 with 50% acceptance, or R rejects. Refusal/rejection requires returning the same package to the lab return shelf. C at the sink cleans, then O at the bedroom bed advances the day. Cash lasts for the current session only.

V changes first/third person. F toggles the flashlight. O at the wall switch toggles the four lab lights. Esc pauses; Resume recaptures the mouse. O on the bedroom chair enters arrangement: WASD move, Q/R rotate, Enter confirms clear space and Esc cancels.

## Authoring

To build interactively, open this project directly in Unity Hub and choose **Jay A Career → Build Final Windows Game**. The command selects the realism scene and creates its output/report directories automatically. See `Deliverables/FINISH THE BUILD.md` for the remaining verification steps.

The environment, characters, lights and stations are saved scene objects and Prefabs. Gameplay initialization sets task state and visibility. Replace model children beneath interaction roots to retain colliders and behaviour.

- `Tools/build_scene.cs`: reproducible Editor API scene authoring. Running it rewrites the realism scene, so save any manual revisions separately before rebuilding.
- `Tools/author_environment.py`, `Tools/author_humans.py`: Blender source generation.
- `ArtSource/`: editable Blender files; exported FBX and separate textures are already included in Assets.
- `REALISM_ASSET_CREDITS.md`, `Deliverables/asset-register.json`: sources, authors, license and adaptations.
- `Tools/rules_checks.cs`, `Tools/scene_checks.cs`: focused rules and Play mode integration checks. Results are in Deliverables/Evidence.
- `Tools/create_gdd.py`, `Tools/create_map.py`: revised submission document/map source.

The authoring tool downloads under Tools/Blender, Tools/Downloads and Tools/MPFB are local working dependencies. The project archive excludes portable executable downloads and caches. Blender 4.5 LTS plus MPFB 2.0.18 is needed only to regenerate human models. Selected CC0 source assets and per-file license headers are retained.

## Evidence automation

Normal play never starts automation. Launch the Windows executable with `-jay-demo` for a scripted two-day route and JPEG capture, or `-jay-benchmark` for the same route without capture overhead. Output is written beside the executable under `Evidence/Demo` or `Evidence/Benchmark`. The route uses CharacterController movement and the same gameplay methods; it is not a recording of human keyboard play. Manual input/feel checks should complement these reports.

## Packaging

The complete project archive contains Assets, Packages, ProjectSettings, ArtSource, authoring scripts, selected MakeHuman CC0 source assets and documentation. It excludes Library, Temp, Obj, Logs, build outputs, downloaded portable tools and generated review/capture caches. Unity restores package dependencies on first open. The Windows build, GDD DOCX/PDF, map and demonstration are separate deliverables.

No publication or course submission is performed by these scripts.

## Current verification status

- Latest player: **Deliverables/Windows Labeled 20261015/Jay A Career.exe**; packaged as **Jay A Career - Windows Labeled.zip**. Earlier players/ZIPs are retained.
- A fresh full Unity BuildPipeline.BuildPlayer build succeeded on 15 October 2026: **0 errors, 1 warning(s), 35.0 seconds**. Scene data and scripts were rebuilt together using the project-local Bee response-file launcher and original Unity generator. No DLL transplant or OS security change was used.
- The final build CLI exited successfully with code 0. The complete output copy is recorded in labeled-copy-provenance.json. Earlier failed authoring/shutdown attempts are superseded by this successful final build.
- Yellow handwritten notes identify all four liftable objects and ten task/furniture locations. Liftable prompts show the object name and controls. Tables/desks are labeled fixed furniture.
- Lab ceiling fixtures start **off**. F controls only the flashlight. O at the labeled Lab lights switch controls the four ceiling fixtures. Clear window openings and shadowed daylight fills admit exterior light; low ambient colours make deeper areas subdued and the room dark at night when both light sources are off.
- The preceding player passed the user's physical movement, mouse look, pause/Resume, camera, door, flashlight toggle and Quit checks. The user did not test lifting/placing and requested these labels/lighting changes. **Targeted physical lift/place and lighting feedback is pending.** See MANUAL PLAYER CHECKS.md and Evidence/manual-player-review-20261015.json.
- The labeled player passed **40 automated input/physics checks** and a visible two-day 1920×1080 route, ending at Day 3/$150: **59.8 average FPS / 58.6 FPS 1% low** on Ryzen 7 4800H / GTX 1650. Prior pure daily rules passed 72 assertions; prior scene integration passed 61 checks.
- Assignment 2 remains 30 pages (DOCX/PDF), with measured map, updated screenshots, source archive and scripted Windows-player demonstration. The video is silent and does not represent human keyboard play.

Original Assignment 1 files and backups are preserved. No publication or submission has occurred.
