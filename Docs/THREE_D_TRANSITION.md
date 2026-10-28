# 3D presentation transition

2D checkpoint branch: `codex/backup-2d-2026-10-28`, commit `28a293b`. It includes the saved working map, design notes and project settings before this transition. The branch is pushed to GitHub. Source gameplay saves were not played or reset.

The current `GeneratedWorld` scene now uses real mesh prefabs, a fixed angled orthographic camera, directional lighting and shadows. There is no pixelation filter. Existing project assets contain no external FBX/OBJ/GLTF models; the included Models3D prefabs are simple functional placeholders, not final art.

## Coordinate and collision contract

Existing simulation and version-1 saves retain `(x,y)` ground coordinates. The Unity ground plane is XY, and upward model height is negative Z. This is deliberate: farming, movement, shots, pathfinding, region lifecycle and saved IDs keep one shared planar rule system. This is a 3D presentation of planar gameplay, not a Rigidbody/vertical-physics rewrite. Standard Y-up model prefabs are oriented by `World3D.ModelRotation`; import models upright with feet at the origin and approximately unit size.

`WorldProjection` is the only screen-to-ground conversion. Query-only 3D colliders on layer 30 map item/NPC model clicks back to their logical owner. Gameplay collision remains `MapCollision` sweeps. Region offscreen checks project the actual camera corners onto the ground, rather than using its offset transform position. Layer 29 is reserved for tree visibility queries; obstructing trees are hidden while covering the player. These query colliders are triggers and do not give dropped items a physical blocking hitbox.

## Ownership and replacement

- `World3D`: per-scene camera, model library and occlusion.
- `WorldModelLibrary`: shared model kinds and item defaults.
- `ModelView3D`: a presentation adapter. Existing SpriteRenderer fields remain hidden logical handles for position, size, visibility and feedback; rendered world objects are mesh prefabs. UI inventory sprites and fullscreen lighting overlays remain 2D.
- `FarmWorldView`: soil/plant model selection and burial-to-growth display; gameplay rules stay in FarmingSystem.
- `World3DSetup` and `World3DModels`: explicit editor conversion/placeholder authoring. Never rebuild edited scenes on Play. The existing editor world generator converts new outputs to 3D too.

Item management exposes PlantDefinition.modelPrefab; monster management exposes MonsterType.modelPrefab. Assigned prefabs are not overwritten by subsequent setup. Edit placeholder prefabs in Resources/Models3D or assign replacements through the data. Prototype dimensions should be tuned alongside the existing interaction/collision footprint when replacing art.

The original 2D ForestClearing template and archived village scenes remain available. New games use the converted GeneratedWorld, and saves already in that scene continue using the same scene GUID and planar coordinates. Historical saves in other scenes remain there; they are not silently relocated.

## Verification

Isolated edit checks cover mesh terrain, angled camera, projection, camera-ground visibility, real model picking, save IDs and idempotence. Isolated Play checks cover real player meshes, movement/aim coordinate conversion, weapon use, farming, night trading, checkpoints, death bags and reload. A D3D11 render was visually inspected. No actual player save was used. The built-in Unity 3D Physics module is enabled for ray queries; the legacy 2D module remains for old components.

Open `Assets/Scenes/OpenWorld/GeneratedWorld.unity` to edit/test the current scene. Use a 3D Scene view; editable positions remain ground X/Y. Wave gameplay, new planting outcomes, final character art/rigged animation and a height-based movement system are outside this transition.
