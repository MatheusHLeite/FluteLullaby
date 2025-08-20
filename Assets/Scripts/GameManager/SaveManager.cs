using Sirenix.OdinInspector;
using Steamworks;
using System.Collections;
using UnityEngine;

public class SaveManager : MonoBehaviour {
    public PlayerSaveData PlayerData { get; private set; }

    private static bool initialized;

    #region Initialization
    private void Awake() {
        if (initialized) return;
        initialized = true;

        Singleton.Instance.GameEvents.OnAmmoUpdated.AddListener(OnWeaponAmmoConsumed);
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(LoadData);
        Singleton.Instance.GameEvents.OnItemCollected.AddListener(OnNewItemAdded);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.AddListener(OnItemRemoved);
        Singleton.Instance.GameEvents.OnItemSplit.AddListener(OnItemSplit);
        Singleton.Instance.GameEvents.OnSensitivityChange.AddListener(OnSensitivityChanged);

        StartCoroutine(LoadPlayerData());
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnAmmoUpdated.RemoveListener(OnWeaponAmmoConsumed);
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(LoadData);
        Singleton.Instance.GameEvents.OnItemCollected.RemoveListener(OnNewItemAdded);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.RemoveListener(OnItemRemoved);
        Singleton.Instance.GameEvents.OnItemSplit.RemoveListener(OnItemSplit);
        Singleton.Instance.GameEvents.OnSensitivityChange.RemoveListener(OnSensitivityChanged);
    }
    #endregion

    #region Data management
    [Button]
    private void DeletePlayerData() => SaveSystemHandler.DeletePlayerData();

    private IEnumerator LoadPlayerData() {
        yield return new WaitUntil(() => SteamClient.IsValid || !GameNetworkManager.IsSteam);
        LoadData();
    }

    private void LoadData() {
        PlayerData = SaveSystemHandler.LoadData();
        Singleton.Instance.GameEvents.OnDataLoaded?.Invoke(PlayerData); 
    }
    #endregion

    #region Inventory management
    private void OnNewItemAdded(Item_SO item, int index, int itemQuantity, bool isSplitItem = false) {
        if (!isSplitItem) {
            for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
                if (PlayerData.acquiredItems[i].id == item.id) {
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
                    id = item.id,
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
                    id = item.id,
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
                    id = item.id,
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
            if (PlayerData.acquiredItems[i].id == item.id &&
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
            if (PlayerData.acquiredItems[i].id == itemData.id && PlayerData.acquiredItems[i].uniqueId == itemData.uniqueId) {
                PlayerData.acquiredItems[i].quantity = splitResult;
                Singleton.Instance.GameEvents.OnItemUpdated?.Invoke(PlayerData.acquiredItems[i]);
                break;
            }
        }

        int newIndex = Singleton.Instance.InventoryManager.GetEmptySlotIndex(UI_InventoryManager._quickSlots.Count);
        Item_SO item = Singleton.Instance.GameManager.GetItemByID(itemData.id);

        OnNewItemAdded(item, newIndex, originalSplit, true);
    }

    private void OnWeaponAmmoConsumed(LongRangeWeapon_SO weapon, int currentAmmo, int stockedAmmo, int remainingAmmo) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].id == weapon.m_ammo.id) 
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
            if (PlayerData.acquiredLongRangeWeapons[i].id == weapon.id) {
                PlayerData.acquiredLongRangeWeapons[i].firearmData.m_currentAmmo = currentAmmo;
                break;
            }
        }

        SaveSystemHandler.SaveData(PlayerData);        
    }

    public void OnInventoryItemUpdated(string id, string uniqueId, int newIndex, int newQuantity) {
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++) {
            if (PlayerData.acquiredItems[i].id == id && 
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
            if (PlayerData.acquiredItems[i].id == itemDataToBeUpdated.id && 
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
            if (PlayerData.acquiredItems[i].id == id)
                return PlayerData.acquiredItems[i];
        return null;
    }

    public LongRangeWeapon GetLongRangeWeaponFromInventory(string id) {
        if (PlayerData.acquiredLongRangeWeapons.Count > 0) {
            for (int i = 0; i < PlayerData.acquiredLongRangeWeapons.Count; i++) {
                if (PlayerData.acquiredLongRangeWeapons[i].id == id)
                    return PlayerData.acquiredLongRangeWeapons[i];
            }
        }
        return null;
    }

    public MeleeWeapon GetMeleeWeaponFromInventory(string id) {
        if (PlayerData.acquiredMeleeWeapons.Count > 0) {
            for (int i = 0; i < PlayerData.acquiredMeleeWeapons.Count; i++) {
                if (PlayerData.acquiredMeleeWeapons[i].id == id)
                    return PlayerData.acquiredMeleeWeapons[i];
            }
        }
        return null;
    }

    public int GetAllItemQuantities(string id) {
        int quantity = 0;
        for (int i = 0; i < PlayerData.acquiredItems.Count; i++)
            if (PlayerData.acquiredItems[i].id == id) 
                quantity += PlayerData.acquiredItems[i].quantity;           
        return quantity;
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
}

#region Data classes
[System.Serializable]
public class FirearmWeaponData {
    public int m_currentAmmo;
    public float m_fireRateMultiplier;
    public float m_reloadSpeedMultiplier;
}

[System.Serializable]
public class MeleeWeaponData {    
    public float m_attackSpeedMultiplier;
}

[System.Serializable]
public class InventoryItemData {
    
}

[System.Serializable]
public class ItemData {
    public string id;
    public string uniqueId;
    public int quantity;
    public int index;
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
    public int refreshRateIndex;
    public int vSyncEnabledIndex;
    public int qualityPresetIndex;
    public int textureQualityIndex;
    public int shadowQualityIndex;
    public int antiAliasingModeIndex;
    public float gammaValue;
    public int anisotropicFilteringIndex;
    public int ambientOcclusionIndex;
    public int effectsQualityIndex;
    public float resolutionScaleValue;
    public int hdrEnabledIndex;
    public int fpsLimitValue;

    public Volume masterVolume;
    public Volume soundEffectsVolume;
    public Volume musicVolume;
    public Volume voiceChatVolume;
    public int outputDeviceIndex;

    public float mouseSensitivity;
}

public class Volume {
    public VolumeMixer volumeMixer;
    public float volume;
}
#endregion