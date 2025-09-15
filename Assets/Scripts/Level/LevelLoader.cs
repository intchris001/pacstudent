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

        [Header("Prefabs")]
        public GameObject wallPrefab;
        public GameObject gatePrefab; // ghost gate 'g'
        public GameObject pelletPrefab; // 's'
        public GameObject powerPelletPrefab; // 'p'

        [Header("Parents (created at runtime if null)")]
        public Transform wallsParent;
        public Transform collectiblesParent;

        // Parsed map (full combined from numeric+letter blocks)
        private char[,] letterMap;

        // Pellet registry for quick consumption
        private Dictionary<Vector2Int, GameObject> pellets = new();
        private Dictionary<Vector2Int, GameObject> powerPellets = new();

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
            LoadAndBuild();
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

            // Ensure parents
            if (wallsParent == null)
            {
                GameObject go = new GameObject("Walls");
                wallsParent = go.transform;
            }
            if (collectiblesParent == null)
            {
                GameObject go = new GameObject("Collectibles");
                collectiblesParent = go.transform;
            }

            // Clear existing children
            foreach (Transform t in wallsParent) Destroy(t.gameObject);
            foreach (Transform t in collectiblesParent) Destroy(t.gameObject);

            // Initialize LevelGrid service
            var gridSvc = FindObjectOfType<LevelGrid>();
            if (gridSvc == null)
            {
                gridSvc = new GameObject("LevelGrid").AddComponent<LevelGrid>();
            }
            gridSvc.Initialize(letterMap, cellSize, originWorld);

            // Instantiate tiles
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = letterMap[y, x];
                    Vector3 wp = gridSvc.GridToWorld(x, y);
                    switch (c)
                    {
                        case 'x': // outside corner
                        case 'o': // outside wall
                        case 'c': // inside corner
                        case 'i': // inside wall
                        case 't': // T junction
                            if (wallPrefab != null)
                            {
                                var go = Instantiate(wallPrefab, wp, Quaternion.identity, wallsParent);
                                go.name = $"Wall_{x}_{y}_{c}";
                            }
                            break;
                        case 'g': // ghost gate (ghosts passable, pacman not)
                            if (gatePrefab != null)
                            {
                                var go = Instantiate(gatePrefab, wp, Quaternion.identity, wallsParent);
                                go.name = $"Gate_{x}_{y}";
                            }
                            break;
                        case 's': // pellet
                            if (pelletPrefab != null)
                            {
                                var go = Instantiate(pelletPrefab, wp, Quaternion.identity, collectiblesParent);
                                go.name = $"Pellet_{x}_{y}";
                                pellets[new Vector2Int(x, y)] = go;
                            }
                            break;
                        case 'p': // power pellet
                            if (powerPelletPrefab != null)
                            {
                                var go = Instantiate(powerPelletPrefab, wp, Quaternion.identity, collectiblesParent);
                                go.name = $"Power_{x}_{y}";
                                powerPellets[new Vector2Int(x, y)] = go;
                            }
                            break;
                        default:
                            // 'e' empty or other tokens: do nothing
                            break;
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

            List<string[]> numericRows = new();
            List<string[]> letterRows = new();

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
    }
}

