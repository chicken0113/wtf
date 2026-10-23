using UnityEditor;
using UnityEngine;
using EverythingGrows;

[CustomEditor(typeof(PlantDefinition))]
public sealed class PlantDefinitionInspector : Editor
{
    private void Field(string name,string label=null)
    {
        var property=serializedObject.FindProperty(name);
        if(label==null)EditorGUILayout.PropertyField(property,true);
        else EditorGUILayout.PropertyField(property,new GUIContent(label,property.tooltip),true);
    }
    private static void Section(string text) { EditorGUILayout.Space();EditorGUILayout.LabelField(text,EditorStyles.boldLabel); }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using(new EditorGUI.DisabledScope(true))Field("m_Script");
        Field("label");Field("description");Field("effect","Item use (held)");
        Field("useEffect","Special use effect");Field("consumeOnUse","Consume on successful use");
        Field("stackable");Field("sellPrice");Field("transformed");Field("isSeed");
        Section("Farming progression");Field("requiredFarmingLevel");Field("harvestExperience");
        Section("Planted growth / 성장 설정");
        var item=(PlantDefinition)target;
        var type=serializedObject.FindProperty("growthType");
        if(type.intValue==0)
        {
            var legacyType=item.GrowthType;var legacyFunction=item.Function;
            type.intValue=(int)legacyType;
            serializedObject.FindProperty("growthFunction").intValue=(int)legacyFunction;
            serializedObject.FindProperty("functionSeconds").floatValue=0;
        }
        int selected=EditorGUILayout.Popup("Growth type / 성장 유형",(int)item.GrowthType-1,new[]{"수확형 / Harvest","생산형 / Producer","기능형 / Functional"});
        // Upgrade legacy values only when this asset is opened for editing.
        if(selected!=(int)item.GrowthType-1)type.intValue=selected+1;
        var active=type.intValue==0?item.GrowthType:(PlantGrowthType)type.intValue;
        Field("growthSeconds");
        if(active==PlantGrowthType.Harvest)
        {
            Field("harvestResult","Harvest item (empty = self)");Field("harvestQuantity","Harvest quantity");
            EditorGUILayout.HelpBox("수확 시 지정한 수량을 얻고 심어진 대상은 사라집니다.",MessageType.Info);
        }
        else if(active==PlantGrowthType.Producer)
        {
            Field("harvestResult","Produced item (empty = self)");Field("harvestQuantity","Quantity per harvest");
            Field("regrowSeconds","Regrowth seconds (0 = timed production)");
            if(serializedObject.FindProperty("regrowSeconds").floatValue<=0)Field("productionSeconds","Production interval (seconds)");
            EditorGUILayout.HelpBox("대상은 남아 반복 생산합니다. Regrowth가 0이면 생산 간격 후 수확 가능해집니다. 0보다 크면 첫 성숙 시 수확하고, 다시 물을 줘야 지정 시간 후 재수확합니다.",MessageType.Info);
        }
        else
        {
            Field("growthFunction","Grown function / 성장 후 기능");
            var function=(PlantFunction)serializedObject.FindProperty("growthFunction").intValue;
            if(type.intValue==0)function=item.Function;
            if(function==PlantFunction.Till || function==PlantFunction.Water)Field("functionSeconds","Action interval (0 = every tick)");
            if(function==PlantFunction.Water)Field("effectRadius","Water range");
            if(function==PlantFunction.Storage)Field("storageBonus");
            Field("harvestResult","Recovered item (empty = self)");
            EditorGUILayout.HelpBox("성숙하면 심어진 상태로 기능합니다. 우클릭으로 회수할 수 있으며, 경작은 인접한 상하좌우 네 칸에 적용됩니다. 보관 기능은 회수 결과 아이템의 Storage Bonus를 사용합니다.",MessageType.Info);
        }
        Section("Item appearance");Field("itemSprite");Field("iconIndex");Field("color");Field("plantedDisplaySize");Field("matureDisplaySize");
        var held=(GrowthEffect)serializedObject.FindProperty("effect").intValue;
        if(held==GrowthEffect.Water) { Section("Held watering tool");Field("effectRadius"); }
        if(held==GrowthEffect.Storage && active!=PlantGrowthType.Functional) { Section("Storage item");Field("storageBonus"); }
        if(held==GrowthEffect.Weapon)
        { Section("Combat");Field("ranged");Field("damage");Field("attackRange");Field("attackInterval"); }
        else if(item.useEffect!=null) { Section("Special use");Field("attackInterval","Use interval (seconds)"); }
        serializedObject.ApplyModifiedProperties();
    }
}
