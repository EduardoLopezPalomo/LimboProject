using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Unity.Behavior;

public class NPCPool : MonoBehaviour
{
    public bool RestAtHome = true;
    public GameObject Npc;

    private BehaviorGraphAgent behaviorAgent;
    private NavMeshAgent navAgent;
    private NPCGraphBridge graphBridge;

    private SkinnedMeshRenderer[] renderers;
    private NpcPlayerDetection playerDetection;
    private Coroutine sleepRoutine;

    private bool isSleeping = false;

    [HideInInspector]
    public bool IsPoolActive => behaviorAgent != null && behaviorAgent.enabled;

    private void Awake()
    {
        graphBridge = GetComponentInChildren<NPCGraphBridge>(true);
        behaviorAgent = GetComponentInChildren<BehaviorGraphAgent>(true);
        navAgent = GetComponentInChildren<NavMeshAgent>(true);
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        playerDetection = GetComponent<NpcPlayerDetection>();
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
            isSleeping = false;
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
        isSleeping = true;
    }

    public void ForceSleep()
    {
        if (Npc && RestAtHome)
        {
            Npc.SetActive(false);
        }
    }

    public void Deactivate()
    {
        if (behaviorAgent && navAgent && playerDetection)
        {
            behaviorAgent.enabled = false;
            navAgent.enabled = false;
            playerDetection.enabled = false;
        }

        foreach (var r in renderers)
        {
            r.enabled = false;
        }
    }

    public void Activate()
    {
        if (isSleeping) return;

        if (behaviorAgent && navAgent && playerDetection)
        {
            behaviorAgent.enabled = true;
            navAgent.enabled = true;
            playerDetection.enabled = true;
        }

        foreach (var r in renderers)
        {
            r.enabled = true;
        }
    }

}
