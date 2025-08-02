using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public static class SaveSystemHandler {
    private const string SaveFileName = "PlayerData.json";

    public static void SaveData(PlayerSaveData data) {
        string json = JsonUtility.ToJson(data);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        SteamRemoteStorage.FileWrite(SaveFileName, bytes);

        Debug.Log("<color=green>Player data saved</color>");
    }

    public static PlayerSaveData LoadData() {
        try {
            if (SteamRemoteStorage.FileExists(SaveFileName)) {
                byte[] bytes = SteamRemoteStorage.FileRead(SaveFileName);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("<color=yellow>Player data loaded</color>");
                return JsonUtility.FromJson<PlayerSaveData>(json);
            }

            Debug.Log("<color=yellow>Player data loaded</color>");
            return PlayerSaveData.Default();
        }
        catch (System.Exception e) {
            Debug.LogError("Saving error: " + e);
            return PlayerSaveData.Default();
        }
    }
}

[System.Serializable]
public struct PlayerSaveData {
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
            acquiredWeapons = new List<WeaponEntry> { }
        };
    }
}