using Unity.Netcode;
using UnityEngine;

public class Player_AudioSystem : MonoBehaviour {
    [Header("AudioSources")]    
    public AudioSource m_shootAudioSource;
    [Space(10)]
    public AudioClip[] revolverShotSounds;
    public AudioClip[] shotgunShotSounds; //[TODO] Add to an SO

    internal void Play3DAudio(Weapons weapon) {
        RequestShootSoundServerRpc(weapon);
    }

    [ServerRpc]
    void RequestShootSoundServerRpc(Weapons weapon, ServerRpcParams rpcParams = default) => PlayShootSoundClientRpc(weapon);

    [ClientRpc]
    void PlayShootSoundClientRpc(Weapons weapon) {
        m_shootAudioSource.pitch = Random.Range(0.9f, 1.4f);
        AudioClip clip;

        switch (weapon) {
            case Weapons.Revolver:
                m_shootAudioSource.volume = 0.5f;
                clip = revolverShotSounds[Random.Range(0, revolverShotSounds.Length)];
                break;
            case Weapons.Shotgun:
                m_shootAudioSource.volume = 1f;
                clip = shotgunShotSounds[Random.Range(0, shotgunShotSounds.Length)];
                break;
            default:
                clip = null;
                break;
        }

        m_shootAudioSource.PlayOneShot(clip);
    }
}
