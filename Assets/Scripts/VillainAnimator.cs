using UnityEngine;

public class VillainAnimator : MonoBehaviour
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

    // Cada linha da spritesheet é uma direção
    // Linha 0 = baixo, Linha 1 = esquerda, Linha 2 = direita, Linha 3 = cima
    // 6 frames por linha
    private const int FRAMES_PER_ROW = 6;
    private const int DIR_DOWN  = 0;
    private const int DIR_LEFT  = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_UP    = 3;

    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % FRAMES_PER_ROW;
            UpdateSprites();
        }
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
        if (rb == null) return DIR_DOWN;

        Vector2 vel = rb.linearVelocity;

        if (vel.magnitude < 0.1f) return DIR_DOWN;

        if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
            return vel.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return vel.y > 0 ? DIR_UP : DIR_DOWN;
    }
}
