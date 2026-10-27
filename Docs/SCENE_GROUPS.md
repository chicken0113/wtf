# Scene groups

Scenes are organized by purpose, with their original names and GUIDs intact:

| Folder | Purpose | Scenes |
| --- | --- | --- |
| `Assets/Scenes/OpenWorld` | Current open-field direction | DungeonEntrance, ForestClearing |
| `Assets/Scenes/LegacyVillage` | Earlier village/farm/tower prototype | FirstGarden, FarmTownPassage, Town, GeneralStore, FarmHouseInterior, GuildInterior, SmithInterior, HospitalInterior, TowerFirstFloor, CombatTest |
| `Assets/Scenes/Tests` | Practice area | TrainingGround |
| `Assets/Scenes/System` | Shared title/menu | MainMenu |

This separates authoring work, not gameplay. LegacyVillage remains connected and enabled because it still supplies the current starting farm, shops and interiors. No levels or placements were removed, no starting map was changed, and no save was reset. OpenWorld is the focus for future field authoring.

The existing management windows now display folder/scene labels in their scene selectors. `ScenePaths` maps old paths to new paths when loading saves, including per-map state/death bags, checkpoints, house return, night visits and accepted guild quests. Version 1 saves retain their data; normal atomic autosaving writes updated paths later.

`SceneOrganization` is an explicit request-driven editor migration, not a recurring organizer. It uses AssetDatabase.MoveAsset, updates serialized routes, spawn regions, house-door prefab, guild catalog, code constants and Build Settings. It never saves or closes a loaded scene. Loaded unsaved scenes have their path fields patched in memory and remain dirty; save them normally after editing. Tests use an isolated project and synthetic save data.
