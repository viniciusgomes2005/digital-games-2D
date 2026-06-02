using UnityEngine;

public class RespawnController : MonoBehaviour
{
    [SerializeField] private bool setCheckpointOnStart;
    [SerializeField] private bool respawnPlayerOnTrigger;

    private void Start()
    {
        if (setCheckpointOnStart && GameStateController.Instance != null)
        {
            GameStateController.Instance.SetCheckpoint(transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GameStateController.Instance == null)
        {
            return;
        }

        if (respawnPlayerOnTrigger)
        {
            GameStateController.Instance.RespawnPlayer();
            return;
        }

        GameStateController.Instance.SetCheckpoint(transform.position);
    }
}
