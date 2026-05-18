using UnityEngine;

public class CavaleiroNivel6Animator : MonoBehaviour
{
    [Header("Sprites - Andar")]
    public Sprite[] walkFrames;

    [Header("Sprites - Atacar")]
    public Sprite[] attackFrames;

    [Header("Referências")]
    public SpriteRenderer walkRenderer;
    public SpriteRenderer attackRenderer;

    [Header("Animação")]
    public float walkFrameRate = 8f;
    public float attackFrameRate = 12f;

    private const int WALK_FRAMES_PER_ROW   = 6;
    private const int ATTACK_FRAMES_PER_ROW = 7;

    private const int DIR_DOWN  = 0;
    private const int DIR_LEFT  = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_UP    = 3;

    public event System.Action OnImpactFrame;

    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (attackRenderer != null)
            attackRenderer.enabled = false;
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
        if (isAttacking) return;

        isAttacking  = true;
        attackTimer  = 0f;
        currentFrame = 0;
        frameTimer   = 0f;

        if (attackRenderer != null) attackRenderer.enabled = true;
        if (walkRenderer != null)   walkRenderer.enabled   = false;
    }

    void HandleAttackAnimation()
    {
        attackTimer += Time.deltaTime;
        frameTimer  += Time.deltaTime;

        if (frameTimer >= 1f / attackFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % ATTACK_FRAMES_PER_ROW;

            if (currentFrame == ATTACK_FRAMES_PER_ROW / 2)
                OnImpactFrame?.Invoke();

            UpdateAttackSprite();
        }

        if (attackTimer >= attackDuration)
        {
            isAttacking  = false;
            currentFrame = 0;
            frameTimer   = 0f;

            if (attackRenderer != null) attackRenderer.enabled = false;
            if (walkRenderer != null)   walkRenderer.enabled   = true;
        }
    }

    void HandleWalkAnimation()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / walkFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % WALK_FRAMES_PER_ROW;
            UpdateWalkSprite();
        }
    }

    void UpdateAttackSprite()
    {
        int direction  = GetDirection();
        int frameIndex = direction * ATTACK_FRAMES_PER_ROW + currentFrame;
        if (attackFrames != null && frameIndex < attackFrames.Length && attackRenderer != null)
            attackRenderer.sprite = attackFrames[frameIndex];
    }

    void UpdateWalkSprite()
    {
        int direction  = GetDirection();
        int frameIndex = direction * WALK_FRAMES_PER_ROW + currentFrame;
        if (walkFrames != null && frameIndex < walkFrames.Length && walkRenderer != null)
            walkRenderer.sprite = walkFrames[frameIndex];
    }

    int GetDirection()
    {
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
