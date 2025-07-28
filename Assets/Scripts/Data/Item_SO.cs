using Sirenix.OdinInspector;
using UnityEngine;

public class Item_SO : ScriptableObject {
    [FoldoutGroup("Item setup")] public string m_itemName;
    [FoldoutGroup("Item setup")] public Sprite m_icon;
    [FoldoutGroup("Item setup")] public GameObject m_collectibleItemPrefab;
    [FoldoutGroup("Item setup")] public GameObject m_onHandItemPrefab;
    [FoldoutGroup("Item setup")] public GameObject m_thirdPersonItemPrefab;
    [FoldoutGroup("Item setup")] [TextArea] public string m_description;
    [FoldoutGroup("Item setup")] public ItemType m_itemType;
    [Space(10)]
    [FoldoutGroup("Item setup")][GUIColor("#FFFF00")][ReadOnly] public string id;

    [FoldoutGroup("Item setup")][Button("Generate New ID")]
    public void GenerateNewID() => id = System.Guid.NewGuid().ToString();    
}
