using UnityEngine;
namespace EverythingGrows
{
    // A shared room returns to the actual house used, including carried/reinstalled houses.
    public sealed class HouseInteriorLink : MonoBehaviour
    {
        public bool returnToHouse;
        public bool Travel(FarmGame farm,MapExit exit) => returnToHouse
            ? GameSession.ReturnFromHouse(farm,exit.destinationScene,exit.arrivalId)
            : GameSession.EnterHouse(farm,exit.destinationScene,exit.arrivalId);
        public static void EnsureDoor(FarmBuilding building)
        {
            if(building.GetComponentInChildren<HouseInteriorLink>()!=null)return;
            var prefab=Resources.Load<GameObject>("Buildings/HouseDoor");if(prefab==null)return;
            var door=Object.Instantiate(prefab,building.transform);door.name="House entrance - E";
            var bounds=building.WorldFootprint;door.transform.position=new Vector3(bounds.center.x,bounds.yMin-.7f,0);
            var scale=building.transform.lossyScale;door.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1);
        }
    }
}
