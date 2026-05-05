using UnityEngine;
using UnityEngine.InputSystem;

public class HeroAnimator : MonoBehaviour
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

        // Sempre olha pro vilão mais próximo se estiver perto
        LookAtNearestVillainIfClose();
    }

    void LookAtNearestVillainIfClose()
    {
        GameObject[] villains = GameObject.FindGameObjectsWithTag("Villain");
        if (villains.Length == 0) return;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (GameObject v in villains)
        {
            float dist = Vector2.Distance(transform.position, v.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = v;
            }
        }

        // Se o vilão mais próximo estiver dentro do alcance de ataque, vira pra ele
        if (nearest != null && minDist < 2f)
        {
            Vector2 dir = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
            lastDirection = dir;
        }
    }

    public void TriggerAttack()
    {
        if (!isAttacking)
        {
            isAttacking  = true;
            attackTimer  = 0f;
            currentFrame = 0;
            frameTimer   = 0f;

            // Vira pro vilão mais próximo
            LookAtNearestVillain();
        }
    }

    void LookAtNearestVillain()
    {
        GameObject[] villains = GameObject.FindGameObjectsWithTag("Villain");
        if (villains.Length == 0) return;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (GameObject v in villains)
        {
            float dist = Vector2.Distance(transform.position, v.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = v;
            }
        }

        if (nearest != null)
        {
            Vector2 dir = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
            lastDirection = dir;
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
        Vector2 input = GetInput();

        if (input.magnitude > 0.1f)
        {
            lastDirection = input;
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / walkFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % WALK_FRAMES_PER_ROW;
                UpdateSprites(false);
            }
        }
        else
        {
            currentFrame = 0;
            UpdateSprites(false);
        }
    }

    Vector2 GetInput()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    input.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  input.y -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  input.x -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;
        return input.normalized;
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
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            return lastDirection.x > 0 ? DIR_RIGHT : DIR_LEFT;
        else
            return lastDirection.y > 0 ? DIR_UP : DIR_DOWN;
    }
}
