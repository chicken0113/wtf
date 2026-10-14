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
            Undo.RecordObject(renderer,"Update item preview"); renderer.sprite=EditableMapBuilder.PreviewIcon(index); renderer.color=Color.white; EditorUtility.SetDirty(renderer);
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
            Undo.RecordObject(renderer,"Update enemy preview"); renderer.sprite=EditableMapBuilder.PreviewIcon(enemy.ranged?21:20); EditorUtility.SetDirty(renderer);
        }
        EditorGUILayout.HelpBox("Ranged를 켜면 원거리 적, 끄면 추격형 적입니다. 안전 구역 안에서는 생성되지 않습니다.",MessageType.Info);
    }
}
