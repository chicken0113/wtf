# Project instructions
- This is a new standalone Unity 6000.3.25f1 project, wtf. Work in C:/Users/MSI/wtf. Keep C:/Users/MSI/what-the-farm, C:/Users/MSI/EverythingGrows and the older 농사 project untouched.
- Use the 2D top-down prototype to test Docs/GAME_DESIGN.md. Do not import old game systems/assets automatically.
- Data goes in PlantDefinition assets; shared detach, planting and growth rules belong in gameplay scripts.
- Objects, background and inventory slots can be detached from the beginning. Removal must affect the original role.
- Protect essential input and the final inventory slot. Existing held items must never be silently deleted when capacity changes.
- Keep .meta files, preserve user scene changes, and use an isolated Unity batch check for destructive validation.
- Remote: https://github.com/chicken0113/wtf.git. Do not use the older what-the-farm remote.
- Develop slowly in small milestones. Focus on detaching and farming first; combat is a future direction. No crafting, assembly or building system, no fixed ending, and do not add exploration/business management as pillars.
