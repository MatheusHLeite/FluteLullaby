using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public enum ColorblindTypes {
    Normal = 0,
    Protanopia,
    Protanomaly,
    Deuteranopia,
    Deuteranomaly,
    Tritanopia,
    Tritanomaly,
    Achromatopsia,
    Achromatomaly,
}

public class Colorblindness : MonoBehaviour {
    [SerializeField] private Volume[] volumes;

    VolumeComponent lastFilter;
    Coroutine coroutine;

    public void SetColorBlindnessFilter(int index) {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(ApplyFilter(index)); 
    }

    IEnumerator ApplyFilter(int index) {
        ResourceRequest loadRequest = Resources.LoadAsync<VolumeProfile>($"Colorblind/{(ColorblindTypes)index}");

        do yield return null; while (!loadRequest.isDone);

        var filter = loadRequest.asset as VolumeProfile;

        if (filter == null) {
            Debug.LogError("An error has occured! Please, report");
            yield break;
        }

        if (lastFilter != null) {
            foreach (var volume in volumes) {
                volume.profile.components.Remove(lastFilter);

                foreach (var component in filter.components)
                    volume.profile.components.Add(component);
            }
        }

        lastFilter = filter.components[0];
        coroutine = null;
    }
}