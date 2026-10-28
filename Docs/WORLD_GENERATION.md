# Editor world generation

Open **Tools > Everything Grows > 월드 생성**. Configure a seed, even width/height (40–96), water amount, forest threshold and tree density. Generate always creates a uniquely named new Scene under `Assets/Scenes/OpenWorld`, plus a recipe under `Assets/Settings/WorldGeneration`. It does not replace an existing scene. The open template's unsaved changes are not consumed: generation reads the saved ForestClearing via a preview scene and preserves all currently loaded work.

The initial default is `GeneratedWorld.unity` (64×56, seed 281026). New Game starts there; continuing an older save keeps its scene. Subsequent generated variants are opened manually from the generator window or Project panel for Play testing. The original ForestClearing and DungeonEntrance remain unchanged.

Generation stages:
1. Seeded noise creates irregular meadow/forest regions, ponds and a winding stream with land crossings. The amount controls shape/size, not an exact percentage of final cells.
2. Establish a starting clearing, remove disconnected land, choose separated boss/camp clearings and reserve walkable connections. Connections are terrain/vegetation spacing, not a drawn road network.
3. Author editable Ground chunks (8×8), trees and merged water collision rows. Land permits farming; water prevents movement/planting while allowing shots across it.
4. Reposition the existing fixed night shop, night visitor candidates, forage, loose supplies and prototype boss. Existing play behavior is reused; new scenery is not promised to be interactable.
5. Validate actual collision paths/access. Register a scene-specific population region and save a recipe. Irregular terrain biomes are visual placement data; population regions retain the existing rectangle-based editor settings. No biome growth modifiers are introduced.

`WorldGenerationPlan` owns deterministic terrain/clearings/reserved space and uses its own RNG; `WorldSceneGenerator` presents those cells with current 2D assets and authors the scene. `WorldGenerationWindow` owns only editor UI/explicit requests. There is no generation during Play, runtime streaming, new save version, wave system or 3D art migration.

After generation, edit normal objects under Ground/Decoration/Items and existing NPC/monster settings. Runtime loads saved scene objects. Generating another world never resets hand edits or old saves. A matching seed/settings reproduces terrain and layout for this algorithm/version, not asset GUIDs/save identities or manual edits. A failed generation reports the error; any newly saved scene is retained for inspection.

Tests: 24 seeds at two sizes for deterministic cells/landmark connectivity, actual default scene collision/navigation/water checks, unsaved-template preservation, and isolated Play farming/trade/combat/checkpoint/death-cache/reload. No source save files are used.

Art discussion: 3D-source/pixel-like visuals are being considered, not approved for implementation. Keep layout data independent of presentation; current output uses the existing 2D assets.
