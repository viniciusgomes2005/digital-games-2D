using UnityEngine;

public class MobileInputController : MonoBehaviour
{
    public static MobileInputController Instance { get; private set; }

    private bool leftHeld;
    private bool rightHeld;
    private bool downHeld;
    private bool jumpQueued;
    private bool attackQueued;

    public float HorizontalInput
    {
        get
        {
            if (leftHeld == rightHeld)
            {
                return 0f;
            }

            return rightHeld ? 1f : -1f;
        }
    }

    public bool DownHeld => downHeld;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PressLeft()
    {
        leftHeld = true;
    }

    public void ReleaseLeft()
    {
        leftHeld = false;
    }

    public void PressRight()
    {
        rightHeld = true;
    }

    public void ReleaseRight()
    {
        rightHeld = false;
    }

    public void PressDown()
    {
        downHeld = true;
    }

    public void ReleaseDown()
    {
        downHeld = false;
    }

    public void PressJump()
    {
        jumpQueued = true;
    }

    public void PressAttack()
    {
        attackQueued = true;
    }

    public bool ConsumeJumpPressed()
    {
        if (!jumpQueued)
        {
            return false;
        }

        jumpQueued = false;
        return true;
    }

    public bool ConsumeAttackPressed()
    {
        if (!attackQueued)
        {
            return false;
        }

        attackQueued = false;
        return true;
    }
}
