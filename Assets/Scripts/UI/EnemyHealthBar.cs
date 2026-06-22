using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string healthAttributeName = "Health";
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    
    private float maxHealth;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
    }

    private void LateUpdate()
    {
        UpdatePosition();
        UpdateFill();
        FaceCamera();
    }

    void UpdateFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        if (maxHealth <= 0) return;

        fillImage.fillAmount = Mathf.Clamp01(current / maxHealth);
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
