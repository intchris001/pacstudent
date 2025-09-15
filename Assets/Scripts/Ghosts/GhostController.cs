using System.Collections.Generic;
using UnityEngine;
using PacmanGame.Level;
using PacmanGame.Util;

namespace PacmanGame.Ghosts
{
    public enum GhostState { Scatter, Chase, Frightened, Eaten }

    public class GhostController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5.5f;
        public float frightenedSpeed = 4.2f;
        public float eatenSpeed = 8f;

        [Header("Targets (grid coords)")]
        public Vector2Int scatterCorner = new Vector2Int(0, 0);

        [Header("Debug")]
        public bool drawTargets = false;

        private LevelGrid grid;
        private Transform pacman;

        private Vector2Int currentCell;
        private Vector2 targetWorldPos;
        private Dir currentDir = Dir.Left;

        public GhostState CurrentState { get; private set; } = GhostState.Scatter;

        // Scatter/Chase timer (simple cycle)
        private float stateTimer = 0f;
        private int cycleIndex = 0;
        private readonly float[] scatterDurations = { 7f, 7f, 5f, 5f };
        private readonly float[] chaseDurations = { 20f, 20f, 20f, 999f };

        // Gate location cache
        private Vector2Int? gateCell;

        private Vector3 spawnPosition;

        private void Start()
        {
            grid = LevelGrid.Instance ?? FindObjectOfType<LevelGrid>();
            var pc = FindObjectOfType<PacmanGame.Player.PacmanController>();
            if (pc != null) pacman = pc.transform;

            // If no LevelGrid is present (e.g., A3 manual layout phase), disable this controller safely
            if (grid == null)
            {
                spawnPosition = transform.position;
                CurrentState = GhostState.Scatter;
                enabled = false; // do not run Update logic
                return;
            }

            currentCell = grid.WorldToGrid(transform.position);
            targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            transform.position = targetWorldPos;
            spawnPosition = transform.position;

            stateTimer = scatterDurations[0];
            CurrentState = GhostState.Scatter;
            cacheGateCell();
        }

