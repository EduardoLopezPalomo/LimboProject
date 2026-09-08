using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string healthAttributeName = "PlayerHealth";
    [SerializeField] private Image fillImage;
    [SerializeField] private SFXDefinition lowHealthSFX;
    [SerializeField] private float lowHealthThreshold = 0.1f;
    [SerializeField] private PlayerDamage playerDamage;

    private float maxHealth;
    private Coroutine lowHealthSFXCoroutine;

    private void Start()
    {
        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();

        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
    }

    void OnDisable()
    {
        if (lowHealthSFXCoroutine != null)
        {
            StopCoroutine(lowHealthSFXCoroutine);
            lowHealthSFXCoroutine = null;
            playerDamage.StopLowHealthEffect();
        }
    }

    private void LateUpdate()
    {
        UpdateFill();
    }

    void UpdateFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        if (maxHealth <= 0) return;

        fillImage.fillAmount = Mathf.Clamp01(current / maxHealth);

        if (current / maxHealth <= lowHealthThreshold)
        {
            if (lowHealthSFXCoroutine == null)
            {
                lowHealthSFXCoroutine = StartCoroutine(PlayLowHealthSFX());
            }
        }
        else
        {
            if (lowHealthSFXCoroutine != null)
            {
                StopCoroutine(lowHealthSFXCoroutine);
                playerDamage.StopLowHealthEffect();
                lowHealthSFXCoroutine = null;
            }
        }
    }

    private System.Collections.IEnumerator PlayLowHealthSFX()
    {
        playerDamage.StartLowHealthEffect();
        SoundFXManager.instance.PlaySoundRandomPitch(lowHealthSFX.sfx, attributeSystem.transform, lowHealthSFX.volume, lowHealthSFX.minPitch, lowHealthSFX.maxPitch);
        yield return new WaitForSeconds(lowHealthSFX.sfx.length);

        lowHealthSFXCoroutine = null;
    }
}