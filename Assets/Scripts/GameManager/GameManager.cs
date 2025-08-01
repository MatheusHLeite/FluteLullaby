using Sirenix.OdinInspector;
using Steamworks;
using System.Collections;
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

    private List<WeaponData> m_weaponData = new List<WeaponData>();

    private void Awake() {
        Singleton.Instance.GameEvents.OnAmmoConsumed.AddListener(UpdateWeaponDataByID); //[TODO] should I set as IsOwner?

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; //[TODO] keep it here?

        SetupWeaponData();
        StartCoroutine(LoadPlayerData());
    }

    private void OnDestroy(){
        m_weaponData.Clear();

        Singleton.Instance.GameEvents.OnAmmoConsumed.RemoveListener(UpdateWeaponDataByID);
    }

    private IEnumerator LoadPlayerData() {
        yield return new WaitUntil(() => SteamServer.IsValid);
        //var save = SaveSystemHandler.Load();
    }

    private void SetupWeaponData() {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].m_itemType == ItemType.Firearm) {
                WeaponData data = new WeaponData {
                    id = m_allGameItems[i].id,
                    m_currentAmmo = PlayerPrefs.GetInt($"currentAmmo_weaponData_{m_allGameItems[i].id}"), 
                    m_stockedAmmo = PlayerPrefs.GetInt($"stockedAmmo_weaponData_{m_allGameItems[i].id}")
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

                data.m_currentAmmo = currentAmmo;
                data.m_stockedAmmo = stockedAmmo;

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

                data.m_currentAmmo = PlayerPrefs.GetInt($"currentAmmo_weaponData_{id}"); //[TODO] Get from a JSON file
                data.m_stockedAmmo = PlayerPrefs.GetInt($"stockedAmmo_weaponData_{id}");
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

public struct WeaponData {
    [ReadOnly] public string id;
    public int m_currentAmmo;
    public int m_stockedAmmo;
    public float m_fireRateMultiplier;
    public float m_reloadSpeedMultiplier;
}

public struct MeleeWeaponData {
    [ReadOnly] public string id;
    
}