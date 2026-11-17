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

    [HideInInspector]
    public bool IsPoolActive => behaviorAgent != null && behaviorAgent.enabled;

    private void Awake()
    {
        graphBridge = GetComponentInChildren<NPCGraphBridge>(true);
        behaviorAgent = GetComponentInChildren<BehaviorGraphAgent>(true);
        navAgent = GetComponentInChildren<NavMeshAgent>(true);
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        // T-057 D2: this was GetComponent, which only matches a detection on the pool's own GameObject.
        // In Village that is true for 65 of the 66 pools, but Guard_masc1 (instanceID 1017254) keeps its
        // detection on a child, so the field was null there and the "behaviorAgent && navAgent &&
        // playerDetection" gate in Activate()/Deactivate() never passed - its graph agent could never be
        // switched back on. The other four lookups on this object already search the children.
        playerDetection = GetComponentInChildren<NpcPlayerDetection>(true);
    }

    private void OnEnable()
    {
        DayNight.OnDayPhaseChanged += WakeUp;
        if (DayNight.Instance != null)
        {
            WakeUp(DayNight.Instance.CurrentPhase);
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
