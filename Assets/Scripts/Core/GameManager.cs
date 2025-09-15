using System.Collections.Generic;
using UnityEngine;
using PacmanGame.Level;
using PacmanGame.Player;
using PacmanGame.Ghosts;
using PacmanGame.Audio;

namespace PacmanGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Gameplay")]
        public int startingLives = 3;
        public int pointsPellet = 10;
        public int pointsPowerPellet = 50;
        public int pointsGhostBase = 200; // 200, 400, 800, 1600 chain
        public float frightenedDuration = 6f;

        [Header("Spawns")]
        public Transform pacmanSpawn;
        public List<GhostController> ghosts = new();

        [Header("Runtime State")]
        public int score = 0;
        public int lives = 0;
        public int ghostsEatenInChain = 0;
        private float frightenedTimer = 0f;

        private PacmanController pacman;

        private enum MusicState { None, Intro, Normal, Frightened, DeadPresent, StartScene }
        private MusicState musicState = MusicState.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            pacman = FindObjectOfType<PacmanController>();
            if (pacman == null)
            {
                Debug.LogWarning("GameManager: No PacmanController found in scene.");
            }

            lives = startingLives;
            ResetRound();

            // Start level intro then normal music (Assessment 3 audio requirement)
            AudioManager.Instance?.PlayLevelIntroThenNormal();
            musicState = MusicState.Intro;
        }

        private void Update()
        {
            if (frightenedTimer > 0f)
            {
                frightenedTimer -= Time.deltaTime;
                if (frightenedTimer <= 0f)
                {
                    EndFrightenedMode();
                }
            }

            UpdateMusicState();
        }

        private void UpdateMusicState()
        {
            if (AudioManager.Instance == null) return;

            bool anyDead = false;
            foreach (var g in ghosts)
            {
                if (g != null && g.CurrentState == GhostState.Eaten) { anyDead = true; break; }
            }

            if (frightenedTimer > 0f)
            {
                if (musicState != MusicState.Frightened)
                {
                    AudioManager.Instance.PlayGhostsFrightenedMusic();
                    musicState = MusicState.Frightened;
                }
            }
            else if (anyDead)
            {
                if (musicState != MusicState.DeadPresent)
                {
                    AudioManager.Instance.PlayGhostsDeadPresentMusic();
                    musicState = MusicState.DeadPresent;
                }
            }
            else
            {
                if (musicState != MusicState.Normal)
                {
                    AudioManager.Instance.PlayGhostsNormalMusic();
                    musicState = MusicState.Normal;
                }
            }
        }

        public void AddScore(int points)
        {
            score += points;
            // TODO: Update UI
        }

        public void OnPelletConsumed(bool power)
        {
            AddScore(power ? pointsPowerPellet : pointsPellet);
            if (power)
            {
                StartFrightenedMode();
            }
            AudioManager.Instance?.PlaySfxPellet();
        }

        public void OnPacmanCollidesWithGhost(GhostController ghost)
        {
            if (ghost.CurrentState == GhostState.Frightened)
            {
                ghostsEatenInChain++;
                int points = pointsGhostBase * (1 << (ghostsEatenInChain - 1));
                AddScore(points);
                ghost.SetEaten();
            }
            else if (ghost.CurrentState != GhostState.Eaten)
            {
                // Pacman loses a life
                lives--;
                AudioManager.Instance?.PlaySfxDeath();
                if (lives <= 0)
                {
                    GameOver();
                }
                else
                {
                    ResetRound();
                }
            }
        }

        public void ResetRound()
        {
            frightenedTimer = 0f;
            ghostsEatenInChain = 0;
            foreach (var g in ghosts)
            {
                if (g != null) g.ResetToSpawn();
            }
            if (pacman != null)
            {
                pacman.ResetToSpawn(pacmanSpawn != null ? pacmanSpawn.position : pacman.transform.position);
            }
        }

        private void StartFrightenedMode()
        {
            frightenedTimer = frightenedDuration;
            ghostsEatenInChain = 0;
            foreach (var g in ghosts)
            {
                if (g != null) g.SetFrightened();
            }
        }

        private void EndFrightenedMode()
        {
            foreach (var g in ghosts)
            {
                if (g != null && g.CurrentState == GhostState.Frightened)
                {
                    g.SetChase();
                }
            }
        }

        private void GameOver()
        {
            Debug.Log("Game Over");
            // TODO: Show UI and allow restart
            // For now, restart level
            lives = startingLives;
            score = 0;
            ResetRound();
        }
    }
}

