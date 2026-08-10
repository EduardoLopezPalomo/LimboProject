using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public float recoverValue = 0.1f;
    
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [Header("Health Bar")] 
    public GameObject HealthBar;
    [SerializeField] private string healthAttributeName = "EnemyHealth";
    [SerializeField] private Image hFillImage;
    [SerializeField] private Image hOverFillImage;
    [Header("Resistance")]
    public GameObject ResistanceBar;
    [SerializeField] private string resistanceAttributeName = "BlockResistance";
    [SerializeField] private Image rFillImage;
    [SerializeField] private Image rRecoveryImage;
    
    private float maxHealth;
    private float overFillMaxhealth;
    private float maxResistance;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
        overFillMaxhealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        maxResistance = attributeSystem.GetAttributeMaxValue(resistanceAttributeName);
        
        float currentResistance = attributeSystem.GetAttributeValue(resistanceAttributeName);
        rFillImage.fillAmount = Mathf.Clamp01(currentResistance / maxResistance);
        rRecoveryImage.fillAmount = rFillImage.fillAmount;
        
        ResistanceBar.SetActive(false);
        HealthBar.SetActive(false);
    }

    private void LateUpdate()
    {
        if (ResistanceBar.activeSelf || HealthBar.activeSelf)
        {
            UpdatePosition();
            UpdateFill();
            UpdateOverFill();
            UpdateResistanceFill();
            UpdateResistanceRecovery();
            FaceCamera();
        }
    }

    void UpdateFill()
    {
        if (maxHealth <= 0) return;
        
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        hFillImage.fillAmount = Mathf.Clamp01(current / maxHealth);
    }

    void UpdateOverFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);
        if (maxHealth <= 0 || current <= maxHealth)
        {
            hOverFillImage.fillAmount = 0f;
            return;
        }
        
        current -= maxHealth;
        hOverFillImage.fillAmount = Mathf.Clamp01(current / (overFillMaxhealth - maxHealth));
    }
    void UpdateResistanceFill()
    {
        if (maxResistance <= 0) return;
 
        float current = attributeSystem.GetAttributeValue(resistanceAttributeName);
        rFillImage.fillAmount = Mathf.Clamp01(current / maxResistance);
    }
    
    void UpdateResistanceRecovery()
    {
        if (maxResistance <= 0) return;
 
        float target = rFillImage.fillAmount;
        float step = recoverValue / maxResistance;
 
        rRecoveryImage.fillAmount = Mathf.MoveTowards(rRecoveryImage.fillAmount, target, step);
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
