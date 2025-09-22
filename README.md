# PacStudent (Assessment 3) – Marker Guide

Unity Version: 2023.2.10f1

This README tells you exactly where to find each marking item (per ass3.md), how to run the project, and what to expect in each scene.

Quick Start (Run This First)
- In Unity, open the menu: ALL DONE! -> 1. Configure All Scenes
- This one click cleans and prepares all scenes, assigns audio/animator/sprite references, and creates the required demo objects.

Scenes
- 65C_AnimationPreview.unity – animation/animator preview
- 75D_ManualLevel.unity – manual level layout (present before Play)
- 85HD_AutoMovement.unity – manual level + PacStudent auto movement loop
- 100HD_ProceduralGameplay.unity – procedural level generator demo

Where each grade band can be marked

10% Z – Git repository
- .git and .gitignore at project root; frequent commits and feature branches.
- Branches: main, development, Feature-Visual, Feature-Audio, Feature-ManualLevel, Feature-Movement, Feature-LevelGenerator.
- Remote: GitHub (see git remote -v).

20% Z – Project structure
- Clean folder layout: Assets/Animations, Assets/Audio Clips, Assets/Scenes, Assets/Scripts, Assets/Sprites, Assets/StreamingAssets.
- Scene Hierarchy is auto-organized by the configuration tool.

35% Z – Audio assets
- Assets/Audio Clips contains: intro.wav, normal.wav, menu.mp3, frightened.wav, deadpresent.wav, sfx_move.wav, sfx_pellet.wav, sfx_wall.wav, sfx_death.wav.
- Demo:
  - 100HD_ProceduralGameplay: BackgroundMusicManager plays intro (≤3s) then loops normal.
  - 85HD_AutoMovement: movement SFX auto-plays in loop for the demo.
- StartMenu music (menu.mp3) is prepared by the configurator if/when a menu scene is added.

50% P – Visual assets (Sprites)
- All sprites are original and located in Assets/Sprites (Walls, Items, Pacstudent, Ghosts).
- All sprites are used in scenes (manual tiles and/or generator output).

65% C – Animations and Animator Controllers
- PacStudent: Assets/Animations/PacStudent/PacStudent.controller
- Ghosts: Assets/Animations/Ghosts/GhostAnimator.controller (+ preview variant)
- Power pellet flash: Assets/Animations/Items/PowerPellet.controller
- Preview: open 65C_AnimationPreview and press Play to see looping previews.

75% D – Manual level layout
- Scenes: 75D_ManualLevel and 85HD_AutoMovement.
- Before Play you will see an object named ManualLevel comprising only the 8 tile types from the legend (outside/inside walls/corners, T, exit, pellet, power pellet).
- Camera is auto-fit by the configurator.

85% HD – PacStudent movement
- Scene: 85HD_AutoMovement.
- AutoMovementRuntime moves PacStudent clockwise around the top-left inner block at constant speed (frame-rate independent). Movement SFX plays; scale enlarged for visibility.

100% HD – Procedural level generator
- Scene: 100HD_ProceduralGameplay (press Play).
- Script: LevelGenerationRuntime (defined in Assets/Scripts/AllDone.cs). On Start it deletes ManualLevel (if present), mirrors the top-left quadrant to build the full map (ignoring the bottom row for vertical mirror), computes rotations for wall pieces, instantiates sprites, and frames the camera to show the entire map.
- The generator adapts to any sized top-left quadrant array (markers may substitute the array to test robustness).

Key script and asset locations
- AllDone.cs (Assets/Scripts/AllDone.cs):
  - LevelGenerationRuntime – manual and procedural generation logic + camera framing
  - AutoMovementRuntime – 85HD demo logic
  - AllDoneConfigurator – editor menu “ALL DONE!” for one‑click setup
- Audio logic: Assets/Scripts/Audio/BackgroundMusicManager.cs
- Level CSV (reference/example): Assets/StreamingAssets/LevelMap.csv

How to run – checklist
1) In Unity: ALL DONE! -> 1. Configure All Scenes
2) Open 75D_ManualLevel – ManualLevel is visible before Play.
3) Open 85HD_AutoMovement – ManualLevel visible before Play; press Play to see auto movement and hear movement SFX.
4) Open 100HD_ProceduralGameplay – press Play to see ProceduralLevel generated and camera auto-framed.
5) Open 65C_AnimationPreview – press Play to preview core animations.

Constraints compliance (per ass3.md)
- 2D sprites only; no Rigidbody/CharacterController for movement.
- Scripts are original; no third‑party packages/plugins.
- Visual assets are original; audio is royalty‑free and differs from the original game.

Troubleshooting
- If a scene looks empty: run ALL DONE! again to regenerate and reassign asset references.
- If 100HD shows no tiles: ensure the sprites under Assets/Sprites/Walls and Assets/Sprites/Items exist and paths unchanged.

Thank you for reviewing!

