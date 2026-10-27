namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        public const string HospitalScene = "Assets/Scenes/LegacyVillage/HospitalInterior.unity";
        public const string HospitalRecoveryArrival = "hospital_recovery";

        public static bool ReturnToHospital(FarmGame farm)
        {
            // Travel saves the departed map and player, then autosaves again after validated arrival.
            if (Travel(farm, HospitalScene, HospitalRecoveryArrival)) return true;
            if (farm != null)
                farm.Notify("병원으로 이동하지 못했습니다. " + (LastError ?? "Play 모드와 병원 씬 연결을 확인하세요."));
            return false;
        }
    }
}
