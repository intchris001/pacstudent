#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Diagnostics;

public static class A3StableSetup
{
    [MenuItem("PacStudent/One-Click Setup (STABLE)")]
    public static void OneClick()
    {
        try
        {
            if (EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("A3 One-Click: Cannot run while in Play Mode. Please exit Play Mode first.");
                return;
            }

            EditorUtility.DisplayProgressBar("A3 One-Click", "Preparing...", 0.01f);
            AssetDatabase.DisallowAutoRefresh();
            var sw = new Stopwatch(); sw.Start();

            EditorUtility.DisplayProgressBar("A3 One-Click", "Cleaning scene...", 0.1f);
            CleanScene();

            EditorUtility.DisplayProgressBar("A3 One-Click", "Ensuring camera...", 0.3f);
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.nearClipPlane = -50; cam.farClipPlane = 50;
            }
            cam.orthographic = true;

            EditorUtility.DisplayProgressBar("A3 One-Click", "Creating AudioManager...", 0.4f);
            var audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<AudioSource>();
            audioGo.AddComponent<PacmanGame.Audio.AudioManager>();

            EditorUtility.DisplayProgressBar("A3 One-Click", "Creating GameManager...", 0.5f);
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<PacmanGame.Core.GameManager>();

            EditorUtility.DisplayProgressBar("A3 One-Click", "Setting up LevelGenerator...", 0.6f);
            var lgGo = new GameObject("LevelGenerator");
            var lg = lgGo.AddComponent<PacmanGame.Level.LevelGenerator>();
            string wallsDir = "Assets/Art/Sprites/Walls";
            lg.s1_OutsideCorner = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/1_outside_corner.png");
            lg.s2_OutsideWall   = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/2_outside_wall.png");
            lg.s3_InsideCorner  = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/3_inside_corner.png");
            lg.s4_InsideWall    = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/4_inside_wall.png");
            lg.s5_PelletSpot    = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/5_pellet_spot.png");
            lg.s6_PowerSpot     = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/6_power_spot.png");
            lg.s7_TJunction     = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/7_t_junction.png");
            var so = new SerializedObject(lg);
            so.FindProperty("s8_GhostExit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/8_ghost_exit.png");
            so.ApplyModifiedProperties();
            lg.topLeftWorld = new Vector2(-13.5f, 14f);

            EditorUtility.DisplayProgressBar("A3 One-Click", "Building level grid from generator...", 0.7f);
            var loaderGo = new GameObject("LevelLoader");
            var loader = loaderGo.AddComponent<PacmanGame.Level.LevelLoader>();
            loader.pelletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pellet.prefab");
            loader.powerPelletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerPellet.prefab");
            loader.BuildFromLetterMap(lg.GetLetterMapFromGenerated(), lg.tileWorldSize, lg.topLeftWorld);
            var gridSvc = Object.FindFirstObjectByType<PacmanGame.Level.LevelGrid>();
            if (gridSvc != null) gridSvc.enableHorizontalWrap = true;

            EditorUtility.DisplayProgressBar("A3 One-Click", "Spawning Pacman & ghosts...", 0.8f);
            var pac = new GameObject("Pacman");
            pac.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_left_0.png");
            pac.AddComponent<Animator>(); // Add component, but do not configure it to prevent stalls
            pac.AddComponent<CircleCollider2D>();
            var pc = pac.AddComponent<PacmanGame.Player.PacmanController>();
            pc.animationFps = 8f;
            pc.spritesUp = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_up_0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_up_1.png")
            };
            pc.spritesDown = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_down_0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_down_1.png")
            };
            pc.spritesLeft = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_left_0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_left_1.png")
            };
            pc.spritesRight = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_right_0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/pac_right_1.png")
            };
            float fullWidth = 28 * lg.tileWorldSize;
            float fullHeight = 29 * lg.tileWorldSize;
            Vector3 center = (Vector3)lg.topLeftWorld + new Vector3(fullWidth * 0.5f, -fullHeight * 0.5f);

            pac.transform.position = center + new Vector3(-3f, 0f, 0f);
            gm.pacmanSpawn = pac.transform;

            var ghosts = new System.Collections.Generic.List<PacmanGame.Ghosts.GhostController>();
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject($"Ghost_{i+1}");
                g.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Characters/ghost_body.png");
                g.AddComponent<Animator>(); // Add component, but do not configure it
                var gc = g.AddComponent<PacmanGame.Ghosts.GhostController>();
                g.transform.position = center + new Vector3(1f + i*0.7f, 0f, 0f);
                ghosts.Add(gc);
            }
            gm.ghosts = ghosts;

            EditorUtility.DisplayProgressBar("A3 One-Click", "Finishing setup...", 0.9f);
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            var fitter = cam.GetComponent<PacmanGame.Util.CameraAutoFit>();
            if (fitter == null) fitter = cam.gameObject.AddComponent<PacmanGame.Util.CameraAutoFit>();
            fitter.extraPaddingWorld = 0.25f;
            fitter.FitNow();

            EditorSceneManager.SaveOpenScenes();
            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };

            UnityEngine.Debug.Log($"A3 One-Click: Setup complete in {sw.ElapsedMilliseconds} ms. Entering Play Mode.");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("A3 One-Click failed: " + ex.Message + "\n" + ex.StackTrace);
        }
        finally
        {
            AssetDatabase.AllowAutoRefresh();
            EditorUtility.ClearProgressBar();
        }
    }

    private static void CleanScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        string[] removeExact = {
            "GameManager", "AudioManager", "LevelGenerator", "ManualLevel", "HUD",
            "LevelLoader", "LevelGrid", "Walls", "Collectibles", "Pacman",
            "PelletTemplate", "PowerPelletTemplate"
        };
        string[] removePrefix = { "Ghost_", "Corner", "GeneratedLevel" };
        System.Type[] removeComponents = {
            typeof(PacmanGame.Core.GameManager), typeof(PacmanGame.Audio.AudioManager),
            typeof(PacmanGame.Level.LevelGenerator), typeof(PacmanGame.Level.LevelLoader),
            typeof(PacmanGame.Level.LevelGrid), typeof(PacStudentDemoMover),
            typeof(PacmanGame.Player.PacmanController)
        };

        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null) TraverseAndCollect(root.transform, toDelete, removeExact, removePrefix, removeComponents);
        }

        foreach (var go in toDelete) { if (go != null) Object.DestroyImmediate(go); }
    }

    private static void TraverseAndCollect(Transform t, System.Collections.Generic.List<GameObject> toDelete, string[] exact, string[] prefix, System.Type[] components)
    {
        bool shouldDelete = false;
        foreach (var n in exact) { if (t.name == n) { shouldDelete = true; break; } }
        if (!shouldDelete) { foreach (var p in prefix) { if (t.name.StartsWith(p)) { shouldDelete = true; break; } } }
        if (!shouldDelete) { foreach (var c in components) { if (t.GetComponent(c) != null) { shouldDelete = true; break; } } }
        
        if (shouldDelete)
        {
            toDelete.Add(t.gameObject);
            return; 
        }

        foreach (Transform child in t) { TraverseAndCollect(child, toDelete, exact, prefix, components); }
    }
}
#endif
