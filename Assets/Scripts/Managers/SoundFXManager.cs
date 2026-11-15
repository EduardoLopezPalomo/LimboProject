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

    /// <summary>
    /// Plays <paramref name="clip"/> with its pitch drawn from the authored (minPitch, maxPitch) pair.
    /// Both bounds used to be accepted and then ignored (T-006); see <see cref="ResolvePitch"/> for how the
    /// pair is interpreted.
    /// </summary>
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
        audioSource.pitch = ResolvePitch(MinPitch, MaxPitch);
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

    /// <summary>
    /// Picks one clip at random and plays it with the pitch drawn from the authored (minPitch, maxPitch)
    /// pair. Both bounds used to be accepted and then ignored (T-006); see <see cref="ResolvePitch"/>.
    /// </summary>
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
        audioSource.pitch = ResolvePitch(MinPitch, MaxPitch);
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
        // The source lives for as long as the clip it plays, and pitch changes how long that is: 0.5x takes
        // twice as long, 2x half as long. This waited clip.length unconditionally, which was only correct
        // because pitch was pinned at 1 — now that authored pitch is honoured (T-006), the unpitched wait
        // would destroy the object mid-sound and cut the tail off everything below 1.0 (BossSlash sits at
        // 0.2 pitch, i.e. five times its clip length).
        float pitch = audioSource != null && audioSource.pitch > 0f ? audioSource.pitch : 1f;

        yield return new WaitForSeconds(clip.length / pitch);
        
        if (activeSources.ContainsKey(clip))
            activeSources[clip].Remove(audioSource);

        if (audioSource != null)
            Destroy(audioSource.gameObject);
    }

    /// <summary>
    /// Turns an authored (minPitch, maxPitch) pair into the pitch to apply.
    ///
    /// Unity clamps <see cref="AudioSource.pitch"/> at 0 and a 0 pitch neither plays nor stops cleanly, so a
    /// pair that was never filled in must not reach the AudioSource. <see cref="SFXDefinition"/> defaults both
    /// bounds to 1, and all 93 assets were authored while these arguments were ignored, so every rule below is
    /// "read the designer's intent, never invent one":
    ///   - both bounds unusable (0, negative or unset) -> 1, native pitch: exactly what shipped before
    ///   - exactly one bound usable -> that bound; a degenerate pair means "this pitch, no variation"
    ///   - min above max -> taken as the pair the author meant the other way round
    ///   - equal bounds -> that value, without a random draw
    ///   - a real range -> a fresh value inside it on every call
    /// </summary>
    public static float ResolvePitch(float minPitch, float maxPitch)
    {
        bool hasMin = minPitch > 0f;
        bool hasMax = maxPitch > 0f;

        if (!hasMin && !hasMax) return 1f;
        if (hasMin && !hasMax) return minPitch;
        if (!hasMin) return maxPitch;

        float low = Mathf.Min(minPitch, maxPitch);
        float high = Mathf.Max(minPitch, maxPitch);

        if (Mathf.Approximately(low, high)) return low;

        return Random.Range(low, high);
    }

}
