using UnityEngine;
using UnityEditor;

// Editor-only recipe; no runtime terrain generation or new save format.
public sealed class WorldGenerationSettings : ScriptableObject
{
    public int seed=281026;
    [Range(40,96)] public int width=64;
    [Range(40,96)] public int height=56;
    [Range(.05f,.35f)] public float waterAmount=.18f;
    [Range(.2f,.8f)] public float forestAmount=.55f;
    [Range(0,.3f)] public float treeDensity=.13f;
    public SceneAsset scene;
    public void Validate()
    {
        if(width<40||width>96||height<40||height>96||width%2!=0||height%2!=0)throw new System.ArgumentException("맵 크기는 40~96의 짝수로 지정하세요.");
        if(!float.IsFinite(waterAmount)||waterAmount<.05f||waterAmount>.35f||!float.IsFinite(forestAmount)||forestAmount<.2f||forestAmount>.8f||!float.IsFinite(treeDensity)||treeDensity<0||treeDensity>.3f)throw new System.ArgumentException("지형 비율이 허용 범위를 벗어났습니다.");
    }
}
