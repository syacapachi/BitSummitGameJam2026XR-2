using System.Collections;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    [SerializeField] private GameObject[] damageBorders;
    [SerializeField] private float flashTime = 0.3f;
    [SerializeField] AudioEffectData damageEffect;
    [Header("Publish Eevnt")]
    [SerializeField] GameEffectEvent effectEvent;
    [Header("Subscribe event")]
    [SerializeField] VoidEvent OnbulletComeRpcEvent;
    private CanvasGroup[] borderGroups;
    private float invFlashTime;

    private void Awake()
    {
        invFlashTime = 1f / flashTime;
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

    private void OnEnable()
    {
         OnbulletComeRpcEvent.Register(OnBulletComeChanged);
    }
    private void OnDisable()
    {
        OnbulletComeRpcEvent.Unregister(OnBulletComeChanged);
    }

    private void OnBulletComeChanged()
    {
         StartCoroutine(DamageFlash());   
         effectEvent.Invoke(new GameEffect(damageEffect.ToRuntimeData(),transform.position));
    }

    private IEnumerator DamageFlash()
    {
        for (int i = 0; i < damageBorders.Length; i++)
            damageBorders[i].SetActive(true);

        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Sin((t * invFlashTime) * Mathf.PI);
            for (int i = 0; i < borderGroups.Length; i++)
                borderGroups[i].alpha = alpha;

            yield return null;
        }

        for (int i = 0; i < damageBorders.Length; i++)
            damageBorders[i].SetActive(false);

    }
}