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
            if(renderer==null || item.item==null) continue;
            var art=new FarmArt(); int index=art.ItemIndex(item.item);
            Undo.RecordObject(renderer,"Update item preview"); renderer.sprite=item.item.itemSprite!=null?item.item.itemSprite:EditableMapBuilder.PreviewIcon(index); renderer.color=Color.white; EditorUtility.SetDirty(renderer);
        }
        EditorGUILayout.HelpBox("Item에서 심을 대상을 지정합니다. 씬의 위치와 크기가 Play에서도 적용됩니다.",MessageType.Info);
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
