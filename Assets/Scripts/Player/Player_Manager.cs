using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player_Manager : NetworkBehaviour {
    [SerializeField] private PlayerParameters_SO m_playerParameters;
    [SerializeField] private Camera m_playerCamera;

    private Player_CameraMovementSystem PlayerCameraMovementSystem;
    private Player_MovementSystem PlayerMovementSystem;
    private Player_HealthSystem PlayerHealthSystem;

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            StartCoroutine(LoadPlayer());
        }
    }

    private IEnumerator LoadPlayer() {
        yield return new WaitForEndOfFrame();

        PlayerCameraMovementSystem = GetComponent<Player_CameraMovementSystem>();
        PlayerMovementSystem = GetComponent<Player_MovementSystem>();
        PlayerHealthSystem = GetComponent<Player_HealthSystem>();

        PlayerCameraMovementSystem.SetPlayerParameters(m_playerParameters);
        PlayerMovementSystem.SetPlayerParameters(m_playerParameters);
        PlayerHealthSystem.SetPlayerParameters(m_playerParameters);

        Singleton.Instance.GameEvents.OnPlayerLoaded?.Invoke(this);
    }

    public Camera GetPlayerCamera() => m_playerCamera;
}