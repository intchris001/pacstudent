using UnityEngine;
using UnityEngine.UI;
using PacmanGame.Core;
using PacmanGame.Level;

namespace PacmanGame.UI
{
    public class HUDController : MonoBehaviour
    {
        public Text scoreText;
        public Text livesText;

        private GameManager gm;

        private void Start()
        {
            gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
            UpdateTexts();
        }

        private void Update()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            if (gm == null) return;
            if (scoreText != null) scoreText.text = $"Score: {gm.score}";
            if (livesText != null) livesText.text = $"Lives: {gm.lives}";
        }
    }
}

