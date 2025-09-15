#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class A3SpriteGenerator
{
    private const int Size = 32; // 32x32 px
    private const float Line = 3f;

    [MenuItem("PacStudent/Generate Sprites (Walls/Items/Characters)")]
    public static void GenerateAll()
    {
        string wallsDir = "Assets/Art/Sprites/Walls";
        string itemsDir = "Assets/Art/Sprites/Items";
        string charsDir = "Assets/Art/Sprites/Characters";
        Directory.CreateDirectory(wallsDir);
        Directory.CreateDirectory(itemsDir);
        Directory.CreateDirectory(charsDir);

        // Wall sprites 1..8 (EXACTLY 8 for manual layout)
        SaveTexture(wallsDir+"/1_outside_corner.png", TexCorner(new Color(0.1f,0.4f,1f), true));
        SaveTexture(wallsDir+"/2_outside_wall.png", TexWall(new Color(0.1f,0.4f,1f), true));
        SaveTexture(wallsDir+"/3_inside_corner.png", TexCorner(new Color(0.3f,1f,0.8f), false));
        SaveTexture(wallsDir+"/4_inside_wall.png", TexWall(new Color(0.3f,1f,0.8f), false));
        SaveTexture(wallsDir+"/5_pellet_spot.png", TexSpot(new Color(0.05f,0.05f,0.05f), new Color(1f,0.95f,0.6f), 3));
        SaveTexture(wallsDir+"/6_power_spot.png", TexSpot(new Color(0.05f,0.05f,0.05f), new Color(1f,0.6f,0.3f), 6));
        SaveTexture(wallsDir+"/7_t_junction.png", TexTJunction(new Color(0.1f,0.4f,1f)));
        SaveTexture(wallsDir+"/8_ghost_exit.png", TexGhostExit(new Color(1f,0.4f,0.7f)));

        // Items (pellets etc., used for animations and HUD)
        SaveTexture(itemsDir+"/pellet.png", Dot(new Color(1f,0.95f,0.6f), 3));
        SaveTexture(itemsDir+"/power_pellet_0.png", Dot(new Color(1f,0.6f,0.3f), 6));
        SaveTexture(itemsDir+"/power_pellet_1.png", Dot(new Color(1f,0.9f,0.5f), 6));
        SaveTexture(itemsDir+"/cherry.png", Cherry());
        SaveTexture(itemsDir+"/life_icon.png", Dot(new Color(1f,0.85f,0.2f), 6));

        // Characters - PacStudent
        SaveTexture(charsDir+"/pac_left_0.png", Pac(Color.yellow, 270));
        SaveTexture(charsDir+"/pac_left_1.png", PacMouthClosed(Color.yellow));
        SaveTexture(charsDir+"/pac_right_0.png", Pac(Color.yellow, 90));
        SaveTexture(charsDir+"/pac_right_1.png", PacMouthClosed(Color.yellow));
        SaveTexture(charsDir+"/pac_up_0.png", Pac(Color.yellow, 0));
        SaveTexture(charsDir+"/pac_up_1.png", PacMouthClosed(Color.yellow));
        SaveTexture(charsDir+"/pac_down_0.png", Pac(Color.yellow, 180));
        SaveTexture(charsDir+"/pac_down_1.png", PacMouthClosed(Color.yellow));
        SaveTexture(charsDir+"/pac_dead_0.png", Cross(Color.red));
        SaveTexture(charsDir+"/pac_dead_1.png", Cross(new Color(1f,0.3f,0.3f)));

        // Characters - Ghosts (normal blue)
        SaveTexture(charsDir+"/ghost_body.png", GhostBody(new Color(0.2f,0.5f,1f)));
        SaveTexture(charsDir+"/ghost_scared.png", GhostBody(new Color(0.1f,0.2f,0.7f)));
        SaveTexture(charsDir+"/ghost_recover_0.png", GhostBody(Color.white));
        SaveTexture(charsDir+"/ghost_recover_1.png", GhostBody(new Color(0.1f,0.2f,0.7f)));
        SaveTexture(charsDir+"/ghost_eyes.png", GhostEyes());

        AssetDatabase.Refresh();
        Debug.Log("Generated sprites in Assets/Art/Sprites. Now create animators and build manual level.");
    }

    private static Texture2D MakeTex()
    {
        var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var px = new Color[Size*Size];
        for (int i=0;i<px.Length;i++) px[i]=new Color(0,0,0,0);
        t.SetPixels(px);
        return t;
    }
    private static void SaveTexture(string path, Texture2D tex)
    {
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
    }
    private static void LineH(Texture2D t, int y, int x0, int x1, Color c){for(int x=x0;x<=x1;x++) t.SetPixel(x,y,c);}
    private static void LineV(Texture2D t, int x, int y0, int y1, Color c){for(int y=y0;y<=y1;y++) t.SetPixel(x,y,c);}

    private static Texture2D TexWall(Color col, bool outside)
    {
        var t = MakeTex();
        int m = outside?2:4;
        for (int i=0;i< (outside?2:1); i++)
        {
            int y = Size/2 + i*2;
            LineH(t, y, 4, Size-5, col);
        }
        t.Apply(); return t;
    }
    private static Texture2D TexCorner(Color col, bool outside)
    {
        var t = MakeTex();
        int thick = outside?2:1;
        for (int i=0;i<thick;i++)
        {
            LineH(t, Size/2+i, Size/2, Size-4, col);
            LineV(t, Size/2+i, Size/2, 4, col);
        }
        t.Apply(); return t;
    }
    private static Texture2D TexTJunction(Color col)
    {
        var t = MakeTex();
        for (int i=0;i<2;i++) LineH(t, Size/2+i, 6, Size-6, col);
        for (int i=0;i<2;i++) LineV(t, Size/2+i, 6, Size-12, col);
        t.Apply(); return t;
    }
    private static Texture2D TexGhostExit(Color col)
    {
        var t = MakeTex();
        for (int i=0;i<2;i++) LineH(t, Size/2+i, 6, Size-6, col);
        t.Apply(); return t;
    }
    private static Texture2D TexSpot(Color bg, Color dotCol, int r)
    {
        var t = MakeTex();
        // dark ground
        for (int y=0;y<Size;y++) for (int x=0;x<Size;x++) t.SetPixel(x,y,new Color(bg.r,bg.g,bg.b,1));
        // small dot center
        FillCircle(t, Size/2, Size/2, r, dotCol);
        t.Apply(); return t;
    }
    private static Texture2D Dot(Color c, int r)
    {
        var t = MakeTex();
        FillCircle(t, Size/2, Size/2, r, c);
        t.Apply(); return t;
    }
    private static void FillCircle(Texture2D t, int cx, int cy, int r, Color c)
    {
        int r2 = r*r;
        for (int y=cy-r; y<=cy+r; y++)
            for (int x=cx-r; x<=cx+r; x++)
            {
                int dx=x-cx, dy=y-cy; if (dx*dx+dy*dy<=r2) t.SetPixel(x,y,c);
            }
    }
    private static Texture2D Cherry()
    {
        var t = MakeTex();
        FillCircle(t, 14, 18, 5, new Color(0.9f,0.1f,0.2f));
        FillCircle(t, 19, 15, 5, new Color(0.9f,0.1f,0.2f));
        // stem
        for (int i=0;i<6;i++) t.SetPixel(19+i, 22+i/2, new Color(0.1f,0.8f,0.2f));
        t.Apply(); return t;
    }
    private static Texture2D Pac(Color body, float mouthDirDeg)
    {
        var t = MakeTex();
        FillCircle(t, Size/2, Size/2, 12, body);
        // mouth: erase wedge
        float rad = mouthDirDeg*Mathf.Deg2Rad;
        float a0 = rad - 0.4f, a1 = rad + 0.4f;
        for (int y=0;y<Size;y++) for (int x=0;x<Size;x++)
        {
            float dx=x-Size/2f, dy=y-Size/2f; float a=Mathf.Atan2(dy,dx); float d=Mathf.Sqrt(dx*dx+dy*dy);
            if (d<13 && InWedge(a,a0,a1)) t.SetPixel(x,y,new Color(0,0,0,0));
        }
        t.Apply(); return t;
    }
    private static bool InWedge(float a, float a0, float a1)
    {
        float da = Mathf.DeltaAngle(a*Mathf.Rad2Deg, a0*Mathf.Rad2Deg);
        float db = Mathf.DeltaAngle(a*Mathf.Rad2Deg, a1*Mathf.Rad2Deg);
        return da<=0 && db>=0;
    }
    private static Texture2D PacMouthClosed(Color body)
    {
        var t = MakeTex();
        FillCircle(t, Size/2, Size/2, 12, body); t.Apply(); return t;
    }
    private static Texture2D Cross(Color c)
    {
        var t = MakeTex();
        for (int i=8;i<24;i++) { t.SetPixel(i,i,c); t.SetPixel(31-i,i,c);} t.Apply(); return t;
    }
    private static Texture2D GhostBody(Color c)
    {
        var t = MakeTex();
        // body
        for (int y=8;y<24;y++) for (int x=8;x<24;x++) t.SetPixel(x,y,c);
        // head curve
        FillCircle(t, 16, 12, 8, c);
        // eyes
        FillCircle(t, 13, 14, 2, Color.white);
        FillCircle(t, 19, 14, 2, Color.white);
        t.Apply(); return t;
    }
    private static Texture2D GhostEyes()
    {
        var t = MakeTex();
        FillCircle(t, 13, 14, 2, Color.white);
        FillCircle(t, 19, 14, 2, Color.white);
        t.Apply(); return t;
    }
}
#endif

