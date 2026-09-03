using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player_Manager : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private PlayerParameters_SO m_playerParameters;
    [SerializeField] private Camera m_playerCamera;

    private Player_AnimationSystem playerAnimationSystem;
    private Player_AudioSystem playerAudioSystem;
    private Player_CameraMovementSystem PlayerCameraMovementSystem;
    private Player_CombatSystem playerCombatSystem;
    private Player_HealthSystem PlayerHealthSystem;
    private Player_InputHandler playerInputHandler;
    private Player_InteractionSystem playerInteractionSystem;
    private Player_InventorySystem playerInventorySystem;
    private Player_MovementSystem PlayerMovementSystem;
    private Player_PauseHandler playerPauseHandler;
    private Player_VisualManagementSystem playerVisualManagementSystem;
    private Player_VoiceChat playerVoiceChat;

    private void Awake() {
        playerAnimationSystem = GetComponent<Player_AnimationSystem>();
        playerAudioSystem = GetComponent<Player_AudioSystem>();
        PlayerCameraMovementSystem = GetComponent<Player_CameraMovementSystem>();
        playerCombatSystem = GetComponent<Player_CombatSystem>();
        PlayerHealthSystem = GetComponent<Player_HealthSystem>();
        playerInputHandler = GetComponent<Player_InputHandler>();
        playerInteractionSystem = GetComponent<Player_InteractionSystem>();
        playerInventorySystem = GetComponent<Player_InventorySystem>();
        PlayerMovementSystem = GetComponent<Player_MovementSystem>();
        playerPauseHandler = GetComponent<Player_PauseHandler>();
        playerVisualManagementSystem = GetComponent<Player_VisualManagementSystem>();
        playerVoiceChat = GetComponent<Player_VoiceChat>();
    }

    public override void OnNetworkSpawn() => InitializeComponents();

    public override void OnNetworkDespawn() => DeinitializeComponents();

    public Camera GetPlayerCamera() => m_playerCamera;

    private void InitializeComponents() {
        bool isOwner = IsOwner;

        playerAnimationSystem.InitializeNetwork(isOwner);
        //playerAudioSystem.InitializeNetwork(isOwner);
        PlayerCameraMovementSystem.InitializeNetwork(isOwner);
        playerCombatSystem.InitializeNetwork(isOwner);
        PlayerHealthSystem.InitializeNetwork(isOwner);
        //playerInputHandler.InitializeNetwork(isOwner);
        playerInteractionSystem.InitializeNetwork(isOwner);
        playerInventorySystem.InitializeNetwork(isOwner);
        PlayerMovementSystem.InitializeNetwork(isOwner);
        playerPauseHandler.InitializeNetwork(isOwner);
        playerVisualManagementSystem.InitializeNetwork(isOwner);
        playerVoiceChat.InitializeNetwork(isOwner);

        if (!isOwner) return;

        StartCoroutine(LoadPlayer());
    }

    private void DeinitializeComponents() {
        bool isOwner = IsOwner;

        playerAnimationSystem.DeinitializeNetwork(isOwner);
        PlayerCameraMovementSystem.DeinitializeNetwork(isOwner);
        playerCombatSystem.DeinitializeNetwork(isOwner);
        PlayerHealthSystem.DeinitializeNetwork(isOwner);
        playerInteractionSystem.DeinitializeNetwork(isOwner);
        playerInventorySystem.DeinitializeNetwork(isOwner);
        PlayerMovementSystem.DeinitializeNetwork(isOwner);
        playerVisualManagementSystem.DeinitializeNetwork(isOwner);
        playerVoiceChat.DeinitializeNetwork(isOwner);
    }

    private IEnumerator LoadPlayer() {       
        PlayerCameraMovementSystem.SetPlayerParameters(m_playerParameters);
        PlayerMovementSystem.SetPlayerParameters(m_playerParameters);
        PlayerHealthSystem.SetPlayerParameters(m_playerParameters);

        yield return new WaitForEndOfFrame();

        Singleton.Instance.GameEvents.OnPlayerLoaded?.Invoke(this);
    }

    private void Update() {
        bool isOwner = IsOwner;

        playerAnimationSystem.Tick(isOwner);
        PlayerCameraMovementSystem.Tick(isOwner);
        playerCombatSystem.Tick(isOwner);
        playerInputHandler.Tick(isOwner);
        playerInteractionSystem.Tick(isOwner);
        PlayerMovementSystem.Tick(isOwner);
        playerPauseHandler.Tick(isOwner);
        playerVisualManagementSystem.Tick(isOwner);
    }

    private void FixedUpdate() {
        bool isOwner = IsOwner;

        PlayerCameraMovementSystem.FixedTick(isOwner);
        PlayerMovementSystem.FixedTick(isOwner);
    }

    private void LateUpdate() {
        bool isOwner = IsOwner;

        playerInputHandler.LateTick(isOwner);
    }
}