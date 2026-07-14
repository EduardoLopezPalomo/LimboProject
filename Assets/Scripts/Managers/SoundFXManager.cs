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
         AudioSource audioSource = Instantiate(soundFXObject, spawnTransform);
         
         audioSource.clip = clip;
         audioSource.volume = volume;
         audioSource.Play();
         
         Destroy(audioSource.gameObject, clip.length);
     }
     
     public void PlayRandomSound(AudioClip[] clips, Transform spawnTransform, float volume)
     {
         int random = Random.Range(0, clips.Length);
         
         AudioSource audioSource = Instantiate(soundFXObject, spawnTransform);
         
         audioSource.clip = clips[random];
         audioSource.volume = volume;
         audioSource.Play();
         
         Destroy(audioSource.gameObject, audioSource.clip.length);
     }
}
