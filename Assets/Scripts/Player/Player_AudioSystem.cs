using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Player_AudioSystem : NetworkBehaviour {
    [Header("AudioSources")]
    [SerializeField] private AudioSource m_shootAudioSource;    
    [Space(10)]
    [SerializeField] private AudioClip[] m_revolverShotSounds;
    [SerializeField] private AudioClip[] m_shotgunShotSounds;

    private void Awake() {
        PreloadClips(m_revolverShotSounds);
        PreloadClips(m_shotgunShotSounds);
    }

    private void PreloadClips(AudioClip[] clips) {
        foreach (var clip in clips) {
            if (clip == null) continue;

            if (!clip.preloadAudioData)
                clip.LoadAudioData();
        }
    }

    public void CallPlayShotSFX(WeaponClass weapon) {
        PlayShotSFX(weapon);
        RequestShootSoundServerRpc(weapon);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestShootSoundServerRpc(WeaponClass weapon, ServerRpcParams rpc = default) {
        ulong senderClientId = rpc.Receive.SenderClientId;

        var targets = NetworkManager.Singleton.ConnectedClientsIds
            .Where(id => id != senderClientId)
            .ToArray();

        if (targets.Length == 0)
            return;

        var sendParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = targets
            }
        };

        PlayShootSoundClientRpc(weapon, sendParams);
    }

    [ClientRpc]
    private void PlayShootSoundClientRpc(WeaponClass weapon, ClientRpcParams rpcParams = default) {
        PlayShotSFX(weapon);
    }

    private void PlayShotSFX(WeaponClass weapon) {
        m_shootAudioSource.pitch = Random.Range(0.9f, 1.4f);
        AudioClip clip;

        switch (weapon) {
            case WeaponClass.Revolver:
                m_shootAudioSource.volume = 0.5f;
                clip = m_revolverShotSounds[Random.Range(0, m_revolverShotSounds.Length)];
                break;
            case WeaponClass.Shotgun:
                m_shootAudioSource.volume = 1f;
                clip = m_shotgunShotSounds[Random.Range(0, m_shotgunShotSounds.Length)];
                break;
            default:
                clip = null;
                break;
        }

        m_shootAudioSource.PlayOneShot(clip);
    }
}
