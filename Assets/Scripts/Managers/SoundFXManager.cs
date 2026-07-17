using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;
    [SerializeField] private AudioSource soundFXObject;

     private void Awake()
     {
         if (instance == null)
         {
             instance = this;
         }
     }

     public void PlaySound(AudioClip clip, Transform spawnTransform, float volume)
     {
         if (clip == null)
         {
             Debug.LogWarning("SoundFXManager::PlaySound: clip is null");
             return;
         }
         
         AudioSource audioSource = Instantiate(soundFXObject, spawnTransform);
         
         audioSource.clip = clip;
         audioSource.volume = volume;
         audioSource.Play();
         
         Destroy(audioSource.gameObject, clip.length);
     }
     
     public void PlaySoundRandomPitch(AudioClip clip, Transform spawnTransform, float volume, float MinPitch, float MaxPitch)
     {
         if (clip == null)
         {
             Debug.LogWarning("SoundFXManager::PlaySound: clip is null");
             return;
         }
         AudioSource audioSource = Instantiate(soundFXObject, spawnTransform);
         
         audioSource.clip = clip;
         audioSource.volume = volume;
         audioSource.pitch = Random.Range(MinPitch, MaxPitch);
         audioSource.Play();
         
         Destroy(audioSource.gameObject, clip.length);
     }
     
     public void PlayRandomSound(AudioClip[] clips, Transform spawnTransform, float volume)
     {
         if (clips.Length == 0)
         {
             Debug.LogWarning("SoundFXManager::PlaySound: clips has 0 length");
             return;
         }
         int random = Random.Range(0, clips.Length);
         
         AudioSource audioSource = Instantiate(soundFXObject, spawnTransform);
         
         audioSource.clip = clips[random];
         audioSource.volume = volume;
         audioSource.Play();
         
         Destroy(audioSource.gameObject, audioSource.clip.length);
     }
    
}
