using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmGround : MonoBehaviour
    {
        public bool canFarm=true;
        public Rect Bounds => new Rect((Vector2)transform.position-new Vector2(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y))*.5f,new Vector2(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y)));
    }
}
