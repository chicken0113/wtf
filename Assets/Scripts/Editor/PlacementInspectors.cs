using UnityEditor;
using UnityEngine;
using EverythingGrows;

[CustomEditor(typeof(FarmItemPlacement)),CanEditMultipleObjects]
public sealed class FarmItemPlacementInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
        if(EditorGUI.EndChangeCheck()) foreach(var value in targets)
        {
            var item=(FarmItemPlacement)value; var renderer=item.GetComponent<SpriteRenderer>();
            if(item.forage!=null && !EditorUtility.IsPersistent(item) && item.GetComponent<MapObjectId>()==null){var id=Undo.AddComponent<MapObjectId>(item.gameObject);id.AssignNew();}
            if(renderer==null || item.DisplayItem==null) continue;
            var art=new FarmArt(); int index=art.ItemIndex(item.DisplayItem);
            Undo.RecordObject(renderer,"Update item preview"); renderer.sprite=item.DisplayItem.itemSprite!=null?item.DisplayItem.itemSprite:EditableMapBuilder.PreviewIcon(index); renderer.color=Color.white; EditorUtility.SetDirty(renderer);
        }
        EditorGUILayout.HelpBox("Forage를 연결하면 수량·재생 기능이 있는 채집물입니다. 비우면 기존 Item 바닥 아이템입니다. 위치와 크기는 Play에도 적용됩니다. 복제 후에는 고급 > 현재 맵 저장 ID 준비로 ID를 정리하세요.",MessageType.Info);
    }
}
[CustomEditor(typeof(EnemyPlacement)),CanEditMultipleObjects]
public sealed class EnemyPlacementInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
        if(EditorGUI.EndChangeCheck()) foreach(var value in targets)
        {
            var enemy=(EnemyPlacement)value; var renderer=enemy.GetComponent<SpriteRenderer>(); if(renderer==null) continue;
            Undo.RecordObject(renderer,"Update enemy preview"); renderer.sprite=EditableMapBuilder.PreviewIcon((enemy.monsterType!=null?enemy.monsterType.IsRanged(enemy.ranged):enemy.ranged)?21:20); EditorUtility.SetDirty(renderer);
        }
        EditorGUILayout.HelpBox("Monster Type은 종류별 공통 보상 에셋입니다. 비우면 Ranged에 따라 Chaser/Spitter를 사용합니다. 명시된 종류의 Behavior가 공격 방식을 정하며 Legacy일 때만 Ranged를 사용합니다. 훈련 표적은 보상을 주지 않습니다.",MessageType.Info);
    }
}
