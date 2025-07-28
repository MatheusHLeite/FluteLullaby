using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private List<Item_SO> m_allGameItems = new List<Item_SO>();
    [SerializeField] private List<BodyPartDamageMultiplier> m_bodyPartDamageMultiplier = new List<BodyPartDamageMultiplier>();
    [SerializeField] private List<Transform> m_spawnPoints = new List<Transform>();

    /*public static readonly string[] AllTags = {
        "Player",
        "Enemy",
        "Collectible",
        "Obstacle"
    };*/

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public Item_SO GetItemByID(string id) {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].id == id)            
                return m_allGameItems[i];            
        }
        return null;
    }

    public float GetDamageMultiplier(BodyPart bodyPart) {
        for (int i = 0; i < m_bodyPartDamageMultiplier.Count; i++) {
            if (m_bodyPartDamageMultiplier[i].m_bodyPart == bodyPart) {
                return m_bodyPartDamageMultiplier[i].m_damageMultiplier;
            }
        }
        return 0;
    }

    public Vector3 GetRandomSpawnPos() {
        return m_spawnPoints[UnityEngine.Random.Range(0, m_spawnPoints.Count)].position;
    }
}

public interface IInteractable {
    void OnHoverOverItem(bool isOnTarget);
    void Interact(Player_InteractionSystem interactor);
}

[Serializable]
public struct CollectableItems {
    public Item_SO m_item;
    public bool m_useActualPositionAndRotation;
    [HideIf("m_useActualPositionAndRotation")] public Vector3 m_position;
    [HideIf("m_useActualPositionAndRotation")] public Quaternion m_rotation;
}

[Serializable]
public struct BodyPartDamageMultiplier {
    public BodyPart m_bodyPart;
    public float m_damageMultiplier;
}

public enum ItemType { MeleeWeapon, Firearm, PuzzlePiece, Collectible }

public enum Tags { Player, Enemy }

public enum BodyPart { UpperBody, LowerBody, Head, Arm, Leg }