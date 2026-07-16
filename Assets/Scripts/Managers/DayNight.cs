using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum DayPhase
{
    Night,
    FirstLight,
    Dawn,
    Day,
    Sunset,
    Null
}
public class DayNight : MonoBehaviour
{
    public static DayNight Instance { get; private set; }
    public float DayDuration = 60f;
    public float SunYaw = 170f;

    [Range(0f, 1f)]
    public float timeOfDay = 0.25f;

    public DayPhase ActualPhase = DayPhase.Day;
    
    public bool CanChangePhase = true;

    public AnimationCurve SunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve SunTemperatureCUrve = AnimationCurve.Linear(0f, 2000f, 1f, 6500f);

    public AnimationCurve MoonIntensityCurve = AnimationCurve.EaseInOut(0f, 0.6f, 1f, 0f);
    public AnimationCurve MoonTemperatureCUrve = AnimationCurve.Linear(0f, 9000f, 1f, 7000f);

    [SerializeField] Light Sun;
    [SerializeField] Light Moon;

    [Header("SkyBoxes")]
    public Material[] daySkies;
    public Material[] nightSkies;
    public Material[] sunsetSkies;
    public Material[] dawnSkies;
    public static event Action<DayPhase> OnDayPhaseChanged;
    private DayPhase lastPhase;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Option to persist across scenes if needed
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Sun != null && Moon != null)
        {
            Sun.useColorTemperature = true;
            Moon.useColorTemperature = true;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        ActualPhase = GetPhase(timeOfDay);
        RotateLights();
        ChangePhase(ActualPhase);
    }

    void Update()
    {
        if(!CanChangePhase) return;
        
        timeOfDay += Time.deltaTime / DayDuration;
        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f;
        }
        
        RotateLights();
        
        DayPhase newPhase = GetPhase(timeOfDay);
        ChangePhase(newPhase);
    }

    Material Pick(Material[] list)
    {
        if (list == null || list.Length == 0) return null;
        return list[UnityEngine.Random.Range(0, list.Length)];
    }

    void ApplySkybox(Material mat)
    {
        if (mat == null || RenderSettings.skybox == mat) return;
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
    }

    void RotateLights()
    {
        float sunAngle = timeOfDay * 360f - 90f;
        float moonAngle = sunAngle + 180f;

        if (Sun == null || Moon == null) return;

        Sun.transform.rotation = Quaternion.Euler(sunAngle, SunYaw, 0f);
        Moon.transform.rotation = Quaternion.Euler(moonAngle, SunYaw, 0f);

        Sun.intensity = SunIntensityCurve.Evaluate(timeOfDay);
        Moon.intensity = MoonIntensityCurve.Evaluate(timeOfDay);

        Sun.colorTemperature = SunTemperatureCUrve.Evaluate(timeOfDay);
        Moon.colorTemperature = MoonTemperatureCUrve.Evaluate(timeOfDay);

        float sunHeight = Mathf.Sin(timeOfDay * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float daylight = Mathf.Clamp01((sunHeight + 0.15f) / 1.15f);
        daylight = Mathf.SmoothStep(0f, 1f, daylight);

        bool isDay = sunHeight >= 0f;

        RenderSettings.sun = isDay ? Sun : Moon;

        Sun.shadows = isDay ? LightShadows.Soft : LightShadows.None;
        Moon.shadows = isDay ? LightShadows.None : LightShadows.Soft;

        RenderSettings.ambientIntensity = Mathf.Lerp(0.35f, 1f, daylight);
    }
    
    void ChangePhase(DayPhase newPhase)
    {
        if (newPhase != lastPhase)
        {
            Material selected = null;
            OnDayPhaseChanged?.Invoke(newPhase);

            switch (newPhase)
            {
                case DayPhase.Day:
                    selected = Pick(daySkies);
                    ActualPhase = DayPhase.Day;
                    break;

                case DayPhase.Night:
                    selected = Pick(nightSkies);
                    ActualPhase = DayPhase.Night;
                    break;

                case DayPhase.Dawn:
                    selected = Pick(dawnSkies);
                    ActualPhase = DayPhase.Dawn;
                    break;

                case DayPhase.Sunset:
                    selected = Pick(sunsetSkies);
                    ActualPhase = DayPhase.Sunset;
                    break;
                case DayPhase.FirstLight:
                    ActualPhase = DayPhase.FirstLight;
                    break;
            }

            ApplySkybox(selected);
            lastPhase = newPhase;
        }
    }

    DayPhase GetPhase(float t)
    {
        if (t < 0.20f) return DayPhase.Night;
        if (t < 0.205f) return DayPhase.FirstLight;
        if (t < 0.25f) return DayPhase.Dawn;
        if (t < 0.65f) return DayPhase.Day;
        if (t < 0.77f) return DayPhase.Sunset;
        return DayPhase.Night;
    }
    
    public void SetTimeOfDay(float t)
    {
        timeOfDay = Mathf.Clamp01(t);
        RotateLights();
        DayPhase newPhase = GetPhase(timeOfDay);
        lastPhase = DayPhase.Null;
        ChangePhase(newPhase);
        
    }
}
