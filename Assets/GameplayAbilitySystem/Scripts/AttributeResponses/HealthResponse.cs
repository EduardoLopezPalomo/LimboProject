using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/Health")]
public class HealthResponse : AttributeResponse
{
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner)
    {
        if (attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue <= 0)
        {
            GameObject target = owner.gameObject;
            Animator animator = target.GetComponent<Animator>();

            animator.SetTrigger("IsDeath");

            Destroy(target, 2f);
        }
    }

}
