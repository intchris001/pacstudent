using UnityEngine;
using PacmanGame.Core;

namespace PacmanGame.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Clips")]
        public AudioClip introMusic; // level intro
        public AudioClip startSceneMusic; // for StartScene
        public AudioClip ghostsNormalMusic;
        public AudioClip ghostsFrightenedMusic;
        public AudioClip ghostsDeadPresentMusic; // when at least one ghost is dead

        [Header("SFX Clips")]
        public AudioClip sfxMove; // moving without pellet
        public AudioClip sfxPellet;
        public AudioClip sfxWall;
        public AudioClip sfxDeath;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private float introTimer;
        private bool playingIntro;
        private bool inStartScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = GetComponent<AudioSource>();
            musicSource.loop = true;

            // create separate SFX source
            var sfxGo = new GameObject("SFXSource");
            sfxGo.transform.SetParent(transform);
            sfxSource = sfxGo.AddComponent<AudioSource>();
            sfxSource.loop = false;
        }

        private void Update()
        {
            if (playingIntro)
            {
                introTimer -= Time.deltaTime;
                if (introTimer <= 0f || !musicSource.isPlaying)
                {
                    PlayGhostsNormalMusic();
                    playingIntro = false;
                }
            }
        }

        public void PlayStartSceneMusic()
        {
            inStartScene = true;
            PlayMusic(startSceneMusic);
        }

        public void PlayLevelIntroThenNormal()
        {
            inStartScene = false;
            if (introMusic != null)
            {
                PlayMusic(introMusic, false);
                introTimer = 3f; // or clip end, whichever first
                playingIntro = true;
            }
            else
            {
                PlayGhostsNormalMusic();
            }
        }

        public void PlayGhostsNormalMusic()
        {
            PlayMusic(ghostsNormalMusic);
        }

        public void PlayGhostsFrightenedMusic()
        {
            PlayMusic(ghostsFrightenedMusic);
        }

        public void PlayGhostsDeadPresentMusic()
        {
            PlayMusic(ghostsDeadPresentMusic);
        }

        private void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            musicSource.loop = loop;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfxMove()
        {
            if (sfxMove != null) sfxSource.PlayOneShot(sfxMove, 0.6f);
        }
        public void PlaySfxPellet()
        {
            if (sfxPellet != null) sfxSource.PlayOneShot(sfxPellet, 0.8f);
        }
        public void PlaySfxWall()
        {
            if (sfxWall != null) sfxSource.PlayOneShot(sfxWall, 0.8f);
        }
        public void PlaySfxDeath()
        {
            if (sfxDeath != null) sfxSource.PlayOneShot(sfxDeath, 0.9f);
        }
    }
}

