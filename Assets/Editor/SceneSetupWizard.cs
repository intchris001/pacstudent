#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PacmanGame.Core;
using PacmanGame.Level;
using PacmanGame.Audio;

public static class SceneSetupWizard
{
    [MenuItem("PacStudent/Setup A3 Scene (SampleScene)")]
    public static void SetupA3Scene()
    {
        // Create root helpers
        var manualRoot = new GameObject("ManualLevel");
        manualRoot.transform.position = Vector3.zero;

        // GameManager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();

        // AudioManager (with AudioSource)
        var audioGo = new GameObject("AudioManager");
        var src = audioGo.AddComponent<AudioSource>();
        audioGo.AddComponent<AudioManager>();

        // LevelGenerator
        var genGo = new GameObject("LevelGenerator");
        var gen = genGo.AddComponent<LevelGenerator>();
        gen.manualLevelRootName = manualRoot.name;
        gen.tileWorldSize = 1f;
        gen.topLeftWorld = new Vector2(-7.5f, 8.5f);

        // PacStudent
        var pacGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pacGo.name = "PacStudent";
        Object.DestroyImmediate(pacGo.GetComponent<MeshRenderer>()); // keep transform only
        var pacDemo = pacGo.AddComponent<PacStudentDemoMover>();
        pacGo.transform.position = new Vector3(-5f, 6f, 0f);

        // Create 4 corner points as empty objects for demo path
        var p0 = new GameObject("Corner0"); p0.transform.position = new Vector3(-5f, 6f, 0f);
        var p1 = new GameObject("Corner1"); p1.transform.position = new Vector3(0f, 6f, 0f);
        var p2 = new GameObject("Corner2"); p2.transform.position = new Vector3(0f, 1f, 0f);
        var p3 = new GameObject("Corner3"); p3.transform.position = new Vector3(-5f, 1f, 0f);
        pacDemo.cornerWorldPoints = new Vector3[] { p0.transform.position, p1.transform.position, p2.transform.position, p3.transform.position };

        // Ghosts (static placeholders)
        for (int i = 0; i < 4; i++)
        {
            var g = new GameObject($"Ghost_{i+1}");
            g.transform.position = new Vector3(-1f + i * 1.0f, 0f, 0f);
            g.AddComponent<PacmanGame.Ghosts.GhostController>();
        }

        // HUD placeholder (optional)
        var hud = new GameObject("HUD");

        // Camera config (orthographic)
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);

        Selection.activeGameObject = gmGo;
        Debug.Log("A3 Scene setup complete. Now: 1) Assign sprites for 8 tile types in LevelGenerator, 2) Import your audio clips and assign in AudioManager, 3) Manually build top-left quadrant under ManualLevel using only 8 sprites, 4) Ensure Animator Controllers per PDF.");
    }
}
#endif

