using UnityEngine;
using PacmanGame.Level;
using PacmanGame.Util;
using PacmanGame.Core;

namespace PacmanGame.Player
{
    [RequireComponent(typeof(Collider2D))]
    public class PacmanController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f; // units per second
        public bool useInterpolation = true;

        private LevelGrid grid;
        private LevelLoader level;

        private Dir currentDir = Dir.Left;
        private Dir bufferedDir = Dir.None;

        private Vector2 targetWorldPos; // center of current target cell
        private Vector2Int currentCell; // grid cell we're moving from/to

        private void Start()
        {
            grid = LevelGrid.Instance;
            level = LevelLoader.Instance;
            if (grid == null || level == null)
            {
                grid = FindObjectOfType<LevelGrid>();
                level = FindObjectOfType<LevelLoader>();
            }

            // Initialize grid position to nearest cell center
            currentCell = grid.WorldToGrid(transform.position);
            targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            transform.position = targetWorldPos;
        }

        private void Update()
        {
            ReadInput();

            // If at center of a cell (or very close), decide next move based on buffered input and walls
            if ((Vector2)transform.position == targetWorldPos || Vector2.Distance(transform.position, targetWorldPos) < 0.01f)
            {
                transform.position = targetWorldPos; // snap
                currentCell = grid.WorldToGrid(transform.position);

                // Try to apply buffered direction if possible
                if (bufferedDir != Dir.None && CanMove(bufferedDir))
                {
                    currentDir = bufferedDir;
                    bufferedDir = Dir.None;
                }
                // If current direction blocked, stop or turn if possible
                if (!CanMove(currentDir))
                {
                    currentDir = Dir.None;
                }

                // Set next target world position
                if (currentDir != Dir.None)
                {
                    Vector2Int nextCell = currentCell + DirectionUtil.ToVec(currentDir);
                    // Wrap horizontally through tunnels
                    nextCell = grid.WrapHorizontal(nextCell);
                    // If wrapping occurred beyond bounds vertically, block
                    if (!grid.InBounds(nextCell.x, nextCell.y))
                    {
                        currentDir = Dir.None;
                    }
                    else
                    {
                        targetWorldPos = grid.GridToWorld(nextCell.x, nextCell.y);
                    }
                }
                else
                {
                    targetWorldPos = transform.position;
                }

                // Consume pellet on entering the cell center
                if (level != null)
                {
                    bool power;
                    if (level.TryConsumePellet(currentCell, out power))
                    {
                        GameManager.Instance?.OnPelletConsumed(power);
                    }
                }
            }

            // Move towards target
            if (useInterpolation)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            }
            else
            {
                Vector2 dir = (targetWorldPos - (Vector2)transform.position).normalized;
                transform.position = (Vector2)transform.position + dir * moveSpeed * Time.deltaTime;
            }

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
            transform.position = spawnPos;
            currentCell = grid.WorldToGrid(transform.position);
            targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            currentDir = Dir.Left;
            bufferedDir = Dir.None;
        }
    }
}

