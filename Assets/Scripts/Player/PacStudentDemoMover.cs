using UnityEngine;
using PacmanGame.Audio;

// Assessment 3 - 85% band demo: move PacStudent clockwise around the top-left inner block
// Requirements:
// - Frame-rate independent motion using programmatic tweening (no MoveTowards/rigidbody velocity)
// - Linear speed along each segment
// - No user input, no wall/pellet collisions
// Usage: Place this on your PacStudent in the scene used for A3 marking (disable other movement scripts)
public class PacStudentDemoMover : MonoBehaviour
{
    [Header("Path Settings")]
    public Vector3[] cornerWorldPoints; // 4 or more corners forming a loop (clockwise)
    public float speed = 3f; // units per second

    private int currentIndex = 0; // moving from index to next index (wraps)
    private Vector3 startPos;
    private Vector3 endPos;
    private float travelTime; // time to traverse current segment
    private float elapsed;

    private void Start()
    {
        if (cornerWorldPoints == null || cornerWorldPoints.Length < 2)
        {
            // Provide a default small square if not set
            Vector3 p = transform.position;
            cornerWorldPoints = new Vector3[]
            {
                p + new Vector3(0,0,0),
                p + new Vector3(3,0,0),
                p + new Vector3(3,-3,0),
                p + new Vector3(0,-3,0),
            };
        }
        SetupNextSegment();
    }

    private void SetupNextSegment()
    {
        startPos = cornerWorldPoints[currentIndex];
        int next = (currentIndex + 1) % cornerWorldPoints.Length;
        endPos = cornerWorldPoints[next];
        float dist = Vector3.Distance(startPos, endPos);
        travelTime = Mathf.Max(0.0001f, dist / Mathf.Max(0.0001f, speed));
        elapsed = 0f;
    }

    private void Update()
    {
        // Linear interpolation with explicit time parameter to ensure constant speed
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);
        transform.position = Vector3.Lerp(startPos, endPos, t);

        // Play move SFX periodically (optional)
        if (AudioManager.Instance != null && Time.frameCount % 30 == 0)
        {
            AudioManager.Instance.PlaySfxMove();
        }

        if (t >= 1f)
        {
            currentIndex = (currentIndex + 1) % cornerWorldPoints.Length;
            SetupNextSegment();
        }
    }
}

