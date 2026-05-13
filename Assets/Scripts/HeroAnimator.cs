using UnityEngine;

public class HeroAnimator : MonoBehaviour
{
    [Header("Sprites - Walk Body")]
    public Sprite[] walkBodyFrames;

    [Header("Sprites - Walk Head")]
    public Sprite[] walkHeadFrames;

    [Header("Sprites - Attack (spritesheet completa)")]
    public Sprite[] attackFrames;

    [Header("Referências")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;
    public SpriteRenderer attackRenderer;

    [Header("Animação")]
    public float walkFrameRate = 8f;
    public float attackFrameRate = 12f;

    private const int WALK_FRAMES_PER_ROW   = 6;
    private const int ATTACK_FRAMES_PER_ROW = 9;

    private const int DIR_UP    = 1;
    private const int DIR_DOWN  = 0;
    private const int DIR_LEFT  = 2;
    private const int DIR_RIGHT = 3;

    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Vector2 lastDirection = Vector2.down;

    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0f;

    private Rigidbody2D rb;
    private VillainHealth cachedNearestVillain;
    private float villainSearchTimer = 0f;
    private const float VILLAIN_SEARCH_INTERVAL = 0.1f;

    public event System.Action OnImpactFrame;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

        villainSearchTimer += Time.deltaTime;
        if (villainSearchTimer >= VILLAIN_SEARCH_INTERVAL)
        {
            villainSearchTimer = 0f;
            RefreshNearestVillain();
        }

        // Só vira pro vilão quando parado — movimento tem prioridade
        if (cachedNearestVillain != null && (rb == null || rb.linearVelocity.magnitude < 0.1f))
        {
            float dist = Vector2.Distance(transform.position, cachedNearestVillain.transform.position);
            if (dist < 2f)
            {
                Vector2 dir = ((Vector2)cachedNearestVillain.transform.position - (Vector2)transform.position).normalized;
                lastDirection = dir;
            }
        }
    }

    void RefreshNearestVillain()
    {
        VillainHealth nearest = null;
        float minDist = float.MaxValue;

        foreach (VillainHealth v in VillainHealth.All)
        {
            float dist = Vector2.Distance(transform.position, v.transform.position);
            if (dist < minDist) { minDist = dist; nearest = v; }
        }

        cachedNearestVillain = nearest;
    }

    public void TriggerAttack()
    {
        if (!isAttacking)
        {
            isAttacking  = true;
            attackTimer  = 0f;
            currentFrame = 0;
            frameTimer   = 0f;

            if (attackRenderer != null) attackRenderer.enabled = true;
            if (bodyRenderer != null)   bodyRenderer.enabled   = false;
            if (headRenderer != null)   headRenderer.enabled   = false;

            if (cachedNearestVillain != null)
            {
                Vector2 dir = ((Vector2)cachedNearestVillain.transform.position - (Vector2)transform.position).normalized;
                lastDirection = dir;
            }
        }
    }

    public Vector2 GetFacingDirection()
    {
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            return lastDirection.x > 0 ? Vector2.right : Vector2.left;
        else
            return lastDirection.y > 0 ? Vector2.up : Vector2.down;
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
            if (bodyRenderer != null)   bodyRenderer.enabled   = true;
            if (headRenderer != null)   headRenderer.enabled   = true;
        }
    }

    void HandleWalkAnimation()
    {
        if (rb == null) return;

        Vector2 vel = rb.linearVelocity;

        if (vel.magnitude > 0.1f)
        {
            lastDirection = vel.normalized;
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / walkFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % WALK_FRAMES_PER_ROW;
                UpdateWalkSprites();
            }
        }
        else
        {
            currentFrame = 0;
            UpdateWalkSprites();
        }
    }

    void UpdateAttackSprite()
    {
        int direction  = GetDirection();
        int frameIndex = direction * ATTACK_FRAMES_PER_ROW + currentFrame;

        if (attackFrames != null && frameIndex < attackFrames.Length && attackRenderer != null)
            attackRenderer.sprite = attackFrames[frameIndex];
    }

    void UpdateWalkSprites()
    {
        int direction  = GetDirection();
        int frameIndex = direction * WALK_FRAMES_PER_ROW + currentFrame;

        if (walkBodyFrames != null && frameIndex < walkBodyFrames.Length && bodyRenderer != null)
            bodyRenderer.sprite = walkBodyFrames[frameIndex];

        if (walkHeadFrames != null && frameIndex < walkHeadFrames.Length && headRenderer != null)
            headRenderer.sprite = walkHeadFrames[frameIndex];
    }

    int GetDirection()
    {
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            return lastDirection.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return lastDirection.y > 0 ? DIR_UP : DIR_DOWN;
    }
}
