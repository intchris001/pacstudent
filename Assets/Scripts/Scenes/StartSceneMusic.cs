using UnityEngine;
using PacmanGame.Audio;

public class StartSceneMusic : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance?.PlayStartSceneMusic();
    }
}

