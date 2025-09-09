using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Audio;

public class Player_VoiceChat : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private AudioSource m_voiceChatSource;

    private Queue<float[]> receivedAudio = new Queue<float[]>();
    private string connectedDevice;

    public override void OnNetworkSpawn() {
        if (!IsOwner) return;

        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.AddListener(OnMicrophoneDeviceSwitch);
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) return;

        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch.RemoveListener(OnMicrophoneDeviceSwitch);
    }

    private void OnMicrophoneDeviceSwitch(string device) {
        if (!string.IsNullOrEmpty(connectedDevice)) Microphone.End(connectedDevice);
        m_voiceChatSource.clip = Microphone.Start(device, true, 10, 44100);

        connectedDevice = device;
    }

    public float maxDistance = 15f;
    public KeyCode pushToTalkKey = KeyCode.V;

    private Dictionary<SteamId, AudioSource> sources = new();
    private Dictionary<SteamId, Queue<float[]>> buffers = new();

    void Update()
    {
        SendVoice();
        ReceiveVoice();
        UpdateVolumes();
    }

    void SendVoice()
    {/*
        if (!Input.GetKey(pushToTalkKey)) return;

        if (SteamUser.Voice.HasVoiceData)
        {
            using var ms = new System.IO.MemoryStream();
            int bytes = SteamUser.Voice.ReadVoiceData(ms);
            if (bytes > 0)
            {
                var data = ms.ToArray();
                SteamNetworking.SendP2PPacket(BinaryWriterPrefix(data), SendType.Unreliable);
            }
        }*/
    }

    void ReceiveVoice()
    {/*
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            SteamNetworking.ReadP2PPacket(out var data, out SteamId sender);
            byte[] voiceData = BinaryReaderSuffix(data);
            int samplesCount = voiceData.Length / 2;
            float[] samples = new float[samplesCount];
            for (int i = 0; i < samplesCount; i++)
                samples[i] = System.BitConverter.ToInt16(voiceData, i * 2) / 32768f;

            if (!buffers.TryGetValue(sender, out var queue))
            {
                queue = new Queue<float[]>();
                buffers[sender] = queue;
            }
            queue.Enqueue(samples);
        }*/

        foreach (var kv in buffers)
        {
            var id = kv.Key;
            var queue = kv.Value;
            if (queue.Count > 0 && !sources.ContainsKey(id))
            {
                var go = new GameObject("Voice_" + id);
                go.transform.parent = transform;
                var src = go.AddComponent<AudioSource>();
                src.spatialBlend = 1;
                src.minDistance = 1f;
                src.maxDistance = maxDistance;
                sources[id] = src;
            }

            if (sources.TryGetValue(id, out var audioSrc) && queue.Count > 0 && !audioSrc.isPlaying)
            {
                var samples = queue.Dequeue();
                var clip = AudioClip.Create("Voice_" + id, samples.Length, 1, 22050, false);
                clip.SetData(samples, 0);
                audioSrc.clip = clip;
                audioSrc.Play();
            }
        }
    }

    void UpdateVolumes()
    {
        foreach (var kv in sources)
        {
            if (kv.Value != null)
            {
                kv.Value.transform.position = transform.position;
            }
        }
    }

    byte[] BinaryWriterPrefix(byte[] data)
    {
        using var ms = new System.IO.MemoryStream();
        using var w = new System.IO.BinaryWriter(ms);
        w.Write(data.Length);
        w.Write(data);
        return ms.ToArray();
    }

    byte[] BinaryReaderSuffix(byte[] data)
    {
        using var ms = new System.IO.MemoryStream(data);
        using var r = new System.IO.BinaryReader(ms);
        int len = r.ReadInt32();
        return r.ReadBytes(len);
    }
}