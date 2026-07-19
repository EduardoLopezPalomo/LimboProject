using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    
    [Header("Loop Settings")]
    public float loopStartSeconds = 0f;
    public float loopEndSeconds = 180f;
    public bool loopTillEnd = true;
    
    [Header("Battle Music")]
    public AudioClip battleMusic;
    public float musicFadeTime = 1.6f;
    
    private AudioSource audioSource;
    private AudioClip normalMusic;
    private int normalTimeSample;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        audioSource = GetComponent<AudioSource>();
        normalMusic = audioSource.clip;
        
    }
    
    void Start()
    {
        audioSource.loop = false;
        if (loopTillEnd)
        {
            loopEndSeconds = audioSource.clip.length;
        }
        audioSource.Play();
    }
    
    void Update()
    {
        if (audioSource.time >= loopEndSeconds || (!audioSource.isPlaying && loopTillEnd))
        {
            audioSource.Play();
            audioSource.time = loopStartSeconds;
        }
    }

    public void StartBattle()
    {
        StartCoroutine(ChangeToBattle(true));
    }

    public void EndBattle()
    {
        StartCoroutine(ChangeToBattle(false));
    }

    IEnumerator ChangeToBattle(bool start)
    {
        float time = 0f;
        float maxTimerEach = musicFadeTime * 0.5f;

        while (time < maxTimerEach)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(1, 0, time / maxTimerEach);
            yield return null;
        }

        time = 0f;
        if (start)
        {
            normalTimeSample = audioSource.timeSamples;
            audioSource.clip = battleMusic;   
        }
        else
        {
            audioSource.clip = normalMusic;
            audioSource.timeSamples = normalTimeSample;
        }
        audioSource.Play();
        
        while (time < maxTimerEach)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, 1, time / maxTimerEach);
            yield return null;
        }

    }
}
