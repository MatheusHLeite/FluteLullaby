using Sirenix.OdinInspector;
using UnityEngine;

public class Item_SO : ScriptableObject {
    [FoldoutGroup("Item setup")] public string m_itemName;
    [FoldoutGroup("Item setup")] public Sprite m_icon;
    [FoldoutGroup("Item setup")][TextArea] public string m_description;
    [Space(10)]
    [FoldoutGroup("Item setup")] public Interactor m_itemPrefab;
    [FoldoutGroup("Item setup")] public ItemType m_itemType; 
    
    [FoldoutGroup("Item Offset")] public Vector3 m_itemPositionOffset;
    [FoldoutGroup("Item Offset")] public Vector3 m_itemRotationOffset;
    [FoldoutGroup("Item Offset")] public Vector3 m_itemSpawnRotation;

    [FoldoutGroup("Item setup")][GUIColor("#FFFF00")][ReadOnly] public string id;
    [FoldoutGroup("Item setup")][Button("Generate ID")]
    public void GenerateNewID() => id = System.Guid.NewGuid().ToString();
}
