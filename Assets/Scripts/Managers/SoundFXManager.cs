using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;
    [SerializeField] private AudioSource soundFXObject;
    
    private Dictionary<AudioClip, List<AudioSource>> activeSources = new Dictionary<AudioClip, List<AudioSource>>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public AudioSource PlaySound(AudioClip clip, Transform spawnTransform, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundFXManager::PlaySound: clip is null");
            return null;
        }
        
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, transform);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        
        if (!activeSources.ContainsKey(clip))
            activeSources[clip] = new List<AudioSource>();

        activeSources[clip].Add(audioSource);

        StartCoroutine(RemoveAfterPlay(clip, audioSource));
        
        return audioSource;
    }

    public AudioSource PlaySoundRandomPitch(AudioClip clip, Transform spawnTransform, float volume, float MinPitch, float MaxPitch)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundFXManager::PlaySound: clip is null");
            return null;
        }
        
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, transform);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        
        if (!activeSources.ContainsKey(clip))
            activeSources[clip] = new List<AudioSource>();

        activeSources[clip].Add(audioSource);

        StartCoroutine(RemoveAfterPlay(clip, audioSource));
        
        return audioSource;
    }

    public AudioSource PlayRandomSound(AudioClip[] clips, Transform spawnTransform, float volume)
    {
        if (clips.Length == 0)
        {
            Debug.LogWarning("SoundFXManager::PlaySound: clips has 0 length");
            return null;
        }
        
        int random = Random.Range(0, clips.Length);
        AudioClip clip = clips[random];

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, transform);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        
        if (!activeSources.ContainsKey(clip))
            activeSources[clip] = new List<AudioSource>();

        activeSources[clip].Add(audioSource);

        StartCoroutine(RemoveAfterPlay(clip, audioSource));
        
        return audioSource;
    }

    public AudioSource PlayRandomSoundRandomPitch(AudioClip[] clips, Transform spawnTransform, float volume, float MinPitch, float MaxPitch)
    {
        if (clips.Length == 0)
        {
            Debug.LogWarning("SoundFXManager::PlaySound: clips has 0 length");
            return null;
        }
        
        int random = Random.Range(0, clips.Length);
        AudioClip clip = clips[random];

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, transform);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        
        if (!activeSources.ContainsKey(clip))
            activeSources[clip] = new List<AudioSource>();

        activeSources[clip].Add(audioSource);

        StartCoroutine(RemoveAfterPlay(clip, audioSource));
        
        return audioSource;
    }
    
    public void StopSound(AudioClip clip, AudioSource audioSource)
    {
        if (activeSources.ContainsKey(clip) && activeSources[clip].Contains(audioSource))
        {
            activeSources[clip].Remove(audioSource);
            Destroy(audioSource.gameObject);
        }
    }
    
    private IEnumerator RemoveAfterPlay(AudioClip clip, AudioSource audioSource)
    {
        yield return new WaitForSeconds(audioSource.clip.length);
        
        if (activeSources.ContainsKey(clip))
            activeSources[clip].Remove(audioSource);

        if (audioSource != null)
            Destroy(audioSource.gameObject);
    }

}
