using UnityEngine;

public class SoldierAnimator : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] attackFrames;

    [Header("Referências")]
    public SpriteRenderer spriteRenderer;

    [Header("Animação")]
    public float frameRate = 12f;

    private int currentFrame = 0;
    private float frameTimer = 0f;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (attackFrames == null || attackFrames.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % attackFrames.Length;
            spriteRenderer.sprite = attackFrames[currentFrame];
        }
    }
}
