using UnityEngine;

public class DeathCheck : MonoBehaviour
{
    [SerializeField]private EnemiesGroupController controller;
    private bool IsDeath = false;

    public void SetIsDeath(bool value)
    {
        IsDeath = value;
        if (!IsDeath) return;

        // Assigned in the scene on 43 of the 44 enemies in Forest; the one that is not (Archer_giant_02
        // under EnemyGroupController_M (6)) used to die silently - the old guard skipped the group,
        // so CheckBattleOver never ran and the battle music and enemy HUD never reset.
        if (controller == null) controller = EnemiesGroupController.Resolve(gameObject);

        if (controller == null)
        {
            Debug.LogError($"{name}: no {nameof(EnemiesGroupController)} assigned and none found above {gameObject.name} - this death will not end the battle.", gameObject);
            return;
        }

        controller.ReturnToken();
        controller.EnemyHasDie(gameObject);
    }

    public bool GetIsDeath()
    {
        return IsDeath;
    }
}
