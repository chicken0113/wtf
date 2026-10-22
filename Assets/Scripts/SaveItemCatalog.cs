using System;
using UnityEngine;
namespace EverythingGrows
{
    public sealed class SaveItemCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string id; public PlantDefinition item; }
        public Entry[] entries=Array.Empty<Entry>();
        public string Id(PlantDefinition item)
        {
            if(item==null) return "";
            if(entries==null) throw new InvalidOperationException("SaveItemCatalog.entries is missing. Refresh the save item catalog.");
            foreach(var entry in entries) if(entry!=null && entry.item==item) return entry.id;
            throw new InvalidOperationException("Item is missing from save catalog: "+item.name);
        }
        public PlantDefinition Resolve(string id)
        {
            if(string.IsNullOrEmpty(id)) return null;
            if(entries==null) throw new InvalidOperationException("SaveItemCatalog.entries is missing. Refresh the save item catalog.");
            foreach(var entry in entries) if(entry!=null && entry.id==id && entry.item!=null) return entry.item;
            throw new InvalidOperationException("Saved item is missing from project: "+id);
        }
    }
}
