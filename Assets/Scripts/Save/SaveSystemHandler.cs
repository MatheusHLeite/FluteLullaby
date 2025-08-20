using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public static class SaveSystemHandler {
    private const string SaveFileName = "PlayerData.json";

    public static void SaveData(PlayerSaveData data) {
        if (!GameNetworkManager.IsSteam) {
            Debug.LogWarning("Steam not connected! Saving will not be able");
            return;
        }

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
        if (!GameNetworkManager.IsSteam) {
            Debug.LogWarning("Steam not connected! Loading won't be able");
            return PlayerSaveData.Default();
        }

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
        if (!GameNetworkManager.IsSteam) {
            Debug.LogWarning("Steam not connected! Cannot delete save file");
            return false;
        }

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
    public Settings settings;
    public List<ItemData> acquiredItems;
    public List<LongRangeWeapon> acquiredLongRangeWeapons;
    public List<MeleeWeapon> acquiredMeleeWeapons;

    public static PlayerSaveData Default() {
        return new PlayerSaveData {
            settings = new Settings() {
                firstSetup = true,
                mouseSensitivity = 2f,
                resolutionIndex = -1,
                refreshRateIndex = -1,
                displayModeIndex = 1,

                qualityPresetIndex = 3,
                textureQualityIndex = 3,
                shadowQualityIndex = 2,
                antiAliasingModeIndex = 2,
                ambientOcclusionIndex = 2,
                effectsQualityIndex = 2,
                anisotropicFilteringIndex = 1,
                hdrEnabledIndex = 0,

                resolutionScaleValue = 1,
                gammaValue = 0.5f,
                vSyncEnabledIndex = 0,

                fpsLimitValue = -1,
                masterVolume = new Volume { volume = 1f, volumeMixer = VolumeMixer.Master },
                soundEffectsVolume = new Volume { volume = .7f, volumeMixer = VolumeMixer.SFX },
                musicVolume = new Volume { volume = .35f, volumeMixer = VolumeMixer.Music },
                voiceChatVolume = new Volume { volume = .6f, volumeMixer = VolumeMixer.VoiceChat },
                outputDeviceIndex = 0,
            },        
            acquiredItems = new List<ItemData>(),
            acquiredLongRangeWeapons = new List<LongRangeWeapon>(),
            acquiredMeleeWeapons = new List<MeleeWeapon>(),            
        };
    }
}