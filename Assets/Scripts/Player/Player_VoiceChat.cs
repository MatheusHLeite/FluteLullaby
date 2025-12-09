using Unity.Netcode;
using UnityEngine;

public class Player_VoiceChat : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private AudioSource m_voiceChatSource;

    private string connectedDevice;

    public override void OnNetworkSpawn() {
        if (!IsOwner) return;

        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.AddListener(OnMicrophoneDeviceSwitch);
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) return;

        Microphone.End(connectedDevice);
        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.RemoveListener(OnMicrophoneDeviceSwitch);
    }

    private void OnMicrophoneDeviceSwitch(string device) {
        if (!string.IsNullOrEmpty(connectedDevice)) Microphone.End(connectedDevice);
        m_voiceChatSource.clip = Microphone.Start(device, true, 10, 14400);

        connectedDevice = device;
    }
}