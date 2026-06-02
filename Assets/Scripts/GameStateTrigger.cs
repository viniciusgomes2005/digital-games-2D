using UnityEngine;

public class GameStateTrigger : MonoBehaviour
{
    [SerializeField] private TriggerResult result = TriggerResult.Victory;
    [SerializeField] private bool onlyPlayer = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (onlyPlayer && !other.CompareTag("Player"))
        {
            return;
        }

        if (GameStateController.Instance == null)
        {
            return;
        }

        switch (result)
        {
            case TriggerResult.Victory:
                GameStateController.Instance.Victory();
                break;
            case TriggerResult.Defeat:
                GameStateController.Instance.Defeat();
                break;
            case TriggerResult.Respawn:
                GameStateController.Instance.RespawnPlayer();
                break;
        }
    }

    public enum TriggerResult
    {
        Victory,
        Defeat,
        Respawn
    }
}
