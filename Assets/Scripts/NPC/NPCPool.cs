using UnityEngine;
using System.Collections;

public class NPCPool : MonoBehaviour
{
    public bool RestAtHome = true;
    [SerializeField] private GameObject Npc;

    private NPCGraphBridge graphBridge;
    private Coroutine sleepRoutine;

    private void Awake()
    {
        graphBridge = GetComponentInChildren<NPCGraphBridge>(true);
    }

    public void WakeUp()
    {
        if (Npc)
        {
            Npc.SetActive(true);
        }
    }

    public void GoToSleep()
    {
        if (Npc && RestAtHome)
        {
            if (sleepRoutine != null)
            {
                StopCoroutine(sleepRoutine);
            }

            sleepRoutine = StartCoroutine(WaitUntilRouteFinishedThenSleep());
        }
    }

    private IEnumerator WaitUntilRouteFinishedThenSleep()
    {
        if (graphBridge == null)
            yield break;

        yield return new WaitUntil(() => graphBridge.GetReachedHome());

        Npc.SetActive(false);
    }

    public void ForceSleep()
    {
        if (Npc && RestAtHome)
        {
            Npc.SetActive(false);
        }
    }

}
