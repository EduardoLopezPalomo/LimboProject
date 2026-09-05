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

    private void OnEnable()
    {
        DayNight.OnDayPhaseChanged += WakeUp;
        if (DayNight.Instance != null)
        {
            WakeUp(DayNight.Instance.ActualPhase);
        }
    }

    void OnDisable()
    {
        DayNight.OnDayPhaseChanged -= WakeUp;
    }

    public void WakeUp(DayPhase newPhase)
    {
        if (Npc && newPhase == DayPhase.FirstLight)
        {
            Npc.SetActive(true);
        }
        else if (Npc && newPhase == DayPhase.Night)
        {
            GoToSleep();
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
        {
            yield break;
        }

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
