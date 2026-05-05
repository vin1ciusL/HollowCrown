using UnityEngine;
using UnityEngine.InputSystem;

public class HeroAnimator : MonoBehaviour
{
    [Header("Sprites - Body")]
    public Sprite[] bodyFrames;

    [Header("Sprites - Head")]
    public Sprite[] headFrames;

    [Header("Referências")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;

    [Header("Animação")]
    public float frameRate = 8f;

    private const int FRAMES_PER_ROW = 6;
    private const int DIR_UP    = 1;
    private const int DIR_DOWN  = 0;
    private const int DIR_LEFT  = 2;
    private const int DIR_RIGHT = 3;

    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Vector2 lastDirection = Vector2.down;

    void Update()
    {
        Vector2 input = GetInput();

        if (input.magnitude > 0.1f)
        {
            lastDirection = input;
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % FRAMES_PER_ROW;
                UpdateSprites();
            }
        }
        else
        {
            // Parado: fica no frame 0 da direção atual
            currentFrame = 0;
            UpdateSprites();
        }
    }

    Vector2 GetInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            input.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            input.y -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            input.x -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            input.x += 1;

        return input.normalized;
    }

    void UpdateSprites()
    {
        int direction = GetDirection();
        int frameIndex = direction * FRAMES_PER_ROW + currentFrame;

        if (bodyFrames != null && frameIndex < bodyFrames.Length && bodyRenderer != null)
            bodyRenderer.sprite = bodyFrames[frameIndex];

        if (headFrames != null && frameIndex < headFrames.Length && headRenderer != null)
            headRenderer.sprite = headFrames[frameIndex];
    }

    int GetDirection()
    {
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            return lastDirection.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return lastDirection.y > 0 ? DIR_UP : DIR_DOWN;
    }
}
