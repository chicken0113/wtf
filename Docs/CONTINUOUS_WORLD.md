# Continuous starter world

New Game starts in `Assets/Scenes/OpenWorld/ForestClearing.unity`, reusing its GUID and editable 52 × 44 terrain. Start/automatic default respawn is the waterside clearing (-12,-10). There is no protected farm or passive safe-area healing.

- The old exterior exit is inactive. Walking around the map does not trigger scene travel. The old arrival marker remains for legacy-save/route compatibility.
- South field and north woodland are separate SpawnRegion assets, using existing day spawn/night sleep/offscreen departure rules. South: interval 10s, max 2; north: 8s, max 4. Values are prototype settings, editable with the existing regional settings window. Both Chaser and Spitter are eligible; the existing boss stays at its authored home.
- A fixed night merchant at (-10,-8) sells seeds, sword, bow, camping bed and checkpoint consumable, and accepts existing sellable items. The probabilistic night visitor remains independent.
- Starting tools/seeds stay unchanged. One sword and one camping bed are loose starting supplies near the clearing; they use existing pickup/drop/save behavior and do not replenish like a shop.
- Existing forage nodes, unusual grove, boss clearing and returning trails remain. No wave, new item transformation, building system, extra scene or loading/streaming layer was added.

This is a single-field prototype, not a finished large world. Edit normal Ground/Decoration/Items objects, NPC profiles and SpawnRegion assets; no runtime layout regeneration. `SingleWorldSetup` is an explicit one-time authoring pass, skips a dirty loaded target, preserves unrelated editor work, and never closes/saves another scene.

Old saves continue in their saved scene; they are not reset or teleported into this prototype. Use the title menu's New Game flow to start in this world (existing confirmation/archive behavior retained). LegacyVillage and its build entries remain for older saves/testing. The currently edited DungeonEntrance is preserved separately.

Validation: isolated edit-mode layout/routes/idempotence and isolated Play checks for starter pickup/weapon, till→plant→water→harvest, night shop buy/sell, checkpoint/death bag and disk reload/recovery. Never run fixture methods against the real save directory.
