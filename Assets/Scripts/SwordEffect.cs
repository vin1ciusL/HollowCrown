using UnityEngine;

public class SwordEffect : MonoBehaviour
{
    [Header("Sprites - Slash Frente")]
    public Sprite[] swordFrames;

    [Header("Sprites - Slash Costas")]
    public Sprite[] swordBackFrames;

    [Header("Referências")]
    public SpriteRenderer swordRenderer;

    [Header("Animação")]
    public float frameRate = 14f;

    private const int FRAMES_PER_ROW = 14;

    private bool isPlaying = false;
    private float frameTimer = 0f;
    private int currentFrame = 0;
    private bool useBack = false;

    void Awake()
    {
        if (swordRenderer != null)
            swordRenderer.enabled = false;
    }

    public void PlaySlash(Vector2 direction)
    {
        if (isPlaying) return;

        useBack = (direction.y < -0.5f || direction.x < -0.5f);

        // Rotaciona o slash na direção do ataque
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle);

        // Posiciona o slash na frente do personagem na direção do ataque
        transform.localPosition = direction * 0.5f;

        isPlaying    = true;
        currentFrame = 0;
        frameTimer   = 0f;

        if (swordRenderer != null)
        {
            swordRenderer.enabled = true;
            UpdateFrame();
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame++;

            if (currentFrame >= FRAMES_PER_ROW)
            {
                // Animação terminou
                isPlaying = false;
                if (swordRenderer != null)
                    swordRenderer.enabled = false;
                return;
            }

            UpdateFrame();
        }
    }

    void UpdateFrame()
    {
        Sprite[] frames = useBack ? swordBackFrames : swordFrames;
        if (frames == null || currentFrame >= frames.Length) return;
        if (swordRenderer != null)
            swordRenderer.sprite = frames[currentFrame];
    }
}
