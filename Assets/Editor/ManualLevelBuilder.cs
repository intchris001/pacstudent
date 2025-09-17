#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ManualLevelBuilder
{
    // The map data from the Assessment 3 PDF
    private static int[,] levelMap =
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

    [MenuItem("PacStudent/Build Manual Level")]
    public static void BuildLevel()
    {
        // 0. Clean up previous manual level
        var existingLevel = GameObject.Find("ManualLevel");
        if (existingLevel != null)
        {
            GameObject.DestroyImmediate(existingLevel);
        }

        // 1. Create parent object
        var levelParent = new GameObject("ManualLevel");

        // 2. Load sprites
        string wallsDir = "Assets/Art/Sprites/Walls";
        var sprites = new Sprite[9]; // 1-indexed
        sprites[1] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/1_outside_corner.png");
        sprites[2] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/2_outside_wall.png");
        sprites[3] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/3_inside_corner.png");
        sprites[4] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/4_inside_wall.png");
        sprites[5] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/5_pellet_spot.png");
        sprites[6] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/6_power_spot.png");
        sprites[7] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/7_t_junction.png");
        sprites[8] = AssetDatabase.LoadAssetAtPath<Sprite>($"{wallsDir}/8_ghost_exit.png");

        // 3. Build Top-Left Quadrant
        int height = levelMap.GetLength(0);
        int width = levelMap.GetLength(1);
        var origin = new Vector2(-width + 0.5f, height - 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int tileId = levelMap[y, x];
                if (tileId == 0) continue;

                var sprite = sprites[tileId];
                if (sprite == null) continue;

                var go = new GameObject($"Tile_{x}_{y}");
                go.transform.SetParent(levelParent.transform);
                go.transform.position = origin + new Vector2(x, -y);
                go.AddComponent<SpriteRenderer>().sprite = sprite;

                // Manual rotation based on the specific level map layout
                // This part is hardcoded for the 75% D requirement
                float rotation = GetTileRotation(x, y, tileId);
                go.transform.rotation = Quaternion.Euler(0, 0, rotation);
            }
        }

        // 4. Mirror to create full level
        Mirror(levelParent, origin, width, height);

        Debug.Log("Manual level built successfully!");
    }

    private static float GetTileRotation(int x, int y, int tileId)
    {
        // This is a simplified, hardcoded logic for the specific A3 level map
        // It doesn't use neighbor checking, which is reserved for the 100% HD task.
        switch (tileId)
        {
            case 1: // Outside Corner
                if (y == 0 && x == 0) return 0; // TL
                if (y == 9 && x == 5) return 270; // TR
                if (y == 13 && x == 5) return 270; // TR
                return 0;
            case 2: // Outside Wall
                if (y == 0 || y == 13) return 0; // Horizontal
                if (x == 0 || x == 13) return 90; // Vertical
                return 0;
            case 3: // Inside Corner
                if ((y == 2 && x == 2) || (y == 6 && x == 2) || (y == 7 && x == 2)) return 0; // TL
                if ((y == 2 && x == 5) || (y == 6 && x == 5)) return 270; // TR
                if ((y == 4 && x == 11) || (y == 6 && x == 8) || (y == 7 && x == 11)) return 180; // BR
                if ((y == 4 && x == 2) || (y == 9 && x == 8) || (y == 12 && x == 10)) return 0; // TL
                return 0;
            case 4: // Inside Wall
                if ((y == 2 && (x > 2 && x < 5)) || (y == 4 && (x > 2 && x < 5))) return 0; // Horizontal
                if ((x == 2 && (y == 3 || y==4)) || (x == 5 && (y == 3 || y==4))) return 90; // Vertical
                return (y == 3 || y == 9 || y == 10 || y == 12) ? 90 : 0;
            case 7: // T-Junction
                return 0;
            case 8: // Ghost Exit
                return 0;
            default: // Pellets, etc.
                return 0;
        }
    }

    private static void Mirror(GameObject parent, Vector2 origin, int width, int height)
    {
        // Mirror Horizontally for Top-Right
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var original = parent.transform.GetChild(i);
            // Don't mirror the center column (T-junctions)
            if (Mathf.Approximately(original.transform.position.x, origin.x + width - 1))
                continue;

            var mirrored = GameObject.Instantiate(original, parent.transform);
            mirrored.transform.position = new Vector3(
                -(original.transform.position.x - (origin.x * 2 + width * 2 - 3)),
                original.transform.position.y, 0);
            mirrored.transform.localScale = new Vector3(-1, 1, 1);
        }

        // Mirror Vertically for Bottom half
        int childCount = parent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var original = parent.transform.GetChild(i);
             // Don't mirror the bottom row of the top half
            if (Mathf.Approximately(original.transform.position.y, origin.y - height + 1))
                continue;

            var mirrored = GameObject.Instantiate(original, parent.transform);
            mirrored.transform.position = new Vector3(
                original.transform.position.x,
                -(original.transform.position.y - (origin.y * 2 + height * 2 - 3)), 0);
            mirrored.transform.localScale = new Vector3(mirrored.transform.localScale.x, -1, 1);
        }
    }
}
#endif

