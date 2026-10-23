namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private FarmingProgression progression;
        public int FarmingLevel => progression?.Level??1;
        public int FarmingExperience => progression?.ExperienceInLevel??0;
        public int FarmingExperienceToNextLevel => progression?.ExperienceToNextLevel??0;
        internal void RecordFarmHarvest(PlantDefinition plantedItem)
        {
            if(progression.Add(plantedItem.harvestExperience))
                Notify("Farming level up! Level "+FarmingLevel+".");
        }
    }
}
