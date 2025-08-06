using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [Header("Development mode")]
    public bool _developmentMode; //[TODO] Remember to turn off when official building

    [Header("Setup")]
    [SerializeField] private List<Item_SO> m_allGameItems = new List<Item_SO>();
    [SerializeField] private List<BodyPartDamageMultiplier> m_bodyPartDamageMultiplier = new List<BodyPartDamageMultiplier>();
    [SerializeField] private List<Transform> m_spawnPoints = new List<Transform>();

    private static bool initialized = false;

    #region Initialization
    private void Awake() {
        if (initialized) return;
        initialized = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; //[TODO] keep it here?
    }
    #endregion

    #region Get
    public Item_SO GetItemByID(string id) {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].id == id)
                return m_allGameItems[i];
        }
        return null;
    }

    public float GetDamageMultiplier(BodyPart bodyPart) {
        for (int i = 0; i < m_bodyPartDamageMultiplier.Count; i++) {
            if (m_bodyPartDamageMultiplier[i].m_bodyPart == bodyPart)
                return m_bodyPartDamageMultiplier[i].m_damageMultiplier;            
        }
        return 0;
    }

    public Vector3 GetRandomSpawnPos() => m_spawnPoints[UnityEngine.Random.Range(0, m_spawnPoints.Count)].position;

    public List<Item_SO> GetAllItems() => m_allGameItems;
    #endregion
}

#region Interfaces, enums and structs
public interface IInteractable {
    void OnHoverOverItem(bool isOnTarget);
    void Interact(Player_InteractionSystem interactor);
}

public enum ItemType { MeleeWeapon, Firearm, PuzzlePiece, Collectible }

public enum BodyPart { UpperBody, LowerBody, Head, Arm, Leg }

public enum Weapons { Revolver, Shotgun }

[System.Serializable]
public struct CollectableItems {
    public Item_SO m_item;
    public bool m_useActualPositionAndRotation;
    [HideIf("m_useActualPositionAndRotation")] public Vector3 m_position;
    [HideIf("m_useActualPositionAndRotation")] public Quaternion m_rotation;
}

[System.Serializable]
public struct BodyPartDamageMultiplier {
    public BodyPart m_bodyPart;
    public float m_damageMultiplier;
}

public struct MovementAnimationParameters : INetworkSerializable {
    public float m_moveMagnitude;
    public float m_moveX;
    public float m_moveY;
    public bool m_isGrounded;
    public bool m_holdingRevolver;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref m_moveMagnitude);
        serializer.SerializeValue(ref m_moveX);
        serializer.SerializeValue(ref m_moveY);
        serializer.SerializeValue(ref m_isGrounded);
        serializer.SerializeValue(ref m_holdingRevolver);
    }
}

public struct WeaponFirearmData {
    [ReadOnly] public string id;
    public int m_currentAmmo;
    public int m_stockedAmmo;
    public float m_fireRateMultiplier;
    public float m_reloadSpeedMultiplier;
}

public struct MeleeWeaponData {
    [ReadOnly] public string id;
    public float m_attackSpeedMultiplier;
}

[System.Serializable]
public class ItemData {
    public string id;
    public int m_quantity;
    public int index;
}
#endregion