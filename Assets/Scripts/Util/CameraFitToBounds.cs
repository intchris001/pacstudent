using System.Linq;
using UnityEngine;

namespace PacmanGame.Util
{
    [RequireComponent(typeof(Camera))]
    public class CameraFitToBounds : MonoBehaviour
    {
        public string rootName = "ManualLevel"; // if empty, use all SpriteRenderers in scene
        public float paddingWorld = 0.6f;
        public bool fitOnStart = true;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null) cam.orthographic = true;
        }

        private void Start()
        {
            if (fitOnStart) FitNow();
        }

        public void FitNow()
        {
            if (cam == null) return;

            var root = !string.IsNullOrEmpty(rootName) ? GameObject.Find(rootName) : null;
            var renderers = (root != null)
                ? root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                : FindObjectsOfType<SpriteRenderer>();
            if (renderers == null || renderers.Length == 0) return;

            Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                b.Encapsulate(r.bounds);
            }

            float worldWidth = b.size.x + paddingWorld * 2f;
            float worldHeight = b.size.y + paddingWorld * 2f;
            Vector3 center = new Vector3(b.center.x, b.center.y, transform.position.z);
            transform.position = center;

            float sizeByHeight = worldHeight * 0.5f;
            float sizeByWidth = worldWidth / (2f * Mathf.Max(0.0001f, cam.aspect));
            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
        }
    }
}

