using UnityEngine;
using System.Collections;

public class BackgroundMusicManager : MonoBehaviour
{
    public AudioClip introClip;
    public AudioClip normalClip;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        StartCoroutine(PlayMusicSequence());
    }

    private IEnumerator PlayMusicSequence()
    {
        // Play intro music
        if (introClip != null)
        {
            audioSource.clip = introClip;
            audioSource.loop = false;
            audioSource.Play();
            yield return new WaitForSeconds(Mathf.Min(3f, introClip.length));
        }

        // Play normal background music
        if (normalClip != null)
        {
            audioSource.clip = normalClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}

