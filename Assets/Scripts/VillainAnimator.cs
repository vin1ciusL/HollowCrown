using UnityEngine;

public class VillainAnimator : MonoBehaviour
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

    public event System.Action OnImpactFrame;

    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0f;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Esconde o renderer de ataque no início
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
        if (!isAttacking)
        {
            isAttacking  = true;
            attackTimer  = 0f;
            currentFrame = 0;
            frameTimer   = 0f;

            // Mostra o renderer de ataque e esconde o de andar
            if (attackRenderer != null) attackRenderer.enabled = true;
            if (bodyRenderer != null)   bodyRenderer.enabled   = false;
            if (headRenderer != null)   headRenderer.enabled   = false;
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

            // Dispara o impacto no frame do meio da animação
            if (currentFrame == ATTACK_FRAMES_PER_ROW / 2)
                OnImpactFrame?.Invoke();

            UpdateAttackSprite();
        }

        if (attackTimer >= attackDuration)
        {
            isAttacking  = false;
            currentFrame = 0;
            frameTimer   = 0f;

            // Volta pro renderer de andar
            if (attackRenderer != null) attackRenderer.enabled = false;
            if (bodyRenderer != null)   bodyRenderer.enabled   = true;
            if (headRenderer != null)   headRenderer.enabled   = true;
        }
    }

    void HandleWalkAnimation()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / walkFrameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % WALK_FRAMES_PER_ROW;
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
