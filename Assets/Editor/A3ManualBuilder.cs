#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class A3ManualBuilder
{
    // Numeric top-left quadrant from PDF
    private static int[,] TL = new int[,]
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

    [MenuItem("PacStudent/Build Manual Level (from 8 sprites)")]
    public static void BuildManual()
    {
        float tile = 1f; // world units per cell
        Vector2 topLeft = new Vector2(-7.5f, 8.5f);

        // Load sprites by known generated names
        string wallsDir = "Assets/Art/Sprites/Walls";
        Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/1_outside_corner.png");
        Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/2_outside_wall.png");
        Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/3_inside_corner.png");
        Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/4_inside_wall.png");
        Sprite s5 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/5_pellet_spot.png");
        Sprite s6_0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/6_power_spot_0.png");
        Sprite s7 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/7_t_junction.png");
        Sprite s8 = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/8_ghost_exit.png");

        if (s1==null||s2==null||s3==null||s4==null||s5==null||s6_0==null||s7==null||s8==null)
        {
            Debug.LogError("Missing wall sprites. Run PacStudent/Generate Sprites first or assign your own at Assets/Art/Sprites/Walls.");
            return;
        }

        var manualRoot = GameObject.Find("ManualLevel");
        if (manualRoot == null) manualRoot = new GameObject("ManualLevel");

        // Clear children
        for (int i = manualRoot.transform.childCount-1; i>=0; i--) Object.DestroyImmediate(manualRoot.transform.GetChild(i).gameObject);

        // Build full map via mirror
        int h = TL.GetLength(0);
        int w = TL.GetLength(1);
        int omitBottom = 1;
        int fullW = w*2;
        int fullH = (h*2)-omitBottom;
        int[,] map = new int[fullH, fullW];
        // place TL
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) map[y,x]=TL[y,x];
        // mirror HR
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) map[y, fullW-1-x] = TL[y,x];
        // mirror vertical
        for(int y=0;y<h-omitBottom;y++) for(int x=0;x<fullW;x++) map[fullH-1-y, x] = map[y,x];

        // instantiate
        for (int y=0;y<fullH;y++)
        {
            for (int x=0;x<fullW;x++)
            {
                int v = map[y,x];
                if (v==0) continue;
                var go = new GameObject($"M_{y}_{x}_{v}");
                go.transform.SetParent(manualRoot.transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = v switch {1=>s1,2=>s2,3=>s3,4=>s4,5=>s5,6=>s6_0,7=>s7,8=>s8,_=>null};
                go.transform.position = new Vector3(topLeft.x + (x+0.5f)*tile, topLeft.y - (y+0.5f)*tile, 0);
                go.transform.rotation = Quaternion.Euler(0,0, ComputeRotation(map,x,y,v));
            }
        }

        Selection.activeGameObject = manualRoot;
        Debug.Log("Manual level built in scene under 'ManualLevel'.");
    }

    private static float ComputeRotation(int[,] map, int x, int y, int v)
    {
        int H = map.GetLength(0), W=map.GetLength(1);
        int Get(int yy,int xx){ if (yy<0||yy>=H||xx<0||xx>=W) return 0; return map[yy,xx]; }
        bool IsOutside(int t)=>(t==1||t==2||t==7);
        bool IsInside(int t)=>(t==3||t==4||t==7||t==8);
        int up=Get(y-1,x),down=Get(y+1,x),left=Get(y,x-1),right=Get(y,x+1);
        switch(v)
        {
            case 2: return (IsOutside(left)&&IsOutside(right))?0f:90f;
            case 1:
            {
                bool u=IsOutside(up), d=IsOutside(down), l=IsOutside(left), r=IsOutside(right);
                if (r&&d) return 0f; if (r&&u) return 270f; if (l&&d) return 90f; if (l&&u) return 180f; return 0f;
            }
            case 7:
            {
                bool u=IsOutside(up), d=IsOutside(down), l=IsOutside(left), r=IsOutside(right);
                if (!u && d && l && r) return 0f;
                if (!r && d && l && u) return 90f;
                if (!d && u && l && r) return 180f;
                if (!l && d && u && r) return 270f; return 0f;
            }
            case 4: return (IsInside(left)&&IsInside(right))?0f:90f;
            case 3:
            {
                bool u=IsInside(up), d=IsInside(down), l=IsInside(left), r=IsInside(right);
                if (r&&d) return 0f; if (r&&u) return 270f; if (l&&d) return 90f; if (l&&u) return 180f; return 0f;
            }
            case 8: return (IsInside(left)&&IsInside(right))?0f:90f;
            default: return 0f;
        }
    }
}
#endif