        private void cacheGateCell()
        {
            if (grid == null) return;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (grid.GetTileChar(x, y) == 'g') { gateCell = new Vector2Int(x, y); return; }
                }
            }
        }

        private void Update()
        {
            UpdateTimers();

            // At cell center: choose next direction
            if ((Vector2)transform.position == targetWorldPos || Vector2.Distance(transform.position, targetWorldPos) < 0.01f)
            {
                transform.position = targetWorldPos;
                currentCell = grid.WorldToGrid(transform.position);

                Dir nextDir = ChooseNextDirection();
                currentDir = nextDir;

                Vector2Int nextCell = currentCell + DirectionUtil.ToVec(currentDir);
                nextCell = grid.WrapHorizontal(nextCell);
                if (!grid.InBounds(nextCell.x, nextCell.y))
                {
                    currentDir = Dir.None;
                    targetWorldPos = transform.position;
                }
                else
                {
                    targetWorldPos = grid.GridToWorld(nextCell.x, nextCell.y);
                }
            }

            float speed = moveSpeed;
            if (CurrentState == GhostState.Frightened) speed = frightenedSpeed;
            else if (CurrentState == GhostState.Eaten) speed = eatenSpeed;

            transform.position = Vector2.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);
        }

        private void UpdateTimers()
        {
            if (CurrentState == GhostState.Frightened || CurrentState == GhostState.Eaten) return; // handled by GameManager or until gate reached

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                if (CurrentState == GhostState.Scatter)
                {
                    SetChase();
                    stateTimer = chaseDurations[Mathf.Min(cycleIndex, chaseDurations.Length - 1)];
                }
                else if (CurrentState == GhostState.Chase)
                {
                    SetScatter();
                    cycleIndex = Mathf.Min(cycleIndex + 1, scatterDurations.Length - 1);
                    stateTimer = scatterDurations[cycleIndex];
                }
            }
        }

        private Dir ChooseNextDirection()
        {
            // Gate passability:
            // - In normal states, ghosts can pass through 'g' tile (outward only) but we don't simulate directionality here; allowed.
            // - When Eaten, head to gate/home and pass through gate if needed.

            // Collect candidate dirs (no reverse unless dead end)
            List<Dir> candidates = new List<Dir>();
            foreach (Dir d in new[] { Dir.Left, Dir.Up, Dir.Right, Dir.Down })
            {
                if (DirectionUtil.Opposite(d) == currentDir) continue; // avoid reversal preference
                Vector2Int nxt = currentCell + DirectionUtil.ToVec(d);
                nxt = grid.WrapHorizontal(nxt);
                if (!grid.InBounds(nxt.x, nxt.y)) continue;
                bool walkable = grid.IsWalkableForGhost(nxt.x, nxt.y);
                if (walkable) candidates.Add(d);
            }

            // If no candidates (dead end), allow reversing
            if (candidates.Count == 0)
            {
                Dir back = DirectionUtil.Opposite(currentDir);
                Vector2Int nxt = currentCell + DirectionUtil.ToVec(back);
                nxt = grid.WrapHorizontal(nxt);
                if (grid.InBounds(nxt.x, nxt.y) && grid.IsWalkableForGhost(nxt.x, nxt.y)) return back;
                return Dir.None;
            }

            // Frightened: choose random among candidates
            if (CurrentState == GhostState.Frightened)
            {
                int idx = Random.Range(0, candidates.Count);
                return candidates[idx];
            }

            // Eaten: target gate/home
            Vector2Int target = GetTargetCell();

            // Choose candidate minimizing squared distance to target
            float bestDist = float.MaxValue;
            Dir bestDir = candidates[0];
            foreach (var d in candidates)
            {
                Vector2Int nxt = currentCell + DirectionUtil.ToVec(d);
                nxt = grid.WrapHorizontal(nxt);
                float dist = (nxt - target).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestDir = d;
                }
            }
            return bestDir;
        }

        private Vector2Int GetTargetCell()
        {
            if (CurrentState == GhostState.Scatter) return scatterCorner;
            if (CurrentState == GhostState.Chase)
            {
                if (pacman != null)
                {
                    var pc = grid.WorldToGrid(pacman.position);
                    return pc;
                }
                return scatterCorner;
            }
            if (CurrentState == GhostState.Eaten)
            {
                if (gateCell.HasValue) return gateCell.Value;
                return scatterCorner;
            }
            // Frightened handled elsewhere (random choice). Return current to reduce bias.
            return currentCell;
        }

        public void SetFrightened()
        {
            if (CurrentState == GhostState.Eaten) return; // cannot frighten when eyes
            CurrentState = GhostState.Frightened;
        }

        public void SetChase()
        {
            if (CurrentState == GhostState.Eaten) return; // wait until reaches gate
            CurrentState = GhostState.Chase;
        }

        public void SetScatter()
        {
            if (CurrentState == GhostState.Eaten) return;
            CurrentState = GhostState.Scatter;
        }

        public void SetEaten()
        {
            CurrentState = GhostState.Eaten;
        }

        private void LateUpdate()
        {
            // If Eaten and reached gate, restore to Chase
            if (CurrentState == GhostState.Eaten && gateCell.HasValue)
            {
                if (currentCell == gateCell.Value)
                {
                    SetChase();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawTargets) return;
            Gizmos.color = Color.red;
            Vector3 tp = grid != null ? grid.GridToWorld(scatterCorner.x, scatterCorner.y) : (Vector3)(Vector2)scatterCorner;
            Gizmos.DrawWireSphere(tp, 0.3f);
        }

        public void ResetToSpawn()
        {
            transform.position = spawnPosition;
            currentCell = grid.WorldToGrid(transform.position);
            targetWorldPos = grid.GridToWorld(currentCell.x, currentCell.y);
            currentDir = Dir.Left;
            CurrentState = GhostState.Scatter;
            stateTimer = scatterDurations[0];
            cycleIndex = 0;
        }
    }
}

