using UnityEngine;
using PacmanGame.Level;
using PacmanGame.Util;
using PacmanGame.Core;

namespace PacmanGame.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class PacmanController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f; // units per second
        public bool useInterpolation = true;

        [Header("Animation")]
        public float animationFps = 10f;
        public Sprite[] spritesUp;
        public Sprite[] spritesDown;
        public Sprite[] spritesLeft;
        public Sprite[] spritesRight;

        private int animationFrame = 0;
        private float animationTimer = 0f;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private LevelGrid grid;
        private LevelLoader level; // Note: This may be null when using LevelGenerator
        private LevelGenerator generator;

        private Dir currentDir = Dir.Left;
        private Dir bufferedDir = Dir.None;

        private Vector2 targetWorldPos; // center of current target cell
        private Vector2Int currentCell; // grid cell we're moving from/to
        private bool initialized = false;

        private void Start()
        {
            TryInit();
        }

        private void TryInit()
        {
            if (initialized) return;
            grid = FindObjectOfType<LevelGrid>();
            level = FindObjectOfType<LevelLoader>();
            generator = FindObjectOfType<LevelGenerator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (grid == null)
            {
                return; // Grid not ready yet.
            }
            currentCell = grid.WorldToGrid(transform.position);
            targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            transform.position = targetWorldPos;
            initialized = true;
            UpdateSprite();
        }

        private void Update()
        {
            if (!initialized) { TryInit(); if (!initialized) return; }
            ReadInput();

            // At or near a cell center, ready for a decision
            float centerTol = (grid != null ? Mathf.Max(0.01f, grid.CellSize * 0.05f) : 0.05f);
            if (Vector2.Distance(transform.position, targetWorldPos) <= centerTol)
            {
                transform.position = targetWorldPos; // Snap to be precise
                currentCell = grid.WorldToGrid(transform.position);

                // Consume pellet
                if (level != null && level.TryConsumePellet(currentCell, out bool power))
                {
                    GameManager.Instance?.OnPelletConsumed(power);
                }

                // Decide next move
                // 1. Try buffered direction first.
                if (bufferedDir != Dir.None && CanMove(bufferedDir))
                {
                    currentDir = bufferedDir;
                    bufferedDir = Dir.None;
                    UpdateSprite();
                }
                // 2. If no new direction, check if we can keep going in the current one.
                else if (!CanMove(currentDir))
                {
                    currentDir = Dir.None; // Stop if blocked
                    UpdateSprite();
                }

                // Set next target if we are moving
                if (currentDir != Dir.None)
                {
                    Vector2Int pre = currentCell + DirectionUtil.ToVec(currentDir);
                    Vector2Int nextCell = grid.WrapHorizontal(pre);

                    // Instant teleport if wrapping occurred
                    if (grid.enableHorizontalWrap && (pre.x != nextCell.x) && (pre.x < 0 || pre.x >= grid.Width))
                    {
                        transform.position = grid.GridToWorld(nextCell.x, nextCell.y);
                        currentCell = nextCell;
                    }

                    if (!grid.IsWalkableForPacman(nextCell.x, nextCell.y))
                    {
                        // Safety guard: never step into walls even if something went out of sync
                        currentDir = Dir.None;
                        targetWorldPos = transform.position;
                    }
                    else
                    {
                        targetWorldPos = grid.GridToWorld(nextCell.x, nextCell.y);
                    }
                }
            }

            // Always move towards the current target
            transform.position = Vector2.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

            // Animate sprite based on direction and time
            UpdateAnimation(currentDir);

            // Try consume pellets on the current cell and near the next cell even when not exactly centered
            ConsumeNearbyPellets();

            // Check collision with ghosts
            CheckGhostCollision();
        }

        private void ReadInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) bufferedDir = Dir.Left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) bufferedDir = Dir.Right;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) bufferedDir = Dir.Up;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) bufferedDir = Dir.Down;
        }

        private bool CanMove(Dir dir)
        {
            if (dir == Dir.None) return false;
            Vector2Int next = currentCell + DirectionUtil.ToVec(dir);
            next = grid.WrapHorizontal(next);
            if (!grid.InBounds(next.x, next.y)) return false;
            return grid.IsWalkableForPacman(next.x, next.y);
        }

        private void CheckGhostCollision()
        {
            var ghosts = FindObjectsOfType<PacmanGame.Ghosts.GhostController>();
            foreach (var g in ghosts)
            {
                float dist = Vector2.Distance(g.transform.position, transform.position);
                if (dist < 0.45f)
                {
                    GameManager.Instance?.OnPacmanCollidesWithGhost(g);
                }
            }
        }

        public void ResetToSpawn(Vector3 spawnPos)
        {
            if (!initialized) TryInit();

            transform.position = spawnPos;
            if (grid != null)
            {
                currentCell = grid.WorldToGrid(transform.position);
                targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            }
            else
            {
                targetWorldPos = transform.position;
            }
            currentDir = Dir.Left;
            bufferedDir = Dir.None;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;
            animationFrame = 0;
            animationTimer = 0f;
            var frames = GetFramesForDir(currentDir);
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[0];
        }

        private void UpdateAnimation(Dir dir)
        {
            if (spriteRenderer == null) return;
            var frames = GetFramesForDir(dir);
            if (frames == null || frames.Length == 0)
            {
                return;
            }
            if (dir == Dir.None)
            {
                spriteRenderer.sprite = frames[0];
                return;
            }

            animationTimer += Time.deltaTime;
            float frameDuration = Mathf.Max(0.01f, 1f / animationFps);
            while (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                animationFrame = (animationFrame + 1) % frames.Length;
            }
            spriteRenderer.sprite = frames[animationFrame];
                private void ConsumeNearbyPellets()
        {
            if (level == null || grid == null) return;
            // Consume on current cell
            Vector2Int here = grid.WorldToGrid(transform.position);
            if (level.TryConsumePellet(here, out bool power1))
            {
                GameManager.Instance?.OnPelletConsumed(power1);
            }
            // Also try the target cell (the one we're moving toward)
            Vector2Int towards = grid.WorldToGrid(targetWorldPos);
            if (level.TryConsumePellet(towards, out bool power2))
            {
                GameManager.Instance?.OnPelletConsumed(power2);
            }
        }
    }
}

        private Sprite[] GetFramesForDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Up: return spritesUp;
                case Dir.Down: return spritesDown;
                case Dir.Left: return spritesLeft;
                case Dir.Right: return spritesRight;
                default: return spritesLeft; // default idle facing left
            }
        }
    }
}

