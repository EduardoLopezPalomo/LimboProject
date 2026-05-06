using UnityEngine;
using Unity.Behavior;

public class NPCPool : MonoBehaviour
{
    [SerializeField] private GameObject Npc;

    public void WakeUp()
    {
        BehaviorGraphAgent agent = GetComponentInChildren<BehaviorGraphAgent>(true);
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("NPCState", NPCState.GoingToWork);
        }
        if(Npc)
        {
            Npc.SetActive(true);
        }

    }

    public void GoToSleep()
    {
        if(Npc)
        {
            Npc.SetActive(false);
        }
    }

}
