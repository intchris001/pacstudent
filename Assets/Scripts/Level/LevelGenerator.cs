using System.Collections.Generic;
using UnityEngine;

namespace PacmanGame.Level
{
    // Assessment 3 - 100% HD task: Procedurally generate the level from the provided 2D array and your 8 sprites.
    // This script expects exactly 8 sprites (1..8) matching the legend. Rotation is computed from neighbours.
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Sprites per Legend Index")]
        [Tooltip("1 - Outside corner")] public Sprite s1_OutsideCorner;
        [Tooltip("2 - Outside wall")] public Sprite s2_OutsideWall;
        [Tooltip("3 - Inside corner")] public Sprite s3_InsideCorner;
        [Tooltip("4 - Inside wall")] public Sprite s4_InsideWall;
        [Tooltip("5 - Standard pellet spot")] public Sprite s5_PelletSpot;
        [Tooltip("6 - Power pellet spot")] public Sprite s6_PowerSpot;
        [Tooltip("7 - T junction")] public Sprite s7_TJunction;
        [Tooltip("8 - Ghost exit wall")][SerializeField] private Sprite s8_GhostExit;

        [Header("Generation Settings")]
        public float tileWorldSize = 1f; // Each sprite's size in world units
        public Vector2 topLeftWorld = Vector2.zero; // Where to place [0,0]
        public string manualLevelRootName = "ManualLevel"; // Will be deleted in Start
        public string generatedRootName = "GeneratedLevel";

        [Header("Parents (optional)")]
        public Transform generatedParent;

        private int[,] levelMapTopLeft = new int[,]
        {
            {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
            {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
            {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
            {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
            {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
            {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
            {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
            {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
            {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
            {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
            {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
            {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
            {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
            {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
            {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
        };

        private Transform root;
        private int[,] fullMap; // Assembled full level (mirrored)

        private void Start()
        {
            // Delete manual level root if present
            var manual = GameObject.Find(manualLevelRootName);
            if (manual != null) Destroy(manual);

            Generate();
        }

        public void Generate()
        {
            if (generatedParent == null)
            {
                var go = new GameObject(generatedRootName);
                root = go.transform;
            }
            else
            {
                // Clear previous
                foreach (Transform c in generatedParent) Destroy(c.gameObject);
                root = generatedParent;
            }

            BuildFullMap();
            InstantiateFromFullMap();
            FitCamera();
        }

        private void BuildFullMap()
        {
            // Build top-left quadrant from levelMapTopLeft, then mirror to others.
            int h = levelMapTopLeft.GetLength(0);
            int w = levelMapTopLeft.GetLength(1);

            // We need to omit the bottom row when vertically mirroring so only a single empty middle row remains.
            int omitBottom = 1; // omit the last row when mirroring vertical

            int fullW = w * 2;
            int fullH = (h * 2) - omitBottom;
            fullMap = new int[fullH, fullW];

            // Helper to set with bounds check
            void Set(int y, int x, int v)
            {
                if (y >= 0 && y < fullH && x >= 0 && x < fullW) fullMap[y, x] = v;
            }

            // Place top-left
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Set(y, x, levelMapTopLeft[y, x]);
                }
            }
            // Mirror horizontally to top-right
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int mx = (fullW - 1) - x;
                    Set(y, mx, MirrorTile(levelMapTopLeft[y, x], horizontal:true));
                }
            }
            // Mirror vertically to bottom halves (skip bottom row of TL when mirroring)
            for (int y = 0; y < h - omitBottom; y++)
            {
                int my = (fullH - 1) - y;
                for (int x = 0; x < fullW; x++)
                {
                    int src = fullMap[y, x];
                    Set(my, x, MirrorTile(src, horizontal:false));
                }
            }
        }

        private int MirrorTile(int v, bool horizontal)
        {
            // For numeric legend, mirroring doesn't change type index; rotation handled later by neighbour analysis
            return v;
        }

        private void InstantiateFromFullMap()
        {
            int H = fullMap.GetLength(0);
            int W = fullMap.GetLength(1);
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int v = fullMap[y, x];
                    if (v == 0) continue; // empty

                    var go = new GameObject($"T_{y}_{x}_{v}");
                    go.transform.SetParent(root, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteFor(v);

                    float rotZ = ComputeRotationFor(x, y, v);
                    go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                    go.transform.position = GridToWorld(x, y);
                }
            }
        }

        private float ComputeRotationFor(int x, int y, int v)
        {
            // Define connectivity sets
            bool IsOutside(int t) => (t == 1 || t == 2 || t == 7);
            bool IsInside(int t) => (t == 3 || t == 4 || t == 7 || t == 8);

            int up = Get(y - 1, x);
            int down = Get(y + 1, x);
            int left = Get(y, x - 1);
            int right = Get(y, x + 1);

            switch (v)
            {
                case 2: // Outside wall - horizontal if neighbours outside on L/R, else vertical
                    if (IsOutside(left) && IsOutside(right)) return 0f;
                    return 90f;
                case 1: // Outside corner - connects two outside sides
                {
                    bool u = IsOutside(up); bool d = IsOutside(down); bool l = IsOutside(left); bool r = IsOutside(right);
                    // default 0: top-left corner connects right+down
                    if (r && d) return 0f;
                    if (r && u) return 270f;
                    if (l && d) return 90f;
                    if (l && u) return 180f;
                    return 0f;
                }
                case 7: // T junction - 3 connections in outside set; rotation such that the missing side points up by default
                {
                    bool u = IsOutside(up); bool d = IsOutside(down); bool l = IsOutside(left); bool r = IsOutside(right);
                    // Find missing side
                    if (!u && d && l && r) return 0f; // open down
                    if (!r && d && l && u) return 90f; // open left
                    if (!d && u && l && r) return 180f; // open up
                    if (!l && d && u && r) return 270f; // open right
                    return 0f;
                }
                case 4: // Inside wall
                    if (IsInside(left) && IsInside(right)) return 0f;
                    return 90f;
                case 3: // Inside corner
                {
                    bool u = IsInside(up); bool d = IsInside(down); bool l = IsInside(left); bool r = IsInside(right);
                    // default 0: top-left inside corner connects right+down
                    if (r && d) return 0f;
                    if (r && u) return 270f;
                    if (l && d) return 90f;
                    if (l && u) return 180f;
                    return 0f;
                }
                case 8: // Ghost exit - usually horizontal
                    if (IsInside(left) && IsInside(right)) return 0f;
                    return 90f;
                case 5: // pellet spot
                case 6: // power spot
                default:
                    return 0f;
            }
        }

        private int Get(int y, int x)
        {
            if (fullMap == null) return 0;
            int H = fullMap.GetLength(0);
            int W = fullMap.GetLength(1);
            if (y < 0 || y >= H || x < 0 || x >= W) return 0;
            return fullMap[y, x];
        }

        private Sprite SpriteFor(int v)
        {
            return v switch
            {
                1 => s1_OutsideCorner,
                2 => s2_OutsideWall,
                3 => s3_InsideCorner,
                4 => s4_InsideWall,
                5 => s5_PelletSpot,
                6 => s6_PowerSpot,
                7 => s7_TJunction,
                8 => s8_GhostExit,
                _ => null,
            };
        }

        private Vector3 GridToWorld(int x, int y)
        {
            // [0,0] at top-left, y increases downward, match topLeftWorld
            return new Vector3(topLeftWorld.x + (x + 0.5f) * tileWorldSize,
                               topLeftWorld.y - (y + 0.5f) * tileWorldSize,
                               0f);
        }

        private void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            int H = fullMap.GetLength(0);
            int W = fullMap.GetLength(1);
            Vector3 center = GridToWorld(W / 2, H / 2);
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

            float halfWidth = (W * tileWorldSize) * 0.5f;
            float halfHeight = (H * tileWorldSize) * 0.5f;
            float aspect = cam.aspect;
            float sizeForHeight = halfHeight;
            float sizeForWidth = halfWidth / aspect;
            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) + 0.5f;
        }
    }
}

