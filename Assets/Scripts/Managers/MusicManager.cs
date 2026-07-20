using System;
using System.Collections;
using UnityEngine;

[Serializable]
public enum MusicState
{
    Default, 
    Normal,
    Battle
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    
    [Header("Normal Loop Settings")]
    public float loopStartSeconds = 0f;
    public float loopEndSeconds = 180f;
    public bool loopTillEnd = true;
    
    [Header("Battle Loop Settings")]
    public float battleStartSeconds = 0f;
    public float battleEndSeconds = 180f;
    public bool battleTillEnd = true;
    
    [Header("Battle Music")]
    public AudioClip battleMusic;
    public float musicFadeTime = 1.6f;
    
    private AudioSource audioSource;
    private AudioClip normalMusic;
    private int normalTimeSample;
    private MusicState musicState = MusicState.Default;

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
        if (audioSource == null) return;
        audioSource.loop = false;
        if (loopTillEnd)
        {
            loopEndSeconds = audioSource.clip.length;
        }
        if (battleTillEnd && battleMusic != null)
        {
            battleEndSeconds = battleMusic.length;
        }
        audioSource.Play();
        musicState = MusicState.Normal;
    }
    
    void Update()
    {
        if (musicState == MusicState.Normal)
        {
            if (audioSource.time >= loopEndSeconds || (!audioSource.isPlaying && loopTillEnd))
            {
                audioSource.Play();
                audioSource.time = loopStartSeconds;
            }    
        }
        else if (musicState == MusicState.Battle)
        {
            if (audioSource.time >= battleEndSeconds || (!audioSource.isPlaying && battleTillEnd))
            {
                audioSource.Play();
                audioSource.time = battleStartSeconds;
            } 
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
            musicState = MusicState.Battle;
        }
        else
        {
            audioSource.clip = normalMusic;
            audioSource.timeSamples = normalTimeSample;
            musicState = MusicState.Normal;
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
