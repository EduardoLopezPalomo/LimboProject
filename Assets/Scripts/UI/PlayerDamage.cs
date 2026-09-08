using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerDamage : MonoBehaviour
{
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 2f;
    [SerializeField] private float maxAlpha = 0.6f;

    private Image image;
    private Coroutine currentEffect;

    void Start()
    {
        image = GetComponent<Image>();
    }

    void OnEnable()
    {
        PlayerHealthResponse.OnPlayerHurt += ShowDamage;
    }

    void OnDisable()
    {
        PlayerHealthResponse.OnPlayerHurt -= ShowDamage;
    }

    public void ShowDamage()
    {
        if (currentEffect != null)
            StopCoroutine(currentEffect);

        currentEffect = StartCoroutine(DamageEffect());
    }

    public void StartLowHealthEffect()
    {
        if (currentEffect != null) StopCoroutine(currentEffect);
        currentEffect = StartCoroutine(LowHealth());
    }

    public void StopLowHealthEffect()
    {
        if (currentEffect != null)
        {
            StopCoroutine(currentEffect);
            currentEffect = null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private IEnumerator DamageEffect()
    {
        Color color = image.color;

        while (color.a < maxAlpha)
        {
            color.a += fadeInSpeed * Time.deltaTime;
            image.color = color;
            yield return null;
        }

        while (color.a > 0f)
        {
            color.a -= fadeOutSpeed * Time.deltaTime;
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }

    private IEnumerator LowHealth()
    {
        Color color = image.color;

        while (color.a < maxAlpha)
        {
            color.a += fadeInSpeed * Time.deltaTime;
            image.color = color;
            yield return null;
        }

        while (color.a > 0f)
        {
            color.a -= fadeOutSpeed * Time.deltaTime;
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }


}
