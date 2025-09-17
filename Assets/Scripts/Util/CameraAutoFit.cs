using UnityEngine;
using PacmanGame.Level;

namespace PacmanGame.Util
{
    [RequireComponent(typeof(Camera))]
    public class CameraAutoFit : MonoBehaviour
    {
        public float extraPaddingWorld = 0.5f; // extra world units around the map
        public bool followCenter = true;       // keep camera centered on the map

        private Camera cam;
        private LevelGrid grid;
        private Vector2 lastScreenSize;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null) cam.orthographic = true;
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private System.Collections.IEnumerator Start()
        {
            // Wait until LevelGrid is available
            while (grid == null)
            {
                grid = FindObjectOfType<LevelGrid>();
                if (grid == null) yield return null; else break;
            }
            FitNow();
        }

        private void Update()
        {
            if (grid == null) return;
            if (Screen.width != (int)lastScreenSize.x || Screen.height != (int)lastScreenSize.y)
            {
                lastScreenSize = new Vector2(Screen.width, Screen.height);
                FitNow();
            }
        }

        public void FitNow()
        {
            if (cam == null || grid == null) return;

            // Compute map bounds using grid's conversions to be robust.
            Vector2 tlCenter = grid.GridToWorld(0, 0);
            Vector2 brCenter = grid.GridToWorld(grid.Width - 1, grid.Height - 1);

            // Estimate cell size from distance between adjacent centers
            float cellSizeX = (grid.Width > 1) ? Mathf.Abs(grid.GridToWorld(1, 0).x - tlCenter.x) : 1f;
            float cellSizeY = (grid.Height > 1) ? Mathf.Abs(grid.GridToWorld(0, 1).y - tlCenter.y) : 1f;
            float cellSize = Mathf.Max(cellSizeX, Mathf.Abs(cellSizeY));

            float minX = Mathf.Min(tlCenter.x, brCenter.x) - cellSize * 0.5f - extraPaddingWorld;
            float maxX = Mathf.Max(tlCenter.x, brCenter.x) + cellSize * 0.5f + extraPaddingWorld;
            float minY = Mathf.Min(tlCenter.y, brCenter.y) - cellSize * 0.5f - extraPaddingWorld;
            float maxY = Mathf.Max(tlCenter.y, brCenter.y) + cellSize * 0.5f + extraPaddingWorld;

            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            if (followCenter)
            {
                transform.position = center;
            }

            float worldWidth = (maxX - minX);
            float worldHeight = (maxY - minY);

            float neededByHeight = worldHeight * 0.5f;
            float neededByWidth = worldWidth / (2f * Mathf.Max(0.0001f, cam.aspect));

            cam.orthographicSize = Mathf.Max(neededByHeight, neededByWidth);
        }
    }
}

