using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [Header("Development mode")]
    public bool _developmentMode; //[TODO] Remember to turn off when official building

    [Header("Setup")]
    [SerializeField] private List<Item_SO> m_allGameItems = new List<Item_SO>();
    [SerializeField] private List<BodyPartDamageMultiplier> m_bodyPartDamageMultiplier = new List<BodyPartDamageMultiplier>();
    [SerializeField] private List<Transform> m_spawnPoints = new List<Transform>();
    private List<WeaponData> m_weaponData = new List<WeaponData>();

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupWeaponData();

        Singleton.Instance.GameEvents.OnAmmoConsumed.AddListener(UpdateWeaponDataByID);
    }

    private void OnDestroy(){
        m_weaponData.Clear();

        Singleton.Instance.GameEvents.OnAmmoConsumed.RemoveListener(UpdateWeaponDataByID);
    }

    private void SetupWeaponData() {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].m_itemType == ItemType.Firearm) {
                WeaponData data = new WeaponData {
                    id = m_allGameItems[i].id,
                    currentAmmo = PlayerPrefs.GetInt($"currentAmmo_weaponData_{m_allGameItems[i].id}"), 
                    stockedAmmo = PlayerPrefs.GetInt($"stockedAmmo_weaponData_{m_allGameItems[i].id}")
                };//[TODO] Load from a JSON file
                m_weaponData.Add(data);
            }                          
        }   
    }

    public Item_SO GetItemByID(string id) {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].id == id)            
                return m_allGameItems[i];            
        }
        return null;
    }

    private void UpdateWeaponDataByID(string id, int currentAmmo, int stockedAmmo) {
        for (int i = 0; i < m_weaponData.Count; i++) {
            if (m_weaponData[i].id == id) {
                WeaponData data = m_weaponData[i];

                data.currentAmmo = currentAmmo;
                data.stockedAmmo = stockedAmmo;

                PlayerPrefs.SetInt($"currentAmmo_weaponData_{id}", currentAmmo); //[TODO] Set to a JSON file
                PlayerPrefs.SetInt($"stockedAmmo_weaponData_{id}", stockedAmmo);

                m_weaponData[i] = data;
                break;
            }
        }
    }

    public WeaponData GetWeaponDataByID(string id) {
        WeaponData data = new WeaponData();
        for (int i = 0; i < m_weaponData.Count; i++) {
            if (m_weaponData[i].id == id) {
                data = m_weaponData[i];

                data.currentAmmo = PlayerPrefs.GetInt($"currentAmmo_weaponData_{id}"); //[TODO] Get from a JSON file
                data.stockedAmmo = PlayerPrefs.GetInt($"stockedAmmo_weaponData_{id}");
                break;
            }
        } 
        return data; 
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

public struct WeaponData {
    [ReadOnly] public string id;
    public int currentAmmo;
    public int stockedAmmo;
}