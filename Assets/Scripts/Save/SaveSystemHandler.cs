using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public static class SaveSystemHandler {
    private const string SaveFileName = "PlayerData.json";

    public static void SaveData(PlayerSaveData data) {
        string json = JsonUtility.ToJson(data);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        bool success = SteamRemoteStorage.FileWrite(SaveFileName, bytes);

        if (!success)  {
            Debug.LogError("<color=red>Steam Cloud save failed!</color>");
            return;
        }
        Debug.Log("<color=green>Player data saved</color>");
    }

    public static PlayerSaveData LoadData() {
        if (SteamRemoteStorage.FileExists(SaveFileName)) {
            byte[] bytes = SteamRemoteStorage.FileRead(SaveFileName);
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("<color=green>Data loaded!</color>");
            return JsonUtility.FromJson<PlayerSaveData>(json);
        }

        PlayerSaveData data = PlayerSaveData.Default();        
        Debug.Log("<color=yellow>Creating new player data!</color>");
        SaveData(data);
        return data;
    }

    public static bool DeletePlayerData() {
        if (SteamRemoteStorage.FileExists(SaveFileName)) {
            bool success = SteamRemoteStorage.FileDelete(SaveFileName);
            if (!success) {
                Debug.LogError("<color=red>Steam Cloud data delete fail</color>");
            }

            Debug.Log("<color=green>Steam Cloud data deleted</color>");
            return success;
        }
        else  {
            Debug.Log("No save file!");
            return false;
        }
    }
}

[System.Serializable]
public class PlayerSaveData {
    public float masterVolume;
    public float musicVolume;
    public float soundEffectsVolume;
    public float mouseSensitivity;
    public List<WeaponEntry> acquiredWeapons;

    public static PlayerSaveData Default() {
        return new PlayerSaveData {
            masterVolume = 1f,
            musicVolume = 0.6f,
            soundEffectsVolume = 0.785f,
            mouseSensitivity = 2f,
            acquiredWeapons = new List<WeaponEntry>()
        };
    }
}