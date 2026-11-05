# Jay A Career — Assignment 3 (Storyline, Levels and UI)

DMET 909 Video Game Design, GUC. Branch `assignment-3`. Assignment 1 is on `main`, Assignment 2 on `assignment-2`.

Jay is behind on rent in Calder Flats. Dr. Hale offers him delivery work for Corvane: sealed CF-7 cartons. Over three story levels Jay delivers to Leo, makes one meaningful choice with Luka, and in the last level learns what Hale has been hiding before Paul, the big buyer, makes him an offer. The product stays a fictional sealed carton; the game has no real production procedure.

## Open and play

1. Open the project with **Unity 6000.3.24f1** (URP 17.3, Cinemachine 3.1.5, Input System, TextMesh Pro, Timeline).
2. Open `Assets/JayACareer/Scenes/MainMenu.unity` and press Play. (Each level scene can also be played directly; it fills in sensible defaults.)
3. To regenerate every story scene from the A2 world: **Jay A Career → Assignment 3 → Build Story Game (all scenes)**.

Scene flow (Build Settings): MainMenu → Loading → L1_FirstDelivery → L2_Middleman → L3_BigBuyer → Ending.

## Controls

| Action | Key |
|---|---|
| Move / look | WASD / mouse |
| Interact, talk, advance dialogue | **E** (also Space/Enter/click for dialogue) |
| Pick a dialogue choice | 1 – 3 or click |
| Lift / place, rotate carried item | L / R |
| Inventory / Journal | Tab / J |
| Switch first/third person | V |
| Flashlight | F |
| Pause (resume, restart level, settings, main menu) | Esc |
| Skip a cutscene | hold Space or Esc |

## What Assignment 3 adds

- **Story:** intro cutscene, three levels with 3+ narrative moments each, one meaningful choice (Luka: accept / push / refuse) with consequences carried to Level 3 and the ending, a revelation (Hale's ledger and confession) and an ending cutscene and end card.
- **Systems (Assets/JayACareer/Scripts/Story):** StoryStateManager (flags, cash, trust, heat, inventory, journal), data-driven ScriptableObject dialogue with conditions and effects, ObjectiveManager with derived objectives, StoryTriggers, gated LevelDirector transitions, Timeline cutscenes, loading screen, pause and settings, end screen.
- **UI:** dialogue box with name and portrait, objective tracker with marker, cash/rent/trust/heat status, toast messages, inventory with lore descriptions, journal, chapter cards and subtitles.
- **World:** time of day follows the story (morning → dusk → night); street lamps switch on automatically in the evening and off in the morning; colour grades, wall props (eviction notice, calendar), decals and tyre tracks; three human buyers (Leo, Luka, Paul).

## Verification

`Jay A Career → Assignment 3 → Test Playthrough → All three branches` plays the whole game from the main menu to the end screen for each Luka choice and checks every beat, flag and transition. Latest result: accepted 63/63, pushed 63/63, refused 65/65 — reports are in `Deliverables/Evidence/Assignment3`. `Record Video` re-records the prototype video with Unity Recorder.

## Credits

See `REALISM_ASSET_CREDITS.md` (Assignment 2 assets plus the Assignment 3 section: Microsoft Rocketbox avatars, MIT).
