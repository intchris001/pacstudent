using UnityEngine;
using PacmanGame.Level;
using PacmanGame.Core;

namespace PacmanGame.Core
{
    // Simple cherry spawner for full-game experience.
    // Spawns a cherry once when remaining collectibles fall under a threshold, then despawns after a timeout or when eaten.
    public class FruitSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public int spawnWhenRemainingAtOrBelow = 120; // spawn when pellets <= this
        public float lifetimeSeconds = 10f;
        public int fruitPoints = 100;

        [Header("Visuals")]
        public Sprite fruitSprite; // cherry

        private GameObject currentFruit;
        private float lifeTimer;
        private Transform pacman;
        private LevelGrid grid;

        private void Start()
        {
            grid = FindObjectOfType<LevelGrid>();
            var pc = FindObjectOfType<PacmanGame.Player.PacmanController>();
            if (pc != null) pacman = pc.transform;
#if UNITY_EDITOR
            if (fruitSprite == null)
            {
                fruitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Items/cherry.png");
            }
#endif
        }

        private void Update()
        {
            var loader = LevelLoader.Instance;
            if (loader == null || grid == null) return;

            if (currentFruit == null)
            {
                if (loader.RemainingCollectibles > 0 && loader.RemainingCollectibles <= spawnWhenRemainingAtOrBelow)
                {
                    SpawnFruitAtCenter();
                }
            }
            else
            {
                lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0f)
                {
                    Destroy(currentFruit);
                    currentFruit = null;
                }
                else if (pacman != null)
                {
                    float dist = Vector2.Distance(pacman.position, currentFruit.transform.position);
                    if (dist < 0.5f)
                    {
                        GameManager.Instance?.OnFruitConsumed(fruitPoints);
                        Destroy(currentFruit);
                        currentFruit = null;
                    }
                }
            }
        }

        private void SpawnFruitAtCenter()
        {
            if (fruitSprite == null) return;
            int cx = Mathf.Clamp(grid.Width / 2, 0, Mathf.Max(0, grid.Width - 1));
            int cy = Mathf.Clamp(grid.Height / 2, 0, Mathf.Max(0, grid.Height - 1));
            Vector3 wp = grid.GridToWorld(cx, cy);

            currentFruit = new GameObject("Fruit_Cherry");
            var sr = currentFruit.AddComponent<SpriteRenderer>();
            sr.sprite = fruitSprite;
            currentFruit.transform.position = wp;
            lifeTimer = lifetimeSeconds;
        }
    }
}

