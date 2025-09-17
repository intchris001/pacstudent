using UnityEngine;
using PacmanGame.Audio;

public class StartSceneMusic : MonoBehaviour
{
    private void Start()
    {
        // Ensure AudioManager exists in StartScene
        if (AudioManager.Instance == null)
        {
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioSource>();
            go.AddComponent<AudioManager>();
        }
        AudioManager.Instance?.PlayStartSceneMusic();
    }
}

