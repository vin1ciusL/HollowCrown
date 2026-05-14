using UnityEngine;

public class GolemAnimator : MonoBehaviour
{
    [Header("Sprites - Walk")]
    public Sprite[] walkFrames;

    [Header("Sprites - Attack")]
    public Sprite[] attackFrames;

    [Header("Referências")]
    public SpriteRenderer bodyRenderer;

    [Header("Animação")]
    public float walkFrameRate = 6f;
    public float attackFrameRate = 10f;

    private const int WALK_FRAMES_PER_ROW   = 8;
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

        void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyRenderer != null) bodyRenderer.enabled = true;
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

            if (cachedNearestVillain != null)
            {
                Vector2 dir = ((Vector2)cachedNearestVillain.transform.position - (Vector2)transform.position).normalized;
                lastDirection = dir;
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
            UpdateAttackSprite();
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
                UpdateWalkSprite();
            }
        }
        else
        {
            currentFrame = 0;
            UpdateWalkSprite();
        }
    }

    void UpdateAttackSprite()
    {
        int direction  = GetDirection();
        int frameIndex = direction * ATTACK_FRAMES_PER_ROW + currentFrame;

        if (attackFrames != null && frameIndex < attackFrames.Length && bodyRenderer != null)
            bodyRenderer.sprite = attackFrames[frameIndex];
    }

    void UpdateWalkSprite()
    {
        int direction  = GetDirection();
        int frameIndex = direction * WALK_FRAMES_PER_ROW + currentFrame;

        if (walkFrames != null && frameIndex < walkFrames.Length && bodyRenderer != null)
            bodyRenderer.sprite = walkFrames[frameIndex];
    }

    int GetDirection()
    {
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            return lastDirection.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return lastDirection.y > 0 ? DIR_UP : DIR_DOWN;
    }
}