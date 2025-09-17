using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PacmanGame.Level
{
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        [Header("CSV Source")]
        [Tooltip("Filename in StreamingAssets to load, e.g., LevelMap.csv")]
        public string csvFileName = "LevelMap.csv";

        [Header("Grid Config")]
        public float cellSize = 1f;
        public Vector2 originWorld = Vector2.zero; // world position of grid (0,0) cell center

        [Header("Mirroring")]
        [Tooltip("If your CSV provides only the top-left quadrant, mirror it to a full symmetric maze.")]
        public bool mirrorTLToFull = true;

        [Header("Prefabs (optional for collectibles)")]
        public GameObject wallPrefab; // unused when using sprite-based walls
        public GameObject gatePrefab; // unused when using sprite-based walls
        public GameObject pelletPrefab; // 's'
        public GameObject powerPelletPrefab; // 'p'

        [Header("Wall Sprites (assigned by One-Click)")]
        public Sprite s1_OutsideCorner;
        public Sprite s2_OutsideWall;
        public Sprite s3_InsideCorner;
        public Sprite s4_InsideWall;
        public Sprite s5_PelletSpot;
        public Sprite s6_PowerSpot;
        public Sprite s7_TJunction;
        public Sprite s8_GhostExit;

        [Header("Parents (created at runtime if null)")]
        public Transform wallsParent;
        public Transform collectiblesParent;

        // Parsed map (full combined from numeric+letter blocks)
        private char[,] letterMap;

        // Pellet registry for quick consumption
        private Dictionary<Vector2Int, GameObject> pellets = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Vector2Int, GameObject> powerPellets = new Dictionary<Vector2Int, GameObject>();

        public int Width => letterMap?.GetLength(1) ?? 0;
        public int Height => letterMap?.GetLength(0) ?? 0;
        public int RemainingCollectibles => (pellets?.Count ?? 0) + (powerPellets?.Count ?? 0);

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
            if (!string.IsNullOrEmpty(csvFileName))
                LoadAndBuild();
        }

        // Build from an in-memory letter map (bypass CSV)
        public void BuildFromLetterMap(char[,] map, float cellSize, Vector2 origin)
        {
            if (map == null) { Debug.LogError("LevelLoader: BuildFromLetterMap got null map"); return; }
            this.letterMap = map;
            this.cellSize = cellSize;
            this.originWorld = origin;

            pellets.Clear(); powerPellets.Clear();
            EnsureParents();
            ClearChildren();
            InitLevelGrid();
            SpawnCollectiblesFromMap();
        }

        public void LoadAndBuild()
        {
            pellets.Clear();
            powerPellets.Clear();

            string path = Path.Combine(Application.streamingAssetsPath, csvFileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"LevelLoader: CSV not found at {path}");
                return;
            }
            string raw = File.ReadAllText(path);
            letterMap = ParseFullMap(raw);
            if (letterMap == null)
            {
                Debug.LogError("LevelLoader: Failed to parse map from CSV.");
                return;
            }

            // If CSV only provides top-left quadrant, mirror it to full map when enabled
            if (mirrorTLToFull)
            {
                letterMap = MirrorTopLeftToFull(letterMap);
            }

            EnsureParents();
            ClearChildren();
            var gridSvc = InitLevelGrid();

            // Instantiate tiles (walls/gate) using sprites; collectibles spawned separately.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = letterMap[y, x];
                    Vector3 wp = gridSvc.GridToWorld(x, y);

                    if (c == 'x' || c == 'o' || c == 'c' || c == 'i' || c == 't' || c == 'g')
                    {
                        // Prefer sprites; fallback to prefabs if provided
                        Sprite sprite = SpriteForLetter(c);
                        if (sprite != null)
                        {
                            var go = new GameObject($"Wall_{x}_{y}_{c}");
                            go.transform.SetParent(wallsParent, false);
                            var sr = go.AddComponent<SpriteRenderer>();
                            sr.sprite = sprite;
                            go.transform.position = wp;
                            float rotZ = ComputeRotationForLetter(x, y, c);
                            go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                        }
                        else
                        {
                            if (c == 'g' && gatePrefab != null)
                            {
                                var go = Instantiate(gatePrefab, wp, Quaternion.identity, wallsParent);
                                go.name = $"Gate_{x}_{y}";
                            }
                            else if (wallPrefab != null)
                            {
                                var go = Instantiate(wallPrefab, wp, Quaternion.identity, wallsParent);
                                go.name = $"Wall_{x}_{y}_{c}";
                            }
                        }
                    }
                }
            }

            SpawnCollectiblesFromMap();
        }

        private void EnsureParents()
        {
            if (wallsParent == null)
            {
                GameObject go = GameObject.Find("Walls");
                if (go == null) go = new GameObject("Walls");
                wallsParent = go.transform;
            }
            if (collectiblesParent == null)
            {
                GameObject go = GameObject.Find("Collectibles");
                if (go == null) go = new GameObject("Collectibles");
                collectiblesParent = go.transform;
            }
        }

        private void ClearChildren()
        {
            if (wallsParent != null)
            {
                var list = new System.Collections.Generic.List<GameObject>();
                foreach (Transform t in wallsParent) list.Add(t.gameObject);
                foreach (var g in list) { if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); }
            }
            if (collectiblesParent != null)
            {
                var list = new System.Collections.Generic.List<GameObject>();
                foreach (Transform t in collectiblesParent) list.Add(t.gameObject);
                foreach (var g in list) { if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); }
            }
        }

        private LevelGrid InitLevelGrid()
        {
            var gridSvc = FindObjectOfType<LevelGrid>();
            if (gridSvc == null) gridSvc = new GameObject("LevelGrid").AddComponent<LevelGrid>();
            gridSvc.Initialize(letterMap, cellSize, originWorld);
            // Enable horizontal wrap so tunnels teleport across left/right edges
            gridSvc.enableHorizontalWrap = true;
            return gridSvc;
        }

        private void SpawnCollectiblesFromMap()
        {
            var gridSvc = FindObjectOfType<LevelGrid>();
            if (gridSvc == null || letterMap == null) return;
            // Only (re)spawn collectibles; walls are handled visually by LevelGenerator
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = letterMap[y, x];
                    Vector3 wp = gridSvc.GridToWorld(x, y);
                    switch (c)
                    {
                        case 's':
                            if (pelletPrefab != null)
                            {
                                var go = Instantiate(pelletPrefab, wp, Quaternion.identity, collectiblesParent);
                                go.name = $"Pellet_{x}_{y}"; go.SetActive(true);
                                pellets[new Vector2Int(x, y)] = go;
                            }
                            else
                            {
                                // Fallback: spawn a simple sprite-based pellet using configured sprites
                                var go = new GameObject($"Pellet_{x}_{y}");
                                go.transform.SetParent(collectiblesParent, false);
                                go.transform.position = wp;
                                var sr = go.AddComponent<SpriteRenderer>();
                                sr.sprite = s5_PelletSpot != null ? s5_PelletSpot : null;
                                pellets[new Vector2Int(x, y)] = go;
                            }
                            break;
                        case 'p':
                            if (powerPelletPrefab != null)
                            {
                                var go = Instantiate(powerPelletPrefab, wp, Quaternion.identity, collectiblesParent);
                                go.name = $"Power_{x}_{y}"; go.SetActive(true);
                                powerPellets[new Vector2Int(x, y)] = go;
                            }
                            else
                            {
                                var go = new GameObject($"Power_{x}_{y}");
                                go.transform.SetParent(collectiblesParent, false);
                                go.transform.position = wp;
                                var sr = go.AddComponent<SpriteRenderer>();
                                sr.sprite = s6_PowerSpot != null ? s6_PowerSpot : null;
                                powerPellets[new Vector2Int(x, y)] = go;
                            }
                            break;
                        default: break; // ignore other tiles
                    }
                }
            }
        }


        public bool TryConsumePellet(Vector2Int gridPos, out bool power)
        {
            power = false;
            if (pellets.TryGetValue(gridPos, out var pellet))
            {
                pellets.Remove(gridPos);
                if (pellet != null) Destroy(pellet);
                power = false;
                return true;
            }
            if (powerPellets.TryGetValue(gridPos, out var pgo))
            {
                powerPellets.Remove(gridPos);
                if (pgo != null) Destroy(pgo);
                power = true;
                return true;
            }
            return false;
        }

        private static char[,] ParseFullMap(string raw)
        {
            // The CSV contains a numeric block, a blank line, then a block of letters, then legend lines.
            var lines = raw.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            List<string[]> numericRows = new List<string[]>();
            List<string[]> letterRows = new List<string[]>();

            bool foundNumeric = false;
            bool collectingNumeric = false;
            bool collectingLetters = false;

            // Helpers
            bool IsNumericRow(string[] tokens)
            {
                foreach (var tk in tokens)
                {
                    var t = tk.Trim();
                    if (t == "") continue; // allow blanks
                    // digits only
                    for (int i = 0; i < t.Length; i++) if (!char.IsDigit(t[i])) return false;
                    // must be 0..8 per legend
                    if (!int.TryParse(t, out int n) || n < 0 || n > 8) return false;
                }
                return true;
            }
            bool IsLetterRow(string[] tokens)
            {
                foreach (var tk in tokens)
                {
                    var t = tk.Trim();
                    if (t == "") continue;
                    if (!(t == "e" || t == "x" || t == "o" || t == "c" || t == "i" || t == "s" || t == "p" || t == "t" || t == "g")) return false;
                }
                return true;
            }

            foreach (var line in lines)
            {
                var tokens = line.Split(',');
                bool emptyLine = string.IsNullOrWhiteSpace(line);

                if (!foundNumeric)
                {
                    if (!emptyLine && IsNumericRow(tokens))
                    {
                        collectingNumeric = true;
                        foundNumeric = true;
                        numericRows.Add(tokens);
                    }
                    continue;
                }

                if (collectingNumeric)
                {
                    if (!emptyLine && IsNumericRow(tokens))
                    {
                        numericRows.Add(tokens);
                        continue;
                    }
                    else
                    {
                        collectingNumeric = false;
                        // fall through to look for letters
                    }
                }

                if (!collectingLetters)
                {
                    if (!emptyLine && IsLetterRow(tokens))
                    {
                        collectingLetters = true;
                        letterRows.Add(tokens);
                    }
                    continue;
                }
                else
                {
                    if (!emptyLine && IsLetterRow(tokens))
                    {
                        letterRows.Add(tokens);
                    }
                    else
                    {
                        break; // legend reached
                    }
                }
            }

            if (numericRows.Count == 0 && letterRows.Count == 0) return null;

            // Build maps
            char[,] numMap = null;
            if (numericRows.Count > 0)
            {
                int h = numericRows.Count;
                int w = numericRows[0].Length;
                numMap = new char[h, w];
                for (int y = 0; y < h; y++)
                {
                    var row = numericRows[y];
                    for (int x = 0; x < w; x++)
                    {
                        string t = x < row.Length ? row[x].Trim() : "";
                        if (string.IsNullOrEmpty(t)) { numMap[y, x] = 'e'; continue; }
                        int n = int.Parse(t);
                        numMap[y, x] = n switch
                        {
                            0 => 'e',
                            1 => 'x',
                            2 => 'o',
                            3 => 'c',
                            4 => 'i',
                            5 => 's',
                            6 => 'p',
                            7 => 't',
                            8 => 'g',
                            _ => 'e'
                        };
                    }
                }
            }

            char[,] letMap = null;
            if (letterRows.Count > 0)
            {
                int h = letterRows.Count;
                int w = letterRows[0].Length;
                letMap = new char[h, w];
                for (int y = 0; y < h; y++)
                {
                    var row = letterRows[y];
                    for (int x = 0; x < w; x++)
                    {
                        string t = x < row.Length ? row[x].Trim() : "";
                        letMap[y, x] = string.IsNullOrEmpty(t) ? 'e' : t[0];
                    }
                }
            }

            if (letMap != null)
            {
                // Prefer letter map if present (it's explicit and already uses tokens)
                return letMap;



            }
            else if (numMap != null)
            {
                return numMap;
            }
            else
            {
                return null;
            }
        }

        private char[,] MirrorTopLeftToFull(char[,] tl)
        {
            if (tl == null) return null;
            int h = tl.GetLength(0);
            int w = tl.GetLength(1);
            if (h == 0 || w == 0) return tl;
            int omitBottom = 1; // omit overlap row when mirroring vertically
            int fullW = w * 2;
            int fullH = (h * 2) - omitBottom;
            var map = new char[fullH, fullW];
            // place top-left
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    map[y, x] = tl[y, x];
                }
            }
            // mirror horizontally to top-right
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int mx = (fullW - 1) - x;
                    map[y, mx] = tl[y, x];
                }
            }
            // mirror vertically to bottom half (skip bottom overlap row)
            for (int y = 0; y < h - omitBottom; y++)
            {
                int my = (fullH - 1) - y;
                for (int x = 0; x < fullW; x++)
                {
                    map[my, x] = map[y, x];
                }
            }
            return map;
        }


        private Sprite SpriteForLetter(char c)
        {
            switch (c)
            {
                case 'x': return s1_OutsideCorner;
                case 'o': return s2_OutsideWall;
                case 'c': return s3_InsideCorner;
                case 'i': return s4_InsideWall;
                case 't': return s7_TJunction;
                case 'g': return s8_GhostExit;
                case 's': return s5_PelletSpot;
                case 'p': return s6_PowerSpot;
                default: return null;
            }
        }

        private float ComputeRotationForLetter(int x, int y, char c)
        {
            bool InBoundsL(int xx, int yy) => (xx >= 0 && xx < Width && yy >= 0 && yy < Height);
            char GetL(int xx, int yy) => InBoundsL(xx, yy) ? letterMap[yy, xx] : 'e';
            bool IsOutside(char ch) => (ch == 'x' || ch == 'o' || ch == 't');
            bool IsInside(char ch) => (ch == 'c' || ch == 'i' || ch == 't' || ch == 'g');

            char up = GetL(x, y - 1);
            char down = GetL(x, y + 1);
            char left = GetL(x - 1, y);
            char right = GetL(x + 1, y);

            switch (c)
            {
                case 'o':
                    if (IsOutside(left) && IsOutside(right)) return 0f;
                    return 90f;
                case 'x':
                {



                    bool u = IsOutside(up), d = IsOutside(down), l = IsOutside(left), r = IsOutside(right);
                    if (r && d) return 0f;
                    if (r && u) return 270f;
                    if (l && d) return 90f;
                    if (l && u) return 180f;
                    return 0f;
                }
                case 't':
                {
                    bool u = IsOutside(up), d = IsOutside(down), l = IsOutside(left), r = IsOutside(right);
                    if (!u && d && l && r) return 0f;
                    if (!r && d && l && u) return 90f;
                    if (!d && u && l && r) return 180f;
                    if (!l && d && u && r) return 270f;
                    return 0f;
                }
                case 'i':
                    if (IsInside(left) && IsInside(right)) return 0f;
                    return 90f;
                case 'c':
                {
                    bool u = IsInside(up), d = IsInside(down), l = IsInside(left), r = IsInside(right);
                    if (r && d) return 0f;
                    if (r && u) return 270f;
                    if (l && d) return 90f;
                    if (l && u) return 180f;
                    return 0f;
                }
                case 'g':
                    if (IsInside(left) && IsInside(right)) return 0f;
                    return 90f;
                default:
                    return 0f;
            }
        }
    }
}
