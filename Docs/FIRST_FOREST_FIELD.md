# First forest field

`Assets/Scenes/OpenWorld/ForestClearing.unity` is the first open-field layout prototype.

- 52 × 44 world units; eastern automatic exit keeps the existing destination and arrival IDs.
- Southwest pond and open grass: a possible player camp, not a protected zone. Walkable grass and trails permit farming; pond collision prevents entry/planting.
- Western abandoned campsite: existing replenishing stone forage.
- Northwest pale-stone grove: existing one-time cloud find.
- Northeast clearing: existing fixed field boss, same home and placement identity.
- Curved main trail connects the locations; eastern shortcut rejoins the southern trail. Existing loose pickups and night merchant remain.

Edit `Ground` and `Decoration` under `EditableMap` in Scene view. Named landmark subgroups hold ordinary sprites and MapObstacle components. Old terrain/scenery groups are inactive for reference. This layout adds no new enemy pattern, quest, safe-camp rule or item transformation.

`ForestFieldLayout.Apply/Author` is an explicit, idempotent one-time authoring pass with dirty-scene protection; it never runs on startup/Play. `Check` checks trail clearance, pond/camp permissions, forage accessibility, boss presence and automatic exterior travel in an isolated project. Existing saves and stable IDs are preserved; a previously saved player location is not teleported to the new entrance.
