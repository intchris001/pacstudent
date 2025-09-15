#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Animations;
using System.IO;

public static class A3OneClickSetup
{
    [MenuItem("PacStudent/One-Click Setup and Run (A3)")]
    public static void OneClick()
    {
        try
        {
            // 1) Generate sprites and audio
            A3SpriteGenerator.GenerateAll();
            A3AudioGenerator.GenerateAllAudio();
            AssetDatabase.Refresh();

            // 2) Setup SampleScene objects (GameManager, AudioManager, LevelGenerator, PacStudent, Ghosts, ManualLevel, Camera)
            SceneSetupWizard.SetupA3Scene();

            // 3) Build Animators and assign to scene objects
            A3AnimatorBuilder.BuildAnimators();
            A3AnimatorBuilder.AssignToScene();

            // 4) Build Manual Level using 8 sprites
            A3ManualBuilder.BuildManual();

            // 5) Assign sprites to LevelGenerator automatically
            AssignLevelGeneratorSprites();

            // 6) Assign audio clips to AudioManager automatically
            AssignAudioToManager();

            // 7) Ensure Build Settings include StartScene and SampleScene (create StartScene if missing)
            EnsureScenesAndBuildSettings();

            // 8) Save current scene and run
            EditorSceneManager.SaveOpenScenes();
            EditorApplication.isPlaying = true;

            Debug.Log("A3 One-Click: Completed. Game is now running.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("A3 One-Click failed: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private static void AssignLevelGeneratorSprites()
    {
        var gen = Object.FindObjectOfType<PacmanGame.Level.LevelGenerator>();
        if (gen == null) { Debug.LogWarning("LevelGenerator not found in scene."); return; }
        var so = new SerializedObject(gen);
        string wallsDir = "Assets/Art/Sprites/Walls";
        so.FindProperty("s1_OutsideCorner").objectReferenceValue = LoadSprite(wallsDir+"/1_outside_corner.png");
        so.FindProperty("s2_OutsideWall").objectReferenceValue = LoadSprite(wallsDir+"/2_outside_wall.png");
        so.FindProperty("s3_InsideCorner").objectReferenceValue = LoadSprite(wallsDir+"/3_inside_corner.png");
        so.FindProperty("s4_InsideWall").objectReferenceValue = LoadSprite(wallsDir+"/4_inside_wall.png");
        so.FindProperty("s5_PelletSpot").objectReferenceValue = LoadSprite(wallsDir+"/5_pellet_spot.png");
        so.FindProperty("s6_PowerSpot").objectReferenceValue = LoadSprite(wallsDir+"/6_power_spot.png");
        so.FindProperty("s7_TJunction").objectReferenceValue = LoadSprite(wallsDir+"/7_t_junction.png");
        so.FindProperty("s8_GhostExit").objectReferenceValue = LoadSprite(wallsDir+"/8_ghost_exit.png");
        so.ApplyModifiedProperties();
    }

    private static void AssignAudioToManager()
    {
        var am = Object.FindObjectOfType<PacmanGame.Audio.AudioManager>();
        if (am == null)
        {
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioSource>();
            am = go.AddComponent<PacmanGame.Audio.AudioManager>();
        }
        string dir = "Assets/Audio Clips";
        var so = new SerializedObject(am);
        so.FindProperty("introMusic").objectReferenceValue = LoadClip(dir+"/intro.wav");
        so.FindProperty("startSceneMusic").objectReferenceValue = LoadClip(dir+"/startscene.wav");
        so.FindProperty("ghostsNormalMusic").objectReferenceValue = LoadClip(dir+"/ghosts_normal.wav");
        so.FindProperty("ghostsFrightenedMusic").objectReferenceValue = LoadClip(dir+"/ghosts_fright.wav");
        so.FindProperty("ghostsDeadPresentMusic").objectReferenceValue = LoadClip(dir+"/ghosts_dead.wav");
        so.FindProperty("sfxMove").objectReferenceValue = LoadClip(dir+"/sfx_move.wav");
        so.FindProperty("sfxPellet").objectReferenceValue = LoadClip(dir+"/sfx_pellet.wav");
        so.FindProperty("sfxWall").objectReferenceValue = LoadClip(dir+"/sfx_wall.wav");
        so.FindProperty("sfxDeath").objectReferenceValue = LoadClip(dir+"/sfx_death.wav");
        so.ApplyModifiedProperties();
    }

    private static void EnsureScenesAndBuildSettings()
    {
        // Ensure StartScene exists (minimal scene with StartSceneMusic)
        string scenesDir = "Assets/Scenes";
        string startPath = Path.Combine(scenesDir, "StartScene.unity");
        if (!File.Exists(startPath))
        {
            Directory.CreateDirectory(scenesDir);
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            var go = new GameObject("StartSceneController");
            go.AddComponent<StartSceneMusic>();
            EditorSceneManager.SaveScene(newScene, startPath);
            EditorSceneManager.CloseScene(newScene, true);
        }
        // Ensure SampleScene exists already in project; if not, skip (user likely has it)
        string samplePath = Path.Combine(scenesDir, "SampleScene.unity");

        // Update Build Settings order: StartScene first, then SampleScene if present
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        if (File.Exists(startPath)) list.Add(new EditorBuildSettingsScene(startPath, true));
        if (File.Exists(samplePath)) list.Add(new EditorBuildSettingsScene(samplePath, true));
        if (list.Count > 0) EditorBuildSettings.scenes = list.ToArray();
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static AudioClip LoadClip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
}
#endif

