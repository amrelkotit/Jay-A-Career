# Jay A Career Assignment 2

Game World and Game Mechanics submission for DMET909 Video Game Design.

This `assignment-2` branch contains the complete realism revision. Assignment 1 remains on `main` and in Git history.

## Open and play in Unity

1. Clone or download **this branch**, including `Assets`, `Packages` and `ProjectSettings`.
2. Add the repository folder in Unity Hub and open with **Unity 6000.3.24f1**.
3. Allow Unity to restore packages and import assets.
4. Open `Assets/JayACareer/Scenes/JayACareer_Realism.unity` and press Play.

URP 17.3.0, Cinemachine 3.1.5 and Input System versions are recorded in `Packages/manifest.json` and `packages-lock.json`. Blender and MPFB are authoring tools, not requirements for running the project.

## Controls and complete daily route

WASD/mouse move/look. O/Q opens/closes the lab door. T on Dr. Hale begins the briefing; T continues it. O at the yellow Preparation bench note creates one package. L lifts/places and R rotates. Carry the package across the road, aim at the yellow Delivery counter and press L to dock it. T at the customer starts negotiation: A accepts $50, C counters $75 with a disclosed 50% acceptance chance, and R rejects. A refusal/rejection requires returning the same package to the lab return shelf. C at the sink removes clutter, then O at the bedroom bed begins another day and retains session cash.

V switches first/third person. Dialogue blends to the speaker through Cinemachine. F toggles the flashlight. O at the yellow Lab lights note beside the entrance toggles the ceiling fixtures. Fixtures start off; windows admit daylight and the deep lab remains subdued. Esc pauses; Resume restores mouse capture. The bedroom chair supports O, WASD, Q/R, Enter and Esc for bounded arrangement.

The bedroom entrance now has its own O light switch, independent of the laboratory fixtures and flashlight. Both roadside lamps automatically fade on and visibly glow at dusk, then switch off in the morning. Preparing, cleaning and sleeping play humanoid animations with Cinemachine action views before applying their world-state changes. Preparation and cleaning take 3.2 seconds; sleep takes 5 seconds. Pause freezes these actions, and completion restores the previous first/third-person view.

For customer handoff, place the package fully on the counter before pressing T. A package resting on the counter is also recognized when it was placed through ordinary surface placement.

## Assignment evidence

- 36 described object types and 23 documented mechanics.
- Five world-changing actions: preparation, carrying/placement, negotiation, cleanup and sleep.
- Three Cinemachine behaviors: first person, third-person follow and blended dialogue focus.
- Three lighting behaviors: 120-second ambient cycle, flashlight and switched lab fixtures.
- `Deliverables/Jay A Career - Assignment 2 Revised GDD.pdf` and the measured world map.
- `Deliverables/Evidence/`: build, input, rules, lighting and performance reports.

The fresh full Windows build succeeded with zero errors. Automated player checks passed 40 assertions; the visible 1080p two-day route averaged 59.8 FPS (1% low 58.6 FPS) on Ryzen 7 4800H / GTX 1650. These figures are automated measurements, not a claim of human testing. The prior player passed physical movement, pause and camera checks; the new labeled lift/place and lighting review is documented separately.

The 18 October lighting/actions update was separately built with zero errors and passed 32 standalone integration checks. These cover the bedroom switch, evening lamp emission, timed actions, pause, package/payment state, view restoration and the next morning. See `Deliverables/Evidence/Lights and Actions Update 20261018.md`, `lights-actions-build-20261018.json` and `LightsActions-Player-Final-20261018/review-result.json`. The measured performance figures above belong to the earlier build; they were not rerun for this update.

## Included files and licenses

`Assets`, `Packages`, `ProjectSettings`, editable `ArtSource`, reproducible authoring helpers, source/license metadata and documentation are included. Generated Unity caches, build binaries and portable tool executables are excluded.

See `REALISM_ASSET_CREDITS.md` and `Deliverables/asset-register.json` for original work and selected CC0 assets. MPFB tool source retains its GPL license; it is not linked into the game. The fictional sealed package has no real production procedure. No persistent saves, driving or combat are implemented.

Course submission is performed separately through the submission PDF and form.
