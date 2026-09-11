# Aqua glass game implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development for the independent core, followed by integrated Unity verification.

**Goal:** A new playable Unity pipe puzzle with transparent glass and animated blue water matching the supplied reference.

**Architecture:** Pure C# puzzle model feeds a uGUI board. Each reusable tile has a rotating visual root and separate procedural glass and shader-driven water layers. One application controller owns screen flow and saves.

**Tech Stack:** Unity 6000.5.7f1, C#, built-in pipeline, uGUI, ShaderLab.

**Spec:** ../specs/2026-09-11-aqua-glass-design.md

## Global constraints
- New project only in pips2. Do not alter other game projects.
- Reference glass/water appearance is the visual acceptance target.
- Keep logic separate from presentation. No paid assets or remote runtime dependencies.
- Portrait safe-area UI, 30 solvable levels and independent water propagation.

## Task 1: Puzzle model and logic tests
- [ ] Implement Assets/AquaPath/Scripts/Core/Puzzle.cs and Tests/PuzzleTests.cs.
- [ ] Contract: Shape {Straight, Elbow, Tee, Cross, Source, Target}; Cell {Shape, Rotation, Solution, Locked}; Puzzle {Size, Cells, Source, Target, Moves, IdealMoves, Rotate(int), Reset(), Network(), Solved}; LevelFactory.Create(int); Directions.Mask(Shape,int); ProgressRating.Stars(int,int).
- [ ] Run pure C# tests with Unity's bundled Mono compiler. Use hand-checked connection fixtures and all 30 level solutions, unsolved starts, reset and star thresholds.

## Task 2: Glass and animated water visual
- [ ] Create GlassArt.cs, PipeView.cs and GlassWater.shader.
- [ ] Generate transparent shell sprites and per-pixel flow masks from the same pipe geometry. Rims and silhouette must stay aligned with water.
- [ ] Keep all rotatable children under VisualRoot. Water material reveals along an entry-to-network distance field; disconnection drains.
- [ ] Import in Unity and visually inspect dry/filled sprites on the gameplay backdrop.

## Task 3: Complete playable screens
- [ ] Create AquaApp.cs, AquaUI.cs, BoardView.cs and AudioController.cs.
- [ ] Connect menu, 2-column level list, help, saved settings, pause, restart, hint, moves, stars, water completion, next and progress.
- [ ] Fit board size and all touch targets to portrait canvas and safe area.

## Task 4: Unity assets and verification
- [ ] Editor builder saves Main.unity, all six pipe prefabs, level catalog and mobile player settings.
- [ ] Editor Play Mode verification clicks tiles through public UI events, checks victory/next/reset across 4×4, 5×5, 6×6, 7×7; captures genuine gameplay PNGs.
- [ ] Build and run a Windows player for a readily playable deliverable; build Android if installed toolchain is usable.
- [ ] Review screenshots, fix visible defects, inspect final logs and deliver the project with accurate test evidence.
