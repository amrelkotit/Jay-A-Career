# Manual keyboard and mouse checks

You reported that the eight quick checks below passed on Windows Fresh 20261015. The updated player is Windows Labeled 20261015. Its new yellow labels and lighting need the targeted check first.

## New labels, lift/place and lighting check

1. Open `Deliverables/Windows Labeled 20261015/Jay A Career.exe` and click inside. Enter the laboratory with **O** at the door. The ceiling lights start **off**. Daylight should illuminate the windows/entrance while the rest of the room stays subdued.
2. Find the yellow **Supply crate** note on the crate in the laboratory aisle. Aim at the crate from within a few steps; the bottom prompt should name it. Press **L** to lift it, **R** to rotate it, then aim at a clear floor area nearby and press **L** to put it down. It should stay above the floor. The empty drum and supply carton also have yellow lift/place notes.
3. For the delivery package, talk to **Dr. Hale** with **T**, then **T** to finish. Find the yellow **Preparation bench** note at the back-left of the lab. Press **O** there. The sealed **Delivery package** appears on that bench. Aim at it and press **L** to lift it; **R** rotates it.
4. Carry it across the road to the yellow **Delivery counter** note. Aim at the counter and press **L**. The package should snap onto the counter. **T** at the customer then starts negotiation. Tables/desks have identifying notes but are fixed furniture, not liftable props.
5. With **F off** and the wall switch off, compare the light near a window with the deeper room. Wait about one minute from a fresh start: outside and inside should become darker. Turn **F** on; its beam should light what you aim at. Turn **F** off again: the room should become dull/dark again. At the yellow **Lab lights** note beside the entrance, press **O**; all four ceiling fixtures should light the room even with F off. Press **O** again to switch them off.

Report which of these five steps passed, or the failed step and what happened. The earlier physical-input pass remains recorded in Evidence/manual-player-review-20261015.json.

## Previous quick checks (already passed)

1. Open the executable from its folder, with all accompanying files present. Click once inside the game.
2. Move the mouse left/right, then up/down. The camera should turn smoothly. The cursor should be captured during play.
3. Hold **W**, **S**, **A**, then **D** for about one second each. Jay should move forward, backward, left and right, and stop when you release the key. Walk against a lab wall: Jay must not pass through it.
4. Press **Esc** once. A pause panel with **Resume** and **Quit** should appear, and the pointer should become visible. Press WASD and move the mouse while paused: Jay/the camera should stay still.
5. Click **Resume**. The panel should close, the pointer should be captured, and mouse look and WASD should work again.
6. Press **V** once. The camera should blend to third person and show Jay. Press **V** again to return to first person, where Jay's head/body should not obstruct the view. Repeat beside a wall; the third-person camera should pull forward instead of showing through the wall.
7. Aim at the laboratory entrance door. The prompt should say **[O] Open door**. Press **O**, walk through, clear the doorway, then aim at the door and press **Q**. It should close. **F** should turn the flashlight on/off.
8. Press **Esc**, then click **Quit**. The game should close normally.

Report `PASS` if all eight steps work, or the step number and what happened. A screenshot/video of a failevd step is useful. Do not mark a step passed if its expected result did not occur.

## Optional complete day check

Reopen the game after the eight quick checks. For extra confidence, complete one normal day using the physical controls:

1. Open the lab door with **O**. Approach Dr. Hale, aim at him and press **T**. Press **T** again to finish his briefing.
2. Aim at the preparation bench and press **O**. One sealed package and workspace clutter should appear. Aim at the package and press **L** to pick it up; press **R** to rotate it.
3. Carry it along the road to the customer counter. Aim at the counter and press **L** to place it in the handoff position.
4. Aim at the customer and press **T**, then **A** to accept. Cash should increase from **$100 to $150** once. The acceptance key must not move Jay sideways.
5. Return to the lab, aim at the cleaning station/sink and press **C**. The visible clutter should disappear.
6. Go to the bedroom, aim at the bed and press **O**. The HUD should show **Day 2**, keep **$150**, and reset the exterior to morning.

If any step is unavailable, read its on-screen prompt and report that message. Do not skip to the bed before cleaning.

Your earlier physical keyboard/mouse pass is recorded. The labeled full build has separate automated regression checks; its targeted label, lift/place and lighting feedback remains pending.
