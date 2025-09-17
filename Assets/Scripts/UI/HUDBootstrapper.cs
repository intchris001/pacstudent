using UnityEngine;
using UnityEngine.UI;

namespace PacmanGame.UI
{
    // Creates a minimal Canvas + Text HUD at runtime if none exists and wires it to HUDController.
    public class HUDBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (FindObjectOfType<HUDController>() != null) { Destroy(this); return; }

            // Create Canvas
            var canvasGo = new GameObject("Canvas_HUD");
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Create score Text
            var scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(canvasGo.transform, false);
            var scoreText = scoreGo.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            scoreText.alignment = TextAnchor.UpperLeft;
            scoreText.fontSize = 24;
            var scoreRt = scoreGo.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0, 1);
            scoreRt.anchorMax = new Vector2(0, 1);
            scoreRt.pivot = new Vector2(0, 1);
            scoreRt.anchoredPosition = new Vector2(10, -10);
            scoreRt.sizeDelta = new Vector2(300, 40);

            // Create lives Text
            var livesGo = new GameObject("LivesText");
            livesGo.transform.SetParent(canvasGo.transform, false);
            var livesText = livesGo.AddComponent<Text>();
            livesText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            livesText.alignment = TextAnchor.UpperLeft;
            livesText.fontSize = 24;
            var livesRt = livesGo.GetComponent<RectTransform>();
            livesRt.anchorMin = new Vector2(0, 1);
            livesRt.anchorMax = new Vector2(0, 1);
            livesRt.pivot = new Vector2(0, 1);
            livesRt.anchoredPosition = new Vector2(10, -50);
            livesRt.sizeDelta = new Vector2(300, 40);

            // Attach HUDController
            var hud = canvasGo.AddComponent<HUDController>();
            hud.scoreText = scoreText;
            hud.livesText = livesText;

            Destroy(this);
        }
    }
}

