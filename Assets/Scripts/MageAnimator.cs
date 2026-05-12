using UnityEngine;

public class MageAnimator : MonoBehaviour
{
    [Header("Sprites - Idle")]
    public Sprite[] idleFrames;

    [Header("Sprites - Cast (lançar bola de fogo)")]
    public Sprite[] castFrames;

    [Header("Referências")]
    public SpriteRenderer spriteRenderer;

    [Header("Velocidade")]
    public float idleFrameRate = 6f;
    public float castFrameRate = 10f;

    private bool isCasting = false;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private float castTimer = 0f;
    private float castDuration = 0f;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isCasting)
            HandleCastAnimation();
        else
            HandleIdleAnimation();
    }

    void HandleIdleAnimation()
    {
        if (idleFrames == null || idleFrames.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / idleFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % idleFrames.Length;
            spriteRenderer.sprite = idleFrames[currentFrame];
        }
    }

    void HandleCastAnimation()
    {
        if (castFrames == null || castFrames.Length == 0) { isCasting = false; return; }

        castTimer += Time.deltaTime;
        frameTimer += Time.deltaTime;

        if (frameTimer >= 1f / castFrameRate)
        {
            frameTimer = 0f;
            currentFrame = Mathf.Min(currentFrame + 1, castFrames.Length - 1);
            spriteRenderer.sprite = castFrames[currentFrame];
        }

        if (castTimer >= castDuration)
        {
            isCasting = false;
            currentFrame = 0;
            frameTimer = 0f;
        }
    }

    public void TriggerCast()
    {
        if (castFrames == null || castFrames.Length == 0) return;
        isCasting = true;
        castTimer = 0f;
        currentFrame = 0;
        frameTimer = 0f;
        castDuration = castFrames.Length / castFrameRate;
    }
}
