# Aqua Path Connect — new Unity project

The user authorized a new project in `pips2`. The attached game brief provides context for a complete mobile pipe puzzle; the new transparent-pipe reference has priority for the actual pipe art.

## Design
Unity 6000.5.7f1, built-in rendering, portrait uGUI. One saved Main scene boots a menu, level selection, help, settings and gameplay. Thirty deterministic, guaranteed-solvable puzzles grow from 4×4 to 7×7. Clockwise rotations update a flags-based connection model and a reciprocal BFS. Save progression, best stars and settings using a versioned local store.

Pipe pieces use clear, broad cylindrical walls with thick elliptic connector rims, dark fine edge refraction, white specular streaks and pale cyan reflections. Internal blue water has its own shape mask, gradient, water line, small animated bubbles and a progressively revealed fill. The glass shell stays visible when dry. Elbows follow a smooth quarter-circle. Straight, elbow, T, cross and endpoint pieces share dimensions and material language. All visual layers rotate together. No green background is carried into the assets.

The board is presented on a deep aqua panel to make the transparent shells readable. Pale aqua light rays and bubbles sit behind the portrait UI. Menus use readable rounded cards and bright primary actions. Source and destination have explicit labels and distinct markers. A hint turns one incorrect route tile by one quarter turn; restart restores the same puzzle. Pausing prevents input. Water fills connected sections and reaches the destination before the victory dialog appears.

## Acceptance
- Saved Unity scene, reusable pipe prefabs, resources, shaders and scripts exist in the new project.
- Model tests cover rotations, reciprocal connections, branches, all 30 generated solutions, reset and star boundaries.
- Unity imports and compiles without project errors.
- Play Mode exercises several board sizes, actual tile rotations, water propagation, victory, next, restart and persistence.
- Capture real Unity gameplay frames and inspect clear walls, blue water, rims, highlights, bubbles and board readability. Code compilation alone does not complete the visual task.
- Android portrait settings are configured; hardware performance and device haptics require a physical device and will be reported as unverified.
