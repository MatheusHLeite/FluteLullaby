using Sirenix.OdinInspector;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour {
    public PlayerSaveData PlayerData { get; private set; }

    private List<WeaponFirearmData> m_weaponData = new List<WeaponFirearmData>();
    private List<ItemData> m_allItemsData = new List<ItemData>();
    private List<Item_SO> m_allGameItems;

    private static bool initialized;

    #region Initialization
    private void Awake() {
        if (initialized) return;
        initialized = true;

        Singleton.Instance.GameEvents.OnAmmoConsumed.AddListener(UpdateWeaponDataByID);
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(LoadData);
        Singleton.Instance.GameEvents.OnSlotItemCollected.AddListener(OnNewWeaponAdded);
        Singleton.Instance.GameEvents.OnDataLoaded.AddListener(OnDataLoaded);

        m_allGameItems = GetComponent<GameManager>().GetAllItems();

        StartCoroutine(LoadPlayerData());
    }

    private void OnDestroy() {
        m_weaponData.Clear();

        Singleton.Instance.GameEvents.OnAmmoConsumed.RemoveListener(UpdateWeaponDataByID);
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(LoadData);
        Singleton.Instance.GameEvents.OnSlotItemCollected.RemoveListener(OnNewWeaponAdded);
        Singleton.Instance.GameEvents.OnDataLoaded.RemoveListener(OnDataLoaded);
    }
    #endregion

    #region Data management
    [Button]
    private void DeleteSaveData() => SaveSystemHandler.DeletePlayerData();

    private IEnumerator LoadPlayerData() {
        yield return new WaitUntil(() => SteamClient.IsValid);
        LoadData();
    }

    private void LoadData() {
        SetupWeaponData();
        PlayerSaveData data = SaveSystemHandler.LoadData();
        Singleton.Instance.GameEvents.OnDataLoaded?.Invoke(data);
    }

    private void OnDataLoaded(PlayerSaveData data) => PlayerData = data;
    #endregion

    #region Inventory management
    private void OnNewWeaponAdded(Item_SO item, int index) {
        ItemData newEntry = new ItemData {
            id = item.id,
            m_quantity = 0,
            index = index
        };

        PlayerData.acquiredWeapons.Add(newEntry);
        SaveSystemHandler.SaveData(PlayerData);
    }
    #endregion

    public WeaponFirearmData GetWeaponDataByID(string id) {
        WeaponFirearmData data = new WeaponFirearmData();
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

    private void SetupWeaponData() {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].m_itemType == ItemType.Firearm) {
                WeaponFirearmData data = new WeaponFirearmData
                {
                    id = m_allGameItems[i].id,
                    m_currentAmmo = PlayerPrefs.GetInt($"currentAmmo_weaponData_{m_allGameItems[i].id}"),
                    m_stockedAmmo = PlayerPrefs.GetInt($"stockedAmmo_weaponData_{m_allGameItems[i].id}")
                };//[TODO] Load from a JSON file
                m_weaponData.Add(data);
            }

            ItemData itemData = new ItemData
            {
                id = m_allGameItems[i].id,
                m_quantity = PlayerPrefs.GetInt($"itemData_quantity_{m_allGameItems[i].id}"),
                index = PlayerPrefs.GetInt($"itemData_inventoryIndex_{m_allGameItems[i].id}")
            };//[TODO] Load from a JSON file
            m_allItemsData.Add(itemData);
        }
    }

    private void UpdateWeaponDataByID(string id, int currentAmmo, int stockedAmmo)
    {
        for (int i = 0; i < m_weaponData.Count; i++)
        {
            if (m_weaponData[i].id == id)
            {
                WeaponFirearmData data = m_weaponData[i];

                data.m_currentAmmo = currentAmmo;
                data.m_stockedAmmo = stockedAmmo;

                PlayerPrefs.SetInt($"currentAmmo_weaponData_{id}", currentAmmo); //[TODO] Set to a JSON file
                PlayerPrefs.SetInt($"stockedAmmo_weaponData_{id}", stockedAmmo);

                m_weaponData[i] = data;
                break;
            }
        }
    }

    public void SaveItemData(string id, int newIndex, int newQuantity)
    {
        for (int i = 0; i < m_allItemsData.Count; i++)
        {
            if (m_allItemsData[i].id == id)
            {
                ItemData data = m_allItemsData[i];

                data.m_quantity = newQuantity;
                data.index = newIndex;

                PlayerPrefs.SetInt($"itemData_quantity_{id}", newQuantity); //[TODO] Set to a JSON file
                PlayerPrefs.SetInt($"itemData_inventoryIndex_{id}", newIndex);

                m_allItemsData[i] = data;
                break;
            }
        }
    }

    public ItemData GetItemData(string id)
    {
        ItemData data = new ItemData();
        for (int i = 0; i < m_allItemsData.Count; i++)
        {
            if (m_allItemsData[i].id == id)
            {
                data = m_allItemsData[i];
                data.m_quantity = PlayerPrefs.GetInt($"itemData_quantity_{id}");
                data.index = PlayerPrefs.GetInt($"itemData_inventoryIndex_{id}"); //[TODO] Add to the JSON
                break;
            }
        }
        return data;
    }
}