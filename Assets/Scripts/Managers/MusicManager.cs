using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Loop Settings")]
    public float loopStartSeconds = 0f;
    public float loopEndSeconds = 180f;
    public bool loopTillEnd = true;
    
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
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
}
