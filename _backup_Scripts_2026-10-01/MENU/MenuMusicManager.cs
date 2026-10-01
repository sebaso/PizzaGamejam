using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Configuración")]
    public AudioClip musicaMenu;
    [Range(0f, 1f)] public float volumen = 0.3f; // Para que no suene muy alto

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicaMenu;
        audioSource.loop = true;
        audioSource.volume = volumen;
        audioSource.playOnAwake = false;
        
        audioSource.Play();
    }

    public void PararMusica()
    {
        audioSource.Stop();
    }
}