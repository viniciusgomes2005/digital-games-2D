using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private MobileButtonAction action;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float pressedAlpha = 0.9f;
    [SerializeField] private float pressedScale = 0.95f;

    private Image image;
    private CanvasGroup canvasGroup;
    private Vector3 defaultScale;
    private bool isPressed;

    private void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        defaultScale = transform.localScale;
        ApplyVisual(false);
    }

    private void OnDisable()
    {
        if (isPressed)
        {
            ReleaseAction();
        }

        isPressed = false;
        ApplyVisual(false);
    }

    public void Configure(MobileButtonAction buttonAction)
    {
        action = buttonAction;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        PressAction();
        ApplyVisual(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        ReleaseAction();
        ApplyVisual(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed || action == MobileButtonAction.Jump || action == MobileButtonAction.Attack)
        {
            return;
        }

        isPressed = false;
        ReleaseAction();
        ApplyVisual(false);
    }

    private void PressAction()
    {
        MobileInputController input = MobileInputController.Instance;
        if (input == null)
        {
            return;
        }

        switch (action)
        {
            case MobileButtonAction.Left:
                input.PressLeft();
                break;
            case MobileButtonAction.Right:
                input.PressRight();
                break;
            case MobileButtonAction.Down:
                input.PressDown();
                break;
            case MobileButtonAction.Jump:
                input.PressJump();
                break;
            case MobileButtonAction.Attack:
                input.PressAttack();
                break;
        }
    }

    private void ReleaseAction()
    {
        MobileInputController input = MobileInputController.Instance;
        if (input == null)
        {
            return;
        }

        switch (action)
        {
            case MobileButtonAction.Left:
                input.ReleaseLeft();
                break;
            case MobileButtonAction.Right:
                input.ReleaseRight();
                break;
            case MobileButtonAction.Down:
                input.ReleaseDown();
                break;
        }
    }

    private void ApplyVisual(bool pressed)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = pressed ? pressedAlpha : normalAlpha;
        }

        if (image != null)
        {
            image.raycastTarget = true;
        }

        transform.localScale = defaultScale * (pressed ? pressedScale : 1f);
    }

    public enum MobileButtonAction
    {
        Left,
        Right,
        Down,
        Jump,
        Attack
    }
}
