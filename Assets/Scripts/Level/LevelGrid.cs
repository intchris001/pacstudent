using System;
using System.Collections.Generic;
using UnityEngine;

namespace PacmanGame.Level
{
    // Centralized grid data and helpers for coordinate conversion and passability checks.
    public class LevelGrid : MonoBehaviour
    {
        public static LevelGrid Instance { get; private set; }

        [Header("Grid/World Mapping")]
        [SerializeField] private float cellSize = 1f; // Unity units per grid cell
        [SerializeField] private Vector2 gridOriginWorld = Vector2.zero; // world position of grid (0,0) cell center

        // Dimensions (width = columns, height = rows)
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Raw map content from CSV's letter section
        private char[,] letterMap; // e,x,o,c,i,s,p,g,t

        // Passability caches
        private bool[,] passableForPacman;
        private bool[,] passableForGhost;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = false;

        public void Initialize(char[,] letterMap, float cellSize, Vector2 origin)
        {
            this.letterMap = letterMap;
            this.cellSize = cellSize;
            this.gridOriginWorld = origin;
            Width = letterMap.GetLength(1);
            Height = letterMap.GetLength(0);

            passableForPacman = new bool[Height, Width];
            passableForGhost = new bool[Height, Width];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = letterMap[y, x];
                    bool isWall = c == 'x' || c == 'o' || c == 'c' || c == 'i' || c == 't';
                    bool isGate = c == 'g';
                    bool walkable = !isWall && !isGate; // gate is not walkable for Pacman
                    passableForPacman[y, x] = walkable;

                    // Ghosts cannot pass walls; gate is special: ghosts can pass outward, and when eaten they can pass inward.
                    // For generic pathfinding allow ghosts to consider gate as passable; runtime checks handle direction/state constraints.
                    passableForGhost[y, x] = !isWall; // gate considered passable for path planning
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public Vector2 GridToWorld(int x, int y)
        {
            // Grid (0,0) is top-left in CSV. Our internal storage uses [row=y, col=x] where y increases downwards.
            // We'll place (0,0) at top-left by mapping yDown to world with negative Y.
            Vector2 centerOffset = new Vector2(0.5f, -0.5f);
            return gridOriginWorld + new Vector2(x, -y) * cellSize + centerOffset * cellSize;
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector2 local = (Vector2)worldPos - gridOriginWorld;
            int x = Mathf.FloorToInt(local.x / cellSize);
            int yDown = Mathf.FloorToInt(-local.y / cellSize);
            return new Vector2Int(x, yDown);
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsWalkableForPacman(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return passableForPacman[y, x];
        }

        public bool IsWalkableForGhost(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return passableForGhost[y, x];
        }

        public char GetTileChar(int x, int y)
        {
            if (!InBounds(x, y)) return ' ';
            return letterMap[y, x];
        }

        public IEnumerable<Vector2Int> GetNeighbors4(int x, int y)
        {
            // left, up, right, down relative to top-left grid with y increasing downwards
            var dirs = new Vector2Int[] { new(-1, 0), new(0, -1), new(1, 0), new(0, 1) };
            foreach (var d in dirs)
            {
                int nx = x + d.x;
                int ny = y + d.y;
                if (InBounds(nx, ny))
                    yield return new Vector2Int(nx, ny);
            }
        }

        public Vector2Int WrapHorizontal(Vector2Int grid)
        {
            // Allow wrap horizontally through tunnels if leaving bounds
            if (grid.x < 0) grid.x = Width - 1;
            else if (grid.x >= Width) grid.x = 0;
            return grid;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || letterMap == null) return;
            Gizmos.color = Color.cyan;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector3 wp = GridToWorld(x, y);
                    Gizmos.DrawWireCube(wp, Vector3.one * cellSize * 0.98f);
                }
            }
        }
    }
}

