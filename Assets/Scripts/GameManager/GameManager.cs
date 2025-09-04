using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private List<Item_SO> m_allGameItems = new List<Item_SO>();
    [SerializeField] private List<BodyPartDamageMultiplier> m_bodyPartDamageMultiplier = new List<BodyPartDamageMultiplier>();
    [SerializeField] private List<Transform> m_spawnPoints = new List<Transform>();

    private static bool initialized = false;

    private static GameState GameState;

    #region Initialization
    private void Awake() {
        if (initialized) return;
        initialized = true;

        Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
        Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.AddListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(OnPlayerLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
        Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.RemoveListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(OnPlayerLoaded);
    }
    #endregion

    private void OnPlayerLoaded() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #region Get
    public Item_SO GetItemByID(string id) {
        for (int i = 0; i < m_allGameItems.Count; i++) {
            if (m_allGameItems[i].id == id)
                return m_allGameItems[i];
        }
        return null;
    }

    public float GetDamageMultiplier(BodyPart bodyPart) {
        for (int i = 0; i < m_bodyPartDamageMultiplier.Count; i++) {
            if (m_bodyPartDamageMultiplier[i].m_bodyPart == bodyPart)
                return m_bodyPartDamageMultiplier[i].m_damageMultiplier;            
        }
        return 0;
    }

    public Vector3 GetRandomSpawnPos() => m_spawnPoints[UnityEngine.Random.Range(0, m_spawnPoints.Count)].position;

    public List<Item_SO> GetAllItems() => m_allGameItems;

    public static GameState GetGameState() => GameState;
    #endregion

    #region Set
    private void OnGameResumed() => GameState = GameState.Resumed;

    private void OnGamePaused() => GameState = GameState.Paused;

    private void OnInventoryOpened() => GameState = GameState.InventoryOpened;    
    #endregion
}

#region Interfaces, enums and structs
public interface IInteractable {
    void OnHoverOverItem(bool isOnTarget);
    void Interact(Player_InteractionSystem interactor);
}

public enum Weapons { None, Revolver, Shotgun }

public enum Language { English, Portuguese, Spanish }

public enum ItemType { MeleeWeapon, Firearm, PuzzlePiece, Collectible, Ammo }

public enum BodyPart { UpperBody, LowerBody, Head, Arm, Leg }

public enum GameState { Resumed, Paused, InventoryOpened }

public enum VolumeMixer { Master, Music, SFX, VoiceChat }

public enum Quality { Low, Medium, High, Ultra }

public enum Size { Small, Normal, Big }

[System.Serializable]
public struct CollectableItems {
    public Item_SO m_item;
    public bool m_useActualPositionAndRotation;
    [HideIf(nameof(m_useActualPositionAndRotation))] public Vector3 m_position;
    [HideIf(nameof(m_useActualPositionAndRotation))] public Quaternion m_rotation;
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref m_moveMagnitude);
        serializer.SerializeValue(ref m_moveX);
        serializer.SerializeValue(ref m_moveY);
        serializer.SerializeValue(ref m_isGrounded);
    }
}

public struct UIOption {
    public string text;
    public int value;
}

[System.Serializable]
public struct UISliderOption {
    public Slider slider;
    public TMP_Text text;
}
#endregion