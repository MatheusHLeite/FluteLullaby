using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class DialogueManager : MonoBehaviour {
    [Header("Audio")]
    [SerializeField] private AudioMixerGroup m_audioMixer;

    [Header("Material")]
    [SerializeField] private Material m_outlineMaterial;

    [Header("Speakers voices")] ///[TODO] Add to SO 
    [SerializeField] private Character m_characterBean;
    [SerializeField] private Character m_characterOther;

    public AudioSource GetAudioSource() {
        AudioSource m_audioSource = gameObject.AddComponent<AudioSource>();

        m_audioSource.playOnAwake = false;

        m_audioSource.priority = 0;
        m_audioSource.volume = 0.4f;
        m_audioSource.outputAudioMixerGroup = m_audioMixer;

        return m_audioSource;
    }

    public Material GetOutlineMaterial() => m_outlineMaterial;

    private Color GetTalkerColor(Characters character) {
        return character switch {
            Characters.None => Color.white,
            Characters.Bean => Color.green,
            _ => Color.black
        };
    }

    private Vector2 GetVoicePitch(TalkerMood talkerMood) {
        return talkerMood switch {
            TalkerMood.Normal => new Vector2(0.9f, 1.2f),
            TalkerMood.Angry => new Vector2(0.75f, .6f),
            TalkerMood.Sad => new Vector2(0.4f, 0.8f),
            _ => Vector2.one
        };
    }

    private float GetVoiceSpeed(TalkerMood talkerMood) {
        return talkerMood switch {
            TalkerMood.Normal => 0.03f,
            TalkerMood.Angry => 0.06f,
            TalkerMood.Sad => 0.09f,
            _ => 0.03f
        };
    }

    public CharacterData GetTalker(Dialogue dialogue) {
        CharacterData data = new CharacterData();

        Characters character = dialogue.m_actualSpeaker;
        TalkerMood talkerMood = dialogue.m_speakerMood;
        CharacterVoice[] characterVoices = new CharacterVoice[] { };

        switch (character) { //[TODO] Edit When all characters added
            case Characters.None:
                characterVoices = m_characterOther.m_characterVoices;
                break;
            case Characters.Bean:
                characterVoices = m_characterBean.m_characterVoices;
                break;
        }

        for (int i = 0; i < characterVoices.Length; i++) {
            data.m_talkerColor = GetTalkerColor(character);
            data.m_talkerVoice = characterVoices[i].m_voice;

            if (characterVoices[i].m_talkerMood == talkerMood) {                
                data.m_pitchRange = GetVoicePitch(talkerMood);
                data.m_voiceSpeed = GetVoiceSpeed(talkerMood);
                break;
            }
        }
        return data;
    }
}

[System.Serializable]
public class Dialogue {    
    public Characters m_actualSpeaker;
    public TalkerMood m_speakerMood;
    [TextArea(2, 5)] public string m_dialogue;
    public List<DialogueOption> m_answers;
}

public enum Characters { None, Bean }

public enum TalkerMood { Normal, Angry, Sad }

[System.Serializable]
public struct Character {
    public Characters m_character;
    public CharacterVoice[] m_characterVoices;
}

[System.Serializable]
public struct CharacterVoice {
    public TalkerMood m_talkerMood;
    public AudioClip m_voice;
}

public struct CharacterData {
    public AudioClip m_talkerVoice;   
    public Color m_talkerColor;
    public Vector2 m_pitchRange;
    public float m_voiceSpeed;
}

[System.Serializable]
public class DialogueOption {
    public string m_optionText;
    public Dialogue_SO m_nextDialogue;
    public UnityEngine.Events.UnityEvent m_onSelected;
}