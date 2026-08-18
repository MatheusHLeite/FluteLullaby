using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

public static class SaveSystemHandler {
    private const string SaveFileName = "data.json";
    private static string LocalSavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    #region Save
    public static void SaveData(PlayerSaveData data) {
        string json = JsonUtility.ToJson(data);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (GameNetworkManager.IsSteam) {
            bool success = SteamRemoteStorage.FileWrite(SaveFileName, bytes);

            if (success) {
                Debug.Log("<color=green>Player data saved to Steam Cloud.</color>");
                return;
            }

            Debug.LogError("<color=red>Steam Cloud save failed! Falling back to local save.</color>");
            return;
        }

        SaveLocal(json);
    }

    private static void SaveLocal(string json) {
        try {
            File.WriteAllText(LocalSavePath, json);

            Debug.Log($"<color=green>Player data saved locally.</color>\nPath: {LocalSavePath}");
        }
        catch (System.Exception e) {
            Debug.LogError($"<color=red>Local save failed!</color>\n{e}");
        }
    }
    #endregion

    #region Load
    public static PlayerSaveData LoadData() {
        if (GameNetworkManager.IsSteam) {
            if (SteamRemoteStorage.FileExists(SaveFileName)) {
                byte[] bytes = SteamRemoteStorage.FileRead(SaveFileName);

                if (bytes != null && bytes.Length > 0) {
                    string json = Encoding.UTF8.GetString(bytes);
                    Debug.Log("<color=green>Player data loaded from Steam Cloud.</color>");

                    return JsonUtility.FromJson<PlayerSaveData>(json);
                }

                Debug.LogWarning("Steam Cloud save exists but could not be read.");
            }

            if (File.Exists(LocalSavePath)) {
                Debug.Log("<color=yellow>No Steam Cloud save found. Loading local save.</color>");
                return LoadLocal();
            }

            return CreateDefaultSave();
        }

        if (File.Exists(LocalSavePath))        
            return LoadLocal();        

        return CreateDefaultSave();
    }

    private static PlayerSaveData LoadLocal() {
        try {
            string json = File.ReadAllText(LocalSavePath);
            Debug.Log("<color=green>Player data loaded from local save.</color>");

            return JsonUtility.FromJson<PlayerSaveData>(json);
        }
        catch (System.Exception e) {
            Debug.LogError($"<color=red>Local save load failed!</color>\n{e}");
            return PlayerSaveData.Default();
        }
    }

    private static PlayerSaveData CreateDefaultSave() {
        PlayerSaveData data = PlayerSaveData.Default();

        Debug.Log("<color=yellow>Creating new player data!</color>");

        SaveData(data);
        return data;
    }
    #endregion

    #region Delete
    public static bool DeletePlayerData() {
        bool deleted = false;

        if (GameNetworkManager.IsSteam) {
            if (SteamRemoteStorage.FileExists(SaveFileName)) {
                deleted = SteamRemoteStorage.FileDelete(SaveFileName);

                if (deleted)
                    Debug.Log("<color=green>Steam Cloud data deleted.</color>");
                else
                    Debug.LogError("<color=red>Steam Cloud data delete failed.</color>");
            }
        }

        if (File.Exists(LocalSavePath)) {
            try {
                File.Delete(LocalSavePath);
                deleted = true;

                Debug.Log("<color=green>Local save data deleted.</color>");
            }
            catch (System.Exception e) {
                Debug.LogError($"<color=red>Local save delete failed!</color>\n{e}");
            }     
        }

        if (!deleted)        
            Debug.Log("No save file found.");        

        return deleted;
    }
    #endregion
}

[System.Serializable]
public class PlayerSaveData {
    public Settings settings;
    public List<ItemData> acquiredItems;
    public List<LongRangeWeapon> acquiredLongRangeWeapons;
    public List<MeleeWeapon> acquiredMeleeWeapons;
    public List<BestiaryData> allMonstersData;
    public List<Statistic> allGameStatistics;
    public NotesSaveData notesData;

    public static PlayerSaveData Default() {
        return new PlayerSaveData {
            settings = new Settings() {
                firstSetup = true,
                mouseSensitivity = 3f,
                invertAxisIndex = 0,
                sprintToggleIndex = 0,

                resolutionIndex = -1, //System handle this
                displayModeIndex = 1,
                vSyncEnabledIndex = 0,

                qualityPresetIndex = -1,
                textureQualityIndex = 3,
                shadowQualityIndex = 3,
                lightningQualityIndex = 3,
                effectsQualityIndex = 3,
                postProcessingQualityIndex = 3,
                anisotropicFilteringIndex = 0,
                antiAliasingModeIndex = 2,
                ambientOcclusionIndex = 0,
                renderDistanceIndex = 2,

                motionBlurEnabled = 0,
                colorblindMode = 0,

                fpsLimitValue = -1,
                resolutionScaleValue = 1,
                gammaValue = 0.5f,
                hdrEnabledIndex = 0,

                masterVolume = new Volume { volume = 1f, volumeMixer = VolumeMixer.Master },
                soundEffectsVolume = new Volume { volume = .7f, volumeMixer = VolumeMixer.SFX },
                musicVolume = new Volume { volume = .35f, volumeMixer = VolumeMixer.Music },
                voiceChatVolume = new Volume { volume = .6f, volumeMixer = VolumeMixer.VoiceChat },
                outputDeviceIndex = 0,

                language = Language.English, //System handle this
                playerIndicatorMode = 0,
                hudSize = 1,
                cameraBobEnabled = 1,
                damageIndicatorEnabled = 0,
                subtitleType = 0,
                fontSize = 0,

                savedBinds = string.Empty
            },
            acquiredItems = new List<ItemData>(),
            acquiredLongRangeWeapons = new List<LongRangeWeapon>(),
            acquiredMeleeWeapons = new List<MeleeWeapon>(),
            allMonstersData = Singleton.Instance.EnemiesManager.DefaultData(),
            allGameStatistics = Singleton.Instance.StatisticsManager.GetAllGameStatistics().ToList(),
            notesData = new NotesSaveData(),
        };
    }
}