using DelightStudio.Data;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SaveManager : MonoBehaviour {
    public PlayerSaveData PlayerData { get; private set; }

    private static bool initialized;

    #region Initialization
    private void Awake() {
        if (initialized) return;
        initialized = true;

        Singleton.Instance.GameEvents.OnAmmoUpdated.AddListener(OnWeaponAmmoConsumed);
        Singleton.Instance.GameEvents.OnItemCollected.AddListener(OnNewItemAdded);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.AddListener(OnItemRemoved);
        Singleton.Instance.GameEvents.OnItemSplit.AddListener(OnItemSplit);
        Singleton.Instance.GameEvents.OnSensitivityChanged.AddListener(OnSensitivityChanged);
        Singleton.Instance.GameEvents.OnUpdateEnemyFound.AddListener(UpdateEnemyFound);
        Singleton.Instance.GameEvents.OnUpdateBestiaryRead.AddListener(UpdateBestiaryRead);
        Singleton.Instance.GameEvents.OnStatisticUpdated.AddListener(UpdatePlayerStatistics);
        Singleton.Instance.GameEvents.OnNoteDataSaved.AddListener(SaveNotesData);

        LoadData();
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnAmmoUpdated.RemoveListener(OnWeaponAmmoConsumed);
        Singleton.Instance.GameEvents.OnItemCollected.RemoveListener(OnNewItemAdded);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.RemoveListener(OnItemRemoved);
        Singleton.Instance.GameEvents.OnItemSplit.RemoveListener(OnItemSplit);
        Singleton.Instance.GameEvents.OnSensitivityChanged.RemoveListener(OnSensitivityChanged);
        Singleton.Instance.GameEvents.OnUpdateEnemyFound.RemoveListener(UpdateEnemyFound);
        Singleton.Instance.GameEvents.OnUpdateBestiaryRead.RemoveListener(UpdateBestiaryRead);
        Singleton.Instance.GameEvents.OnStatisticUpdated.RemoveListener(UpdatePlayerStatistics);
        Singleton.Instance.GameEvents.OnNoteDataSaved.RemoveListener(SaveNotesData);
    }
    #endregion

    #region Data management
    [Button]
    private void DeletePlayerData() => SaveSystemHandler.DeletePlayerData();

    private void LoadData() {
        PlayerData = SaveSystemHandler.LoadData();
        StartCoroutine(LoadPlayerData());
    }

    private IEnumerator LoadPlayerData() {        
        yield return new WaitForEndOfFrame();
        Singleton.Instance.GameEvents.OnDataLoaded?.Invoke(PlayerData);
    }
    #endregion

    #region Inventory management
    private void OnNewItemAdded(Item_SO item, int index, int itemQuantity, bool isSplitItem = false) {
        if (!isSplitItem) {
            for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
                if (PlayerData.acquiredItems[i].itemBaseId == item.id) {
                    UpdateItemData(i, itemQuantity);
                    return;
                }
            }
        }

        switch (item.m_itemType) {
            case ItemType.MeleeWeapon:
                MeleeWeaponData meleeData = new MeleeWeaponData {
                    m_attackSpeedMultiplier = 1,

                };
                MeleeWeapon meleeWeapon = new MeleeWeapon {
                    itemBaseId = item.id,
                    uniqueId = System.Guid.NewGuid().ToString(),
                    quantity = itemQuantity,
                    index = index,
                    meleeData = meleeData 
                };

                PlayerData.acquiredItems.Add(meleeWeapon);
                PlayerData.acquiredMeleeWeapons.Add(meleeWeapon);
                Singleton.Instance.GameEvents.OnItemSaved?.Invoke(meleeWeapon);
                break;
            case ItemType.Firearm:
                FirearmWeaponData firearmData = new FirearmWeaponData {
                    m_currentAmmo = 0,
                    m_fireRateMultiplier = 1,
                    m_reloadSpeedMultiplier = 1
                };
                LongRangeWeapon longRangeWeapon = new LongRangeWeapon {
                    itemBaseId = item.id,
                    uniqueId = System.Guid.NewGuid().ToString(),
                    quantity = itemQuantity,
                    index = index,
                    firearmData = firearmData 
                };

                PlayerData.acquiredItems.Add(longRangeWeapon);
                PlayerData.acquiredLongRangeWeapons.Add(longRangeWeapon);
                Singleton.Instance.GameEvents.OnItemSaved?.Invoke(longRangeWeapon);
                break;
            default:
                InventoryItemData inventoryItemData = new InventoryItemData {

                };
                Item newItem = new Item {
                    itemBaseId = item.id,
                    uniqueId = System.Guid.NewGuid().ToString(),
                    quantity = itemQuantity,
                    index = index,
                    itemData = inventoryItemData 
                };

                PlayerData.acquiredItems.Add(newItem);
                Singleton.Instance.GameEvents.OnItemSaved?.Invoke(newItem);
                break;
        }
        
        SaveSystemHandler.SaveData(PlayerData);        
    }

    private void OnItemRemoved(ItemData item, int index) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].itemBaseId == item.itemBaseId &&
                PlayerData.acquiredItems[i].uniqueId == item.uniqueId) {
                PlayerData.acquiredItems.RemoveAt(i);
                SaveSystemHandler.SaveData(PlayerData);
                break;
            }
        } 
    }

    private void UpdateItemData(int index, int quantity) {
        PlayerData.acquiredItems[index].quantity += quantity;
        SaveSystemHandler.SaveData(PlayerData);
        Singleton.Instance.GameEvents.OnItemUpdated?.Invoke(PlayerData.acquiredItems[index]);
    }

    private void OnItemSplit(ItemData itemData, int quantity) {
        int originalSplit = quantity / 2;
        int splitResult = quantity - originalSplit;

        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].itemBaseId == 
                itemData.itemBaseId && PlayerData.acquiredItems[i].uniqueId == itemData.uniqueId) {
                PlayerData.acquiredItems[i].quantity = splitResult;
                Singleton.Instance.GameEvents.OnItemUpdated?.Invoke(PlayerData.acquiredItems[i]);
                break;
            }
        }

        int newIndex = Singleton.Instance.InventoryManager.GetEmptySlotIndex(UI_InventoryManager._quickSlots.Count);
        Item_SO item = Singleton.Instance.GameManager.GetItemByID(itemData.itemBaseId);

        OnNewItemAdded(item, newIndex, originalSplit, true);
    }

    private void OnWeaponAmmoConsumed(LongRangeWeapon_SO weapon, int currentAmmo, int stockedAmmo, int remainingAmmo) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].itemBaseId == weapon.m_ammo.id) 
            {
                int amountToRemove = Mathf.Min(PlayerData.acquiredItems[i].quantity, remainingAmmo);

                PlayerData.acquiredItems[i].quantity -= amountToRemove;
                remainingAmmo -= amountToRemove;

                Singleton.Instance.GameEvents.OnItemUpdated?.Invoke(PlayerData.acquiredItems[i]);

                if (PlayerData.acquiredItems[i].quantity <= 0)               
                    OnItemRemoved(PlayerData.acquiredItems[i], PlayerData.acquiredItems[i].index);                
            }
        }

        for (int i = 0; i < PlayerData.acquiredLongRangeWeapons.Count; i++) {
            if (PlayerData.acquiredLongRangeWeapons[i].itemBaseId == weapon.id) {
                PlayerData.acquiredLongRangeWeapons[i].firearmData.m_currentAmmo = currentAmmo;
                break;
            }
        }

        SaveSystemHandler.SaveData(PlayerData);        
    }

    public void OnInventoryItemUpdated(string id, string uniqueId, int newIndex, int newQuantity) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].itemBaseId == id && 
                PlayerData.acquiredItems[i].uniqueId == uniqueId) {
                PlayerData.acquiredItems[i].index = newIndex;
                PlayerData.acquiredItems[i].quantity = newQuantity;

                SaveSystemHandler.SaveData(PlayerData);
                break;
            }
        }
    }

    public void OnItemStackUpdated(ItemData itemDataToRemove, ItemData itemDataToBeUpdated) {
        int quantityToBeAdded = itemDataToRemove.quantity;

        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].itemBaseId == itemDataToBeUpdated.itemBaseId && 
                PlayerData.acquiredItems[i].uniqueId == itemDataToBeUpdated.uniqueId) {
                UpdateItemData(i, quantityToBeAdded);
                break;
            }
        }

        OnItemRemoved(itemDataToRemove, itemDataToRemove.index);
    }
    #endregion

    #region Get
    public ItemData GetItemFromInventory(string id) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++)
            if (PlayerData.acquiredItems[i].itemBaseId == id)
                return PlayerData.acquiredItems[i];
        return null;
    }

    public LongRangeWeapon GetLongRangeWeaponFromInventory(string id) {
        if (PlayerData.acquiredLongRangeWeapons.Count > 0) {
            for (int i = 0; i < PlayerData.acquiredLongRangeWeapons.Count; i++) {
                if (PlayerData.acquiredLongRangeWeapons[i].itemBaseId == id)
                    return PlayerData.acquiredLongRangeWeapons[i];
            }
        }
        return null;
    }

    public MeleeWeapon GetMeleeWeaponFromInventory(string id) {
        if (PlayerData.acquiredMeleeWeapons.Count > 0) {
            for (int i = 0; i < PlayerData.acquiredMeleeWeapons.Count; i++) {
                if (PlayerData.acquiredMeleeWeapons[i].itemBaseId == id)
                    return PlayerData.acquiredMeleeWeapons[i];
            }
        }
        return null;
    }

    public int GetAllItemQuantities(string id) {
        int quantity = 0;
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++)
            if (PlayerData.acquiredItems[i].itemBaseId == id) 
                quantity += PlayerData.acquiredItems[i].quantity;           
        return quantity;
    }

    public List<BestiaryData> GetBestiaryDatas() {
        return PlayerData.allMonstersData;
    }
    #endregion

    #region Settings management
    private void OnSensitivityChanged(float sensitivity) {
        PlayerData.settings.mouseSensitivity = sensitivity;
        OnSettingsChanged(PlayerData.settings);
    }

    private void OnSettingsChanged(Settings settings) {
        PlayerData.settings = settings;
        SaveSystemHandler.SaveData(PlayerData);
    }
    #endregion

    private void UpdateEnemyFound(Enemy_SO enemy) {
        int currentIndex;
        BestiaryData currentData = GetCurrentBeastData(enemy, out currentIndex);
        
        if (currentData == null || currentData.enemyDiscovered)
            return;

        currentData.enemyDiscovered = true;
        PlayerData.allMonstersData[currentIndex] = currentData;

        Singleton.Instance.GameEvents.OnNewEnemyFound?.Invoke(currentData);

        SaveSystemHandler.SaveData(PlayerData);        
    }

    private void UpdateBestiaryRead(Enemy_SO enemy) {
        int currentIndex;
        BestiaryData currentData = GetCurrentBeastData(enemy, out currentIndex);

        if (currentData == null || currentData.notificationRead)
            return;

        currentData.notificationRead = true;
        PlayerData.allMonstersData[currentIndex] = currentData;

        Singleton.Instance.GameEvents.OnBestiaryNotificationRead?.Invoke(currentData);

        SaveSystemHandler.SaveData(PlayerData);        
    }

    private BestiaryData GetCurrentBeastData(Enemy_SO enemy, out int currentIndex) {
        currentIndex = -1;
        for (int i = 0; i < PlayerData.allMonstersData.Count; i++) {
            if (PlayerData.allMonstersData[i].enemyID == enemy.id) {
                currentIndex = i;
                return PlayerData.allMonstersData[i];
            }
        }
        return null;
    }

    private void UpdatePlayerStatistics(Statistic stat) {
        for (int i = 0; i < PlayerData.allGameStatistics.Count; i++) {
            Statistic currentStat = PlayerData.allGameStatistics[i];
            if (currentStat.id == stat.id && currentStat.monsterId == stat.monsterId) {
                PlayerData.allGameStatistics[i] = stat;
                break;
            }
        }

        SaveSystemHandler.SaveData(PlayerData);
    }

    public bool IsMonsterDiscovered(string enemyId) {
        BestiaryData data = PlayerData.allMonstersData.FirstOrDefault(enemy => enemy.enemyID == enemyId);
        return data.enemyDiscovered;
    }

    private void SaveNotesData(NotesSaveData data) { 
        PlayerData.notesData = data;
        SaveSystemHandler.SaveData(PlayerData);
    }
}

#region Data classes
public enum ItemExtraKind : byte {
    None,
    Weapon,
    Melee,
}

[System.Serializable]
public struct FirearmWeaponData : INetworkSerializable {
    public int m_currentAmmo;
    public float m_fireRateMultiplier;
    public float m_reloadSpeedMultiplier;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref m_currentAmmo);
        serializer.SerializeValue(ref m_fireRateMultiplier);
        serializer.SerializeValue(ref m_reloadSpeedMultiplier);
    }
}

[System.Serializable]
public struct MeleeWeaponData : INetworkSerializable {
    public float m_attackSpeedMultiplier;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref m_attackSpeedMultiplier);
    }
}

[System.Serializable]
public class InventoryItemData {
    
}

[System.Serializable]
public class BestiaryData {
    public string enemyID;
    public bool enemyDiscovered;
    public bool notificationRead;
}

[System.Serializable]
public class NotesSaveData {
    public string textureData;
    public string notesData;
}

[System.Serializable]
public class ItemData : INetworkSerializable {
    public string itemBaseId;
    public string uniqueId;
    public ulong ownerId;

    public int quantity;
    public int index;

    public Vector3 initialPos;
    public Quaternion initialRot;

    public ItemExtraKind extraKind;
    public FirearmWeaponData firearm;
    public MeleeWeaponData melee;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref itemBaseId);
        serializer.SerializeValue(ref uniqueId);
        serializer.SerializeValue(ref ownerId);

        serializer.SerializeValue(ref initialPos);
        serializer.SerializeValue(ref initialRot);

        serializer.SerializeValue(ref extraKind);

        switch (extraKind) {
            case ItemExtraKind.Weapon:
                serializer.SerializeValue(ref firearm);
                break;
            case ItemExtraKind.Melee:
                serializer.SerializeValue(ref melee);
                break;
        }
    }
}

[System.Serializable]
public class LongRangeWeapon : ItemData {
    public FirearmWeaponData firearmData;
}

[System.Serializable]
public class MeleeWeapon : ItemData {
    public MeleeWeaponData meleeData;
}

[System.Serializable]
public class Item : ItemData {
    public InventoryItemData itemData;
}

[System.Serializable]
public class Settings {
    public bool firstSetup;

    public int resolutionIndex;
    public int displayModeIndex;
    public int vSyncEnabledIndex;
    public int qualityPresetIndex;
    public int textureQualityIndex;
    public int shadowQualityIndex;
    public int lightningQualityIndex;
    public int antiAliasingModeIndex;
    public float gammaValue;
    public int anisotropicFilteringIndex;
    public int ambientOcclusionIndex;
    public int effectsQualityIndex;
    public int postProcessingQualityIndex;
    public float resolutionScaleValue;
    public int hdrEnabledIndex;
    public int renderDistanceIndex;
    public int fpsLimitValue;

    public int motionBlurEnabled;

    public int colorblindMode;

    public Volume masterVolume;
    public Volume soundEffectsVolume;
    public Volume musicVolume;
    public Volume voiceChatVolume;
    public int outputDeviceIndex;

    public string savedBinds;

    public float mouseSensitivity;
    public int invertAxisIndex;
    public int sprintToggleIndex;

    public Language language;
    public int playerIndicatorMode;
    public int hudSize;
    public int cameraBobEnabled;
    public int damageIndicatorEnabled;
    public int subtitleType;
    public int fontSize;
}

[System.Serializable]
public class Volume {
    public VolumeMixer volumeMixer;
    public float volume;
}
#endregion