using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour {
    [Header("Trail")]
    [SerializeField] private TrailRenderer m_shotTrail;
    [Header("Decals")]
    [SerializeField] private GameObject m_shotDecal;

    private Queue<GameObject> _shotDecalPool = new Queue<GameObject>();
    private Queue<TrailRenderer> _shotTrailPool = new Queue<TrailRenderer>();

    private int _poolSize = 90;
    private GameObject decalsParentTransform;

    private void Awake() {
        Singleton.Instance.GameEvents.OnGameStarted.AddListener(InitializePool);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnGameStarted.RemoveListener(InitializePool);
        if (decalsParentTransform) Destroy(decalsParentTransform);
    }

    #region Shot Decal Pool
    private void InitializePool() {
        decalsParentTransform = new GameObject("ShotDecalsHolder");

        for (int i = 0; i < _poolSize; i++) {
            GameObject decal = Instantiate(m_shotDecal);
            decal.SetActive(false);
            _shotDecalPool.Enqueue(decal);
            decal.transform.SetParent(decalsParentTransform.transform);

            TrailRenderer trail = Instantiate(m_shotTrail);
            trail.gameObject.SetActive(false);
            _shotTrailPool.Enqueue(trail);
            trail.transform.SetParent(decalsParentTransform.transform);
        }
    }

    public GameObject GetShotDecal() {
        if (_shotDecalPool.Count > 0)
            return _shotDecalPool.Dequeue();

        GameObject decal = Instantiate(m_shotDecal);
        decal.SetActive(false);

        return decal;
    }

    public TrailRenderer GetShotTrail() {
        if (_shotDecalPool.Count > 0)
            return _shotTrailPool.Dequeue();

        TrailRenderer trail = Instantiate(m_shotTrail);
        trail.gameObject.SetActive(false);

        return trail;
    }

    public void ReturnShotDecal(GameObject decal) => StartCoroutine(ReturnShotDecalToPool(decal));

    private IEnumerator ReturnShotDecalToPool(GameObject decal) {
        yield return new WaitForSeconds(10);
        decal.SetActive(false);
        _shotDecalPool.Enqueue(decal);
    }

    public void ReturnTrail(TrailRenderer trail) {
        trail.gameObject.SetActive(false);
        _shotTrailPool.Enqueue(trail);
    }
    #endregion
}
