using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour {
    [Header("Decals")]
    [SerializeField] private GameObject m_shotDecal;
    private Queue<GameObject> _shotDecalPool = new Queue<GameObject>();
    private int _poolSize = 90;
    private GameObject decalsParentTransform;

    private void Awake() {
        Singleton.Instance.GameEvents.OnGameStarted.AddListener(InitializeShotPool);
    }
    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnGameStarted.RemoveListener(InitializeShotPool);
        if (decalsParentTransform) Destroy(decalsParentTransform);
    }

    #region Shot Decal Pool
    private void InitializeShotPool() {
        decalsParentTransform = new GameObject("ShotDecalsHolder");

        for (int i = 0; i < _poolSize; i++) {
            GameObject decal = Instantiate(m_shotDecal);
            decal.SetActive(false);
            _shotDecalPool.Enqueue(decal);
            decal.transform.SetParent(decalsParentTransform.transform);
        }
    }

    public GameObject GetShotDecal() {
        if (_shotDecalPool.Count > 0)
            return _shotDecalPool.Dequeue();

        GameObject decal = Instantiate(m_shotDecal);
        decal.SetActive(false);

        return decal;
    }

    public void ReturnShotDecal(GameObject decal) => StartCoroutine(ReturnToPool(decal));

    private IEnumerator ReturnToPool(GameObject decal) {
        yield return new WaitForSeconds(10);
        decal.SetActive(false);
        _shotDecalPool.Enqueue(decal);
    }
    #endregion
}
