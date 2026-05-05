using UnityEngine;

public class VillainAnimator : MonoBehaviour
{
    [Header("Sprites - Walk Body")]
    public Sprite[] walkBodyFrames;

    [Header("Sprites - Walk Head")]
    public Sprite[] walkHeadFrames;

    [Header("Sprites - Attack Body")]
    public Sprite[] attackBodyFrames;

    [Header("Sprites - Attack Head")]
    public Sprite[] attackHeadFrames;

    [Header("Referências")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;

    [Header("Animação")]
    public float walkFrameRate = 8f;
    public float attackFrameRate = 12f;

    private const int WALK_FRAMES_PER_ROW   = 6;
    private const int ATTACK_FRAMES_PER_ROW = 7;

    private const int DIR_DOWN  = 0;
    private const int DIR_LEFT  = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_UP    = 3;

    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Rigidbody2D rb;

    [Header("Efeito de Espada")]
    public SwordEffect swordEffect;

    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0f;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        attackDuration = ATTACK_FRAMES_PER_ROW / attackFrameRate;
    }

    void Update()
    {
        if (isAttacking)
            HandleAttackAnimation();
        else
            HandleWalkAnimation();
    }

    public void TriggerAttack()
    {
        if (!isAttacking)
        {
            isAttacking  = true;
            attackTimer  = 0f;
            currentFrame = 0;
            frameTimer   = 0f;

            // Toca o efeito de espada na direção do herói
            if (swordEffect != null)
            {
                GameObject hero = GameObject.FindWithTag("Player");
                if (hero != null)
                {
                    Vector2 dir = ((Vector2)hero.transform.position - (Vector2)transform.position).normalized;
                    swordEffect.PlaySlash(dir);
                }
            }
        }
    }

    void HandleAttackAnimation()
    {
        attackTimer += Time.deltaTime;
        frameTimer  += Time.deltaTime;

        if (frameTimer >= 1f / attackFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % ATTACK_FRAMES_PER_ROW;
            UpdateSprites(true);
        }

        if (attackTimer >= attackDuration)
        {
            isAttacking  = false;
            currentFrame = 0;
            frameTimer   = 0f;
        }
    }

    void HandleWalkAnimation()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / walkFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % WALK_FRAMES_PER_ROW;
            UpdateSprites(false);
        }
    }

    void UpdateSprites(bool attacking)
    {
        int direction    = GetDirection();
        int framesPerRow = attacking ? ATTACK_FRAMES_PER_ROW : WALK_FRAMES_PER_ROW;
        int frameIndex   = direction * framesPerRow + currentFrame;

        Sprite[] bodyFrames = attacking ? attackBodyFrames : walkBodyFrames;
        Sprite[] headFrames = attacking ? attackHeadFrames : walkHeadFrames;

        if (bodyFrames != null && frameIndex < bodyFrames.Length && bodyRenderer != null)
            bodyRenderer.sprite = bodyFrames[frameIndex];

        if (headFrames != null && frameIndex < headFrames.Length && headRenderer != null)
            headRenderer.sprite = headFrames[frameIndex];
    }

    int GetDirection()
    {
        // Sempre olha pro herói
        GameObject hero = GameObject.FindWithTag("Player");
        if (hero != null)
        {
            Vector2 dir = ((Vector2)hero.transform.position - (Vector2)transform.position).normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                return dir.x > 0 ? DIR_RIGHT : DIR_LEFT;
            else
                return dir.y > 0 ? DIR_UP : DIR_DOWN;
        }

        if (rb == null) return DIR_DOWN;
        Vector2 vel = rb.linearVelocity;
        if (vel.magnitude < 0.1f) return DIR_DOWN;
        if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
            return vel.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return vel.y > 0 ? DIR_UP : DIR_DOWN;
    }
}
