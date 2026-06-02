using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class VictoryPlatformHoldTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float requiredStayTime = 2f;
    [SerializeField] private string victorySceneName = "Vitoria";
    [SerializeField] private Vector2 overlapPadding = new Vector2(0.1f, 0.35f);

    private Collider2D[] platformColliders;
    private Transform player;
    private Collider2D playerCollider;
    private float stayTimer;
    private bool victoryTriggered;

    private void Awake()
    {
        platformColliders = GetComponentsInChildren<Collider2D>();
        ResolvePlayer();
    }

    private void OnValidate()
    {
        requiredStayTime = Mathf.Max(0f, requiredStayTime);
        overlapPadding.x = Mathf.Max(0f, overlapPadding.x);
        overlapPadding.y = Mathf.Max(0f, overlapPadding.y);
    }

    private void Update()
    {
        if (victoryTriggered)
        {
            return;
        }

        ResolvePlayer();

        if (IsPlayerOnThisPlatform())
        {
            AdvanceTimer();
            return;
        }

        stayTimer = 0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsPlayer(collision.collider))
        {
            return;
        }

        AdvanceTimer();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsPlayer(collision.collider))
        {
            stayTimer = 0f;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        AdvanceTimer();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            stayTimer = 0f;
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        Transform current = other != null ? other.transform : null;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void AdvanceTimer()
    {
        stayTimer += Time.deltaTime;
        if (!victoryTriggered && stayTimer >= requiredStayTime)
        {
            victoryTriggered = true;
            SceneManager.LoadScene(victorySceneName);
        }
    }

    private void ResolvePlayer()
    {
        if (player != null && playerCollider != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerCollider = playerObject.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            playerCollider = playerObject.GetComponentInChildren<Collider2D>();
        }
    }

    private bool IsPlayerOnThisPlatform()
    {
        if (playerCollider == null || platformColliders == null || platformColliders.Length == 0)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        foreach (Collider2D platformCollider in platformColliders)
        {
            if (platformCollider == null || platformCollider.isTrigger)
            {
                continue;
            }

            Bounds platformBounds = platformCollider.bounds;
            bool horizontallyInside = playerBounds.max.x >= platformBounds.min.x - overlapPadding.x
                && playerBounds.min.x <= platformBounds.max.x + overlapPadding.x;
            bool standingOnTop = playerBounds.min.y >= platformBounds.max.y - overlapPadding.y
                && playerBounds.min.y <= platformBounds.max.y + overlapPadding.y;

            if (horizontallyInside && standingOnTop)
            {
                return true;
            }
        }

        return false;
    }
}
