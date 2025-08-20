using Unity.Netcode;
using UnityEngine;

public class Player_AudioSystem : NetworkBehaviour {
    [Header("AudioSources")]    
    public AudioSource m_shootAudioSource;
    public AudioSource m_voiceChatSource;
    [Space(10)]
    public AudioClip[] m_revolverShotSounds;
    public AudioClip[] m_shotgunShotSounds;

    private string connectedDevice;

    public override void OnNetworkSpawn() {
        if (!IsOwner) return;

        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.AddListener(OnMicrophoneDeviceSwitch);
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) return;

        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.RemoveListener(OnMicrophoneDeviceSwitch);
    }

    internal void PlayShotSFX(Weapons weapon) => RequestShootSoundServerRpc(weapon);
 
    [ServerRpc]
    void RequestShootSoundServerRpc(Weapons weapon, ServerRpcParams rpcParams = default) => PlayShootSoundClientRpc(weapon);

    [ClientRpc]
    void PlayShootSoundClientRpc(Weapons weapon) {
        m_shootAudioSource.pitch = Random.Range(0.9f, 1.4f);
        AudioClip clip;

        switch (weapon) {
            case Weapons.Revolver:
                m_shootAudioSource.volume = 0.5f;
                clip = m_revolverShotSounds[Random.Range(0, m_revolverShotSounds.Length)];
                break;
            case Weapons.Shotgun:
                m_shootAudioSource.volume = 1f;
                clip = m_shotgunShotSounds[Random.Range(0, m_shotgunShotSounds.Length)];
                break;
            default:
                clip = null;
                break;
        }

        m_shootAudioSource.PlayOneShot(clip);
    }

    private void OnMicrophoneDeviceSwitch(string device) {
        if (!string.IsNullOrEmpty(connectedDevice)) Microphone.End(connectedDevice);
        m_voiceChatSource.clip = Microphone.Start(device, true, 10, 44100);

        connectedDevice = device;
    }
}
