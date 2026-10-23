using System;

namespace EverythingGrows
{
    // Total XP is saved; level and progress are derived from the editable thresholds.
    internal sealed class FarmingProgression
    {
        private readonly FarmingLevelSettings settings;
        public int TotalExperience { get; private set; }
        public int Level { get; private set; } = 1;
        public int ExperienceInLevel { get; private set; }
        public int ExperienceToNextLevel => Level>=settings.MaxLevel?0:settings.experienceToNextLevel[Level-1];
        public FarmingProgression(FarmingLevelSettings settings) { this.settings=settings; }
        public void Restore(int total)
        {
            if(total<0)throw new ArgumentOutOfRangeException(nameof(total),"Farming experience cannot be negative.");
            TotalExperience=total;Level=1;int remaining=total;
            foreach(int required in settings.experienceToNextLevel)
            {
                if(required<1)throw new InvalidOperationException("Farming level XP thresholds must be positive.");
                if(remaining<required)break;
                remaining-=required;Level++;
            }
            ExperienceInLevel=Level>=settings.MaxLevel?0:remaining;
        }
        public bool Add(int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            int before=Level;
            Restore((int)Math.Min(int.MaxValue,(long)TotalExperience+amount));
            return Level>before;
        }
    }
}
