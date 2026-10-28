using UnityEngine;
namespace EverythingGrows
{
    // Query-only collider ownership. Movement and shots keep the shared planar sweeps.
    public sealed class ModelPickProxy : MonoBehaviour { public SpriteRenderer owner; }
}
