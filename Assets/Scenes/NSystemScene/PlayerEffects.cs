using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerEffects : NetworkBehaviour
{
    [SerializeField] private GameObject[] damageBorders;
    [SerializeField] private float flashTime = 0.3f;
    private CanvasGroup[] borderGroups;

    private void Awake()
    {
        borderGroups = new CanvasGroup[damageBorders.Length];
        for (int i = 0; i < damageBorders.Length; i++)
        {
            borderGroups[i] = damageBorders[i].GetComponent<CanvasGroup>();
            if (borderGroups[i] == null)
                borderGroups[i] = damageBorders[i].AddComponent<CanvasGroup>();

            borderGroups[i].alpha = 0f;
            damageBorders[i].SetActive(false);
        }
    }

    private void Start()
    {
         ManagerLocator.Instance.AllGameManager.OnbulletComeRpcEvent += OnBulletComeChanged;
    }

    private void OnBulletComeChanged()
    {
         StartCoroutine(DamageFlash());        
    }

    private IEnumerator DamageFlash()
    {
        for (int i = 0; i < damageBorders.Length; i++)
            damageBorders[i].SetActive(true);

        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Sin((t / flashTime) * Mathf.PI);
            for (int i = 0; i < borderGroups.Length; i++)
                borderGroups[i].alpha = alpha;

            yield return null;
        }

        for (int i = 0; i < damageBorders.Length; i++)
            damageBorders[i].SetActive(false);

    }
}