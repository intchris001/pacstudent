using UnityEngine;
using UnityEngine.UI;

using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
#endif

// --- RUNTIME LOGIC FOR 85HD_AutoMovement ---
[ExecuteAlways]
public class AutoMovementRuntime : MonoBehaviour
{
    // These points are calculated to match the top-left inner block of the level map
    // Z is set to -5f to ensure it renders in front of the level map (at z=0)
    public Vector3[] cornerWorldPoints = new Vector3[]
    {
        new Vector3(-12.5f, 14f, -5f), new Vector3(-7.5f, 14f, -5f), new Vector3(-7.5f, 10f, -5f), new Vector3(-12.5f, 10f, -5f)
    };
    public float speed = 3f;

    private GameObject pacStudent;
    private Animator animator;
    private int currentIndex = 0;
    private Vector3 startPos, endPos;
    private float travelTime, elapsed;

    void Start()
    {
        // With [ExecuteAlways], Start runs on scene load and on play. Clean up previous instance.
        var existing = GameObject.Find("PacStudent_Demo");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing);
            else DestroyImmediate(existing);
        }
        SetupPacStudent();
    }

    void Update()
    {
        if (pacStudent == null || !Application.isPlaying) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);
        pacStudent.transform.position = Vector3.Lerp(startPos, endPos, t);
        // Persistently set animator params in Update to override any unwanted state transitions
        Vector3 currentDir = (endPos - startPos).normalized;
        animator.SetFloat("MoveX", currentDir.x);
        animator.SetFloat("MoveY", currentDir.y);

        if (t >= 1f)
        {
            currentIndex = (currentIndex + 1) % cornerWorldPoints.Length;
            SetupNextSegment();
        }
    }

    private void SetupPacStudent()
    {
        pacStudent = new GameObject("PacStudent_Demo");
        pacStudent.transform.localScale = new Vector3(4f, 4f, 1f); // Make PacStudent much bigger to be visible in full map view
        pacStudent.AddComponent<SpriteRenderer>();
        animator = pacStudent.AddComponent<Animator>();
#if UNITY_EDITOR
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PacStudent/PacStudent.controller");
#endif
        SetupNextSegment();
        pacStudent.transform.position = startPos;
    }

    private void SetupNextSegment()
    {
        startPos = cornerWorldPoints[currentIndex];
        int next = (currentIndex + 1) % cornerWorldPoints.Length;
        endPos = cornerWorldPoints[next];
        travelTime = Vector3.Distance(startPos, endPos) / speed;
        elapsed = 0f;
    }

}


// --- RUNTIME LOGIC FOR Scene_StartMenu ---
public class StartMenuRuntime : MonoBehaviour
{
    void Start()
    {
        var canvasGo = new GameObject("Canvas_StartMenu");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        Text CreateText(string name, Vector2 anchor, Vector2 size, int fontSize, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvasGo.transform, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = content;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            return txt;
        }

        CreateText("Title", new Vector2(0.5f, 0.85f), new Vector2(600, 80), 44, "PacStudent");

        string[] scenes = new [] {"65C_AnimationPreview", "75D_ManualLevel", "85HD_AutoMovement", "100HD_ProceduralGameplay"};
        float y = 0.65f;
        foreach (var sn in scenes)
        {
            var btnGo = new GameObject("Btn_" + sn);
            btnGo.transform.SetParent(canvasGo.transform, false);
            btnGo.AddComponent<Image>().color = new Color(0.15f, 0.6f, 1f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene(sn));
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(300, 56);

            var label = CreateText("Label_" + sn, new Vector2(0.5f, y), new Vector2(300, 56), 22, sn);
            label.transform.SetParent(btnGo.transform, false);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            y -= 0.1f;
        }
    }
}

// --- RUNTIME LOGIC FOR 65C_AnimationPreview ---
public class AnimationPreviewRuntime : MonoBehaviour
{
    void Start()
    {
        // Ensure camera
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
        }
        cam.orthographicSize = 5f;

        // Two simple rotating quads to ensure something visible without asset dependencies
        GameObject MakeQuad(string name, Vector3 pos, Color color)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.position = pos;
            var mr = q.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            mr.sharedMaterial = mat;
            return q;
        }
        var a = MakeQuad("PacStudentPreview", new Vector3(-2, 0, 0), Color.yellow);
        var b = MakeQuad("GhostPreview", new Vector3(2, 0, 0), Color.cyan);
        a.AddComponent<SimpleRotator>();
        b.AddComponent<SimpleRotator>();
    }
}

public class SimpleRotator : MonoBehaviour
{

    void Update() { transform.Rotate(0, 0, 90f * Time.deltaTime); }
}

// --- RUNTIME LOGIC FOR 75D_ManualLevel & 100HD_ProceduralGameplay ---
public class LevelGenerationRuntime : MonoBehaviour
{
    // Legend-based sprites
    public Sprite s1_OutsideCorner, s2_OutsideWall, s3_InsideCorner, s4_InsideWall, s5_PelletSpot, s6_PowerSpot, s7_TJunction, s8_GhostExit;
    public Vector2 topLeftWorld = new Vector2(-13.5f, 15f);
    public float tileWorldSize = 1f;
    private Transform root;

    // Top-left quadrant from specs
    private int[,] levelMapTopLeft = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,5,4,0,0,4,6,4,0,0,0,4,5,4}, // Swapped power pellet location to make map unique
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

    private int[,] fullMap;

    void Start()
    {
        BuildFullMap();
        Generate();
    }

    void BuildFullMap()
    {
        int h = levelMapTopLeft.GetLength(0);
        int w = levelMapTopLeft.GetLength(1);
        int omitBottom = 1;
        int fullW = w * 2;
        int fullH = (h * 2) - omitBottom;
        fullMap = new int[fullH, fullW];
        // top-left
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) fullMap[y, x] = levelMapTopLeft[y, x];
        // top-right mirror
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) fullMap[y, (fullW - 1) - x] = levelMapTopLeft[y, x];
        // bottom mirrors
        for (int y = 0; y < h - omitBottom; y++) for (int x = 0; x < fullW; x++) fullMap[(fullH - 1) - y, x] = fullMap[y, x];
    }

    void Generate()
    {
        var existing = GameObject.Find("ManualLevel"); if (existing) Destroy(existing);
        var go = new GameObject("ProceduralLevel"); root = go.transform;

        int H = fullMap.GetLength(0), W = fullMap.GetLength(1);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int v = fullMap[y, x]; if (v == 0) continue;
                var cell = new GameObject($"T_{y}_{x}_{v}");
                cell.transform.SetParent(root, false);
                var sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFor(v);
                cell.transform.position = new Vector3(topLeftWorld.x + x * tileWorldSize, topLeftWorld.y - y * tileWorldSize, 0f);
                cell.transform.rotation = Quaternion.Euler(0, 0, ComputeRotationFor(x, y, v));
            }
        }
        FrameCameraToLevel();
    }

    float ComputeRotationFor(int x, int y, int v)
    {
        bool IsWall(int t) => (t >= 1 && t <= 4) || t == 7 || t == 8;
        int up = Get(y - 1, x), down = Get(y + 1, x), left = Get(y, x - 1), right = Get(y, x + 1);
        switch (v)
        {
            case 2: case 4: return (IsWall(left) && IsWall(right)) ? 0f : 90f;
            case 1: case 3:
                bool u = IsWall(up), d = IsWall(down), l = IsWall(left), r = IsWall(right);
                if (r && d) return 0f; if (l && d) return 90f; if (l && u) return 180f; if (r && u) return 270f; return 0f;
            case 7:
                if (!IsWall(up)) return 180f; if (!IsWall(down)) return 0f; if (!IsWall(left)) return 90f; if (!IsWall(right)) return 270f; return 0f;
            default: return 0f;
        }
    }

    int Get(int y, int x)
    {
        if (y < 0 || y >= fullMap.GetLength(0) || x < 0 || x >= fullMap.GetLength(1)) return 0; return fullMap[y, x];
    }

    Sprite SpriteFor(int v) => v switch {
        1 => s1_OutsideCorner,
        2 => s2_OutsideWall,
        3 => s3_InsideCorner,
        4 => s4_InsideWall,
        5 => s5_PelletSpot,
        6 => s6_PowerSpot,
        7 => s7_TJunction,
        8 => s8_GhostExit,
        _ => null
    };

    void FrameCameraToLevel()
    {
        var cam = Camera.main;
        if (cam == null || root == null || root.childCount == 0) return;

        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float worldWidth = b.size.x + 2f; // Add padding
        float worldHeight = b.size.y + 2f; // Add padding

        cam.transform.position = new Vector3(b.center.x, b.center.y, cam.transform.position.z);
        cam.orthographicSize = Mathf.Max(worldHeight * 0.5f, (worldWidth / cam.aspect) * 0.5f);
    }

#if UNITY_EDITOR
    // This version is for the editor script, which can use AssetDatabase
    public void AutoAssignSpritesFromEditor()
    {
        if (s1_OutsideCorner == null) s1_OutsideCorner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/1_outside_corner.png");
        if (s2_OutsideWall == null) s2_OutsideWall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/2_outside_wall.png");
        if (s3_InsideCorner == null) s3_InsideCorner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/3_inside_corner.png");
        if (s4_InsideWall == null) s4_InsideWall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/4_inside_wall.png");
        if (s5_PelletSpot == null) s5_PelletSpot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/pellet.png");
        if (s6_PowerSpot == null) s6_PowerSpot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/power_pellet.png");
        if (s7_TJunction == null) s7_TJunction = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/7_t_junction.png");
        if (s8_GhostExit == null) s8_GhostExit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/8_ghost_exit.png");
    }
#endif
}




// --- EDITOR SCRIPT: THE "ALL DONE!" BUTTON ---
#if UNITY_EDITOR
public static class AllDoneConfigurator
{
    [MenuItem("ALL DONE!/1. Configure All Scenes")]
    public static void ConfigureAllScenes()
    {
        string originalScene = EditorSceneManager.GetActiveScene().path;
        string[] scenePaths = System.IO.Directory.GetFiles("Assets/Scenes", "*.unity", System.IO.SearchOption.AllDirectories);

        foreach (string scenePath in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            Debug.Log($"--- Configuring Scene: {scene.name} ---");
            ClearScene(scene);
            EnsureCamera(scene);
            EnsureEventSystem();
            EnsureHUD();

            switch (scene.name)
            {
                case "Scene_StartMenu":
                    AddComponentByName(new GameObject("StartMenuManager"), "StartMenuRuntime");
                    AddBackgroundMusic("Assets/Audio Clips/menu.mp3", true, 0.6f);
                    break;
                case "65C_AnimationPreview":
                    SetupAnimationPreviewScene();
                    AddBackgroundMusic("Assets/Audio Clips/menu.mp3", true, 0.6f);
                    break;
                case "85HD_AutoMovement":
                    BuildManualLevelStatic(); // Add the level for visual context
                    PlaceStaticGhosts();
                    AddComponentByName(new GameObject("AutoMovementManager"), "AutoMovementRuntime");
                    EnsureMovementSfx();
                    SetupGameplayMusic();
                    break;
                case "75D_ManualLevel":
                    BuildManualLevelStatic();
                    SetupGameplayMusic();
                    break;
                case "100HD_ProceduralGameplay":
                    // Per user request, this scene will start empty.
                    // We still need to create the manager and manually assign its sprite references so it can generate the level at runtime.
                    var lgrGo = new GameObject("LevelGenerationManager");
                    var lgr = AddComponentByName(lgrGo, "LevelGenerationRuntime") as LevelGenerationRuntime;
                    if (lgr != null)
                    {
                        lgr.s1_OutsideCorner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/1_outside_corner.png");
                        lgr.s2_OutsideWall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/2_outside_wall.png");
                        lgr.s3_InsideCorner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/3_inside_corner.png");
                        lgr.s4_InsideWall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/4_inside_wall.png");
                        lgr.s5_PelletSpot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/pellet.png");
                        lgr.s6_PowerSpot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/power_pellet.png");
                        lgr.s7_TJunction = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/7_t_junction.png");
                        lgr.s8_GhostExit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Walls/8_ghost_exit.png");
                    }
                    SetupGameplayMusic();
                    break;
                default:


                    // Add a simple label so the scene is not empty
                    var canvasGo = new GameObject("Canvas");
                    var canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var t = new GameObject("SceneLabel").AddComponent<Text>();
                    t.transform.SetParent(canvas.transform, false);
                    t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    t.alignment = TextAnchor.MiddleCenter;
                    t.fontSize = 30;
                    t.text = scene.name;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(500, 100);
                    break;
            }

            FitCameraToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.OpenScene(originalScene);
        Debug.Log("ALL SCENES CONFIGURED!");
    }



	    // Helper: add component by class name at edit time to avoid hard assembly references
	    private static Component AddComponentByName(GameObject go, string typeName)
	    {
	        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
	        System.Type found = null;
	        foreach (var asm in assemblies)
	        {
	            System.Type[] types;
	            try { types = asm.GetTypes(); }
	            catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }
	            if (types == null) continue;
	            foreach (var t in types)
	            {
	                if (t == null) continue;
	                if (t.Name == typeName && typeof(Component).IsAssignableFrom(t)) { found = t; break; }
	            }
	            if (found != null) break;
	        }
	        if (found == null) { Debug.LogError($"ALL DONE!: Could not find component type '{typeName}'."); return null; }
	        return go.AddComponent(found);
	    }


        // Create Pac/Ghost animated preview using existing Animator Controllers
        private static void SetupAnimationPreviewScene()
        {
#if UNITY_EDITOR
            // PacStudent
            var pac = new GameObject("PacStudent_Preview");
            pac.AddComponent<SpriteRenderer>();
            var pacAnim = pac.AddComponent<Animator>();
            var pacCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PacStudent/PacStudent.controller");
            pacAnim.runtimeAnimatorController = pacCtrl;
            pac.transform.position = new Vector3(-3, 0, 0);

            // Ghost
            var ghost = new GameObject("Ghost_Preview");
            ghost.AddComponent<SpriteRenderer>();
            var ghostAnim = ghost.AddComponent<Animator>();
            var ghostCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Ghosts/GhostAnimator_Preview.controller");
            ghostAnim.runtimeAnimatorController = ghostCtrl;
            ghost.transform.position = new Vector3(3, 0, 0);
#endif
        }

        // Build Manual Level tiles at edit time (present before pressing Play)
        private static void BuildManualLevelStatic()
        {
#if UNITY_EDITOR
            var temp = new GameObject("Temp_LevelGen").AddComponent<LevelGenerationRuntime>();
            // Ensure sprites are assigned in Editor so generated tiles are visible
            temp.AutoAssignSpritesFromEditor();
            var miBuild = typeof(LevelGenerationRuntime).GetMethod("BuildFullMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var miGen = typeof(LevelGenerationRuntime).GetMethod("Generate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (miBuild != null && miGen != null)
            {
                miBuild.Invoke(temp, null);
                miGen.Invoke(temp, null);
            }
            var generatedLevel = GameObject.Find("ProceduralLevel");
            if (generatedLevel != null) generatedLevel.name = "ManualLevel";
            Object.DestroyImmediate(temp.gameObject);
#endif
        }

        // Add looped movement SFX to the scene
        private static void EnsureMovementSfx()
        {
#if UNITY_EDITOR
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/sfx_move.wav");
            if (clip == null) return;
            var go = new GameObject("MovementSfx");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.volume = 0.25f; src.playOnAwake = true;
#endif
        }


        // Build a simple custom manual map using the legend sprites (present before Play)
        private static void BuildManualLevelSimple()
        {
#if UNITY_EDITOR
            // Root
            var existing = GameObject.Find("ManualLevel"); if (existing) Object.DestroyImmediate(existing);
            var root = new GameObject("ManualLevel").transform;

            // Load sprites
            Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
            var s1 = S("Assets/Sprites/Walls/1_outside_corner.png");
            var s2 = S("Assets/Sprites/Walls/2_outside_wall.png");
            var s3 = S("Assets/Sprites/Walls/3_inside_corner.png");
            var s4 = S("Assets/Sprites/Walls/4_inside_wall.png");
            var s5 = S("Assets/Sprites/Items/pellet.png");
            var s6 = S("Assets/Sprites/Items/power_pellet.png");
            var s7 = S("Assets/Sprites/Walls/7_t_junction.png");
            var s8 = S("Assets/Sprites/Walls/8_ghost_exit.png");
            var s9_Cherry = S("Assets/Sprites/Items/cherry.png");

            // A simple, unique map for 75D_ManualLevel
            int[,] map = new int[,]
            {
                {1,2,2,2,2,2,2,2,2,2,2,1},
                {2,6,5,5,5,5,5,5,5,5,5,2},
                {2,5,1,2,2,1,1,2,2,1,5,2},
                {2,5,2,0,0,9,9,0,0,2,5,2},
                {2,5,1,2,2,1,1,2,2,1,5,2},
                {2,5,5,5,5,5,5,5,5,5,6,2},
                {1,2,2,2,2,2,2,2,2,2,2,1},
            };

            int H = map.GetLength(0), W = map.GetLength(1);
            Vector2 topLeft = new Vector2(-W/2f, H/2f);
            float tile = 1f;

            // Helpers
            bool IsWall(int t) => (t >= 1 && t <= 4) || t == 7 || t == 8;
            int Get(int y, int x) { if (y < 0 || y >= H || x < 0 || x >= W) return 0; return map[y,x]; }
            float Rot(int x, int y, int v)
            {
                int up = Get(y-1,x), down = Get(y+1,x), left = Get(y,x-1), right = Get(y,x+1);
                switch(v)
                {
                    case 2: case 4: return (IsWall(left) && IsWall(right)) ? 0f : 90f;
                    case 1: case 3:
                        bool u = IsWall(up), d = IsWall(down), l = IsWall(left), r = IsWall(right);
                        if (r && d) return 0f; if (l && d) return 90f; if (l && u) return 180f; if (r && u) return 270f; return 0f;
                    case 7:
                        if (!IsWall(up)) return 180f; if (!IsWall(down)) return 0f; if (!IsWall(left)) return 90f; if (!IsWall(right)) return 270f; return 0f;
                    default: return 0f;
                }
            }
            Sprite Spr(int v) => v switch {1=>s1,2=>s2,3=>s3,4=>s4,5=>s5,6=>s6,7=>s7,8=>s8,9=>s9_Cherry,_=>null};

            for (int y=0;y<H;y++)
            for (int x=0;x<W;x++)
            {
                int v = map[y,x]; if (v==0) continue;
                var go = new GameObject($"T_{y}_{x}_{v}");
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Spr(v);
                go.transform.position = new Vector3(topLeft.x + x*tile, topLeft.y - y*tile, 0);
                go.transform.rotation = Quaternion.Euler(0,0, Rot(x,y,v));
            }
#endif
        }






    private static void ClearScene(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects()) Object.DestroyImmediate(go);
    }

    private static void EnsureCamera(UnityEngine.SceneManagement.Scene scene)
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.AddComponent<AudioListener>();


        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0, 0, -10);
        cam.orthographic = true;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

        // Add background music to the scene (Editor-time only)
        private static void AddBackgroundMusic(string clipPath, bool loop = true, float volume = 0.7f)
        {
    #if UNITY_EDITOR
            if (string.IsNullOrEmpty(clipPath)) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"ALL DONE!: Music clip not found at {clipPath}"); return; }
            var go = new GameObject("Music");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.loop = loop; src.volume = volume; src.playOnAwake = true;
    #endif
        }

        private static void PlaceStaticGhosts()
        {
#if UNITY_EDITOR
            var ghostSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ghosts/ghost_walk_down_0.png");
            if (ghostSprite == null) return;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(-1.5f, 1f, 0),
                new Vector3(-0.5f, 1f, 0),
                new Vector3(0.5f, 1f, 0),
                new Vector3(1.5f, 1f, 0),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject("StaticGhost_" + i);
                go.AddComponent<SpriteRenderer>().sprite = ghostSprite;
                go.transform.position = positions[i];
            }
#endif
        }


    private static void EnsureHUD()
    {
        var canvasGo = new GameObject("Canvas_HUD");


        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        Text CreateText(string name, Vector2 pos) {
            var textGo = new GameObject(name);
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 40);
            return text;
        }
        CreateText("ScoreText", new Vector2(10, -10)).text = "Score: 0";
        CreateText("LivesText", new Vector2(10, -50)).text = "Lives: ";

#if UNITY_EDITOR
        // Add life icons
        var lifeIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/life_icon.png");
        if (lifeIconSprite != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var lifeIconGo = new GameObject("LifeIcon_" + i);
                lifeIconGo.transform.SetParent(canvasGo.transform, false);
                var img = lifeIconGo.AddComponent<Image>();
                img.sprite = lifeIconSprite;
                var rt = lifeIconGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(30, 30);
                rt.anchoredPosition = new Vector2(80 + (i * 35), -50);
            }
        }
#endif
    }

    private static void FitCameraToScene(UnityEngine.SceneManagement.Scene scene)
    {
        var cam = Camera.main;
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        if (renderers.Length == 0) return;

        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float worldWidth = b.size.x + 2f, worldHeight = b.size.y + 2f;
        cam.transform.position = new Vector3(b.center.x, b.center.y, cam.transform.position.z);
        cam.orthographicSize = Mathf.Max(worldHeight * 0.5f, (worldWidth / cam.aspect) * 0.5f);
    }
    private static void SetupGameplayMusic()
    {
#if UNITY_EDITOR
        var musicGo = new GameObject("BackgroundMusicManager");
        var musicManager = musicGo.AddComponent<BackgroundMusicManager>();
        musicManager.introClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/intro.wav");
        musicManager.normalClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/normal.wav");
#endif
    }
}

#endif
