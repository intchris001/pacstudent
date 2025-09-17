using UnityEngine;
using UnityEngine.SceneManagement;
using PacmanGame.Audio;

public class StartSceneMusic : MonoBehaviour
{
    [Header("Start Input Settings")]
    public KeyCode[] startKeys = new KeyCode[] { KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter };
    public string nextSceneName = "RecreatedLevel";

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

    private void Update()
    {
        // If any key in the list is pressed, load the next scene
        for (int i = 0; i < startKeys.Length; i++)
        {
            if (Input.GetKeyDown(startKeys[i]))
            {
                TryLoadNext();
                return;
            }
        }
    }

    private void TryLoadNext()
    {
        if (string.IsNullOrEmpty(nextSceneName)) return;
        // Note: Ensure the scene is added to Build Settings for builds
        SceneManager.LoadScene(nextSceneName);
    }
}

