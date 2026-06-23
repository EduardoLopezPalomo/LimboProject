using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string healthAttributeName = "Health";
    [SerializeField] private Image fillImage;
    [SerializeField] private Image overFillImage;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    
    private float maxHealth;
    private float overFillMaxhealth;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
        overFillMaxhealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        UpdatePosition();
        UpdateFill();
        UpdateOverFill();
        FaceCamera();
    }

    void UpdateFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        if (maxHealth <= 0) return;

        fillImage.fillAmount = Mathf.Clamp01(current / maxHealth);
    }

    void UpdateOverFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);
        if (maxHealth <= 0 || current <= maxHealth)
        {
            overFillImage.fillAmount = 0f;
            return;
        }
        
        current -= maxHealth;
        overFillImage.fillAmount = Mathf.Clamp01(current / (overFillMaxhealth - maxHealth));
    }

    void UpdatePosition()
    {
        if (followTarget != null)
            transform.position = followTarget.position + offset;
    }

    void FaceCamera()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}
