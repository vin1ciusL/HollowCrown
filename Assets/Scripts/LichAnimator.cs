using UnityEngine;

public class LichAnimator : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] idleFrames;
    public Sprite[] attackFrames;

    [Header("Velocidade")]
    public float idleFrameRate = 8f;
    public float attackFrameRate = 12f;

    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isAttacking = false;
    private float attackDuration = 0f;
    private float attackTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                isAttacking = false;
                currentFrame = 0;
                frameTimer = 0f;
            }
            else
            {
                Animate(attackFrames, attackFrameRate);
            }
            return;
        }

        Animate(idleFrames, idleFrameRate);
    }

    void Animate(Sprite[] frames, float frameRate)
    {
        if (frames == null || frames.Length == 0 || sr == null) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            sr.sprite = frames[currentFrame];
        }
    }

    public void TriggerAttack()
    {
        if (attackFrames == null || attackFrames.Length == 0) return;
        isAttacking = true;
        attackTimer = 0f;
        currentFrame = 0;
        frameTimer = 0f;
        attackDuration = attackFrames.Length / attackFrameRate;
    }
}
