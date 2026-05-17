using UnityEngine;

// Controla a entrada do vilão pela porta:
//   - Desabilita colliders (atravessa a parede)
//   - Desabilita o controller (VillainController/MageAttack) → vilão NÃO persegue alvo
//   - Move o vilão DIRETO em linha reta vertical, do spawn até o centro do mapa
//   - Só se move se há ao menos uma carta no mapa (Hero/Golem/Mage/Lich)
//   - Quando entra nos bounds com folga e sem overlap, restaura tudo
[DisallowMultipleComponent]
public class WallPasser : MonoBehaviour
{
    [Tooltip("Quanto dentro dos bounds o vilão precisa estar antes de restaurar.")]
    public float margemRestauracao = 0.6f;

    private Collider2D[] meusColliders;
    private Vector2 boundsMin;
    private Vector2 boundsMax;
    private LayerMask layerObstaculos;
    private Vector2 spawnPos;
    private bool ativo;
    // Quando definido, só restaura quando rb.position.y <= yRestauracao.
    // Útil para garantir que o vilão atravesse portas interiores antes de ligar IA.
    private float yRestauracao = float.NegativeInfinity;

    private VillainController controller;
    private bool controllerEstavaAtivo;
    private LayerMask layerOriginal;
    private bool layerSalva;

    private MageAttack mago;
    private bool magoEstavaAtivo;

    private Rigidbody2D rb;
    private float moveSpeedEntrada = 3f;

    public void Configurar(Vector2 min, Vector2 max, LayerMask obstaculos, Vector2 spawnInicial, float yRestauracaoMin = float.NegativeInfinity)
    {
        boundsMin       = min;
        boundsMax       = max;
        layerObstaculos = obstaculos;
        spawnPos        = spawnInicial;
        yRestauracao    = yRestauracaoMin;
        meusColliders   = GetComponentsInChildren<Collider2D>();
        controller      = GetComponent<VillainController>();
        mago            = GetComponent<MageAttack>();
        rb              = GetComponent<Rigidbody2D>();

        if (meusColliders == null || meusColliders.Length == 0)
        {
            Destroy(this);
            return;
        }

        // Desabilita colliders — atravessa qualquer parede.
        foreach (var c in meusColliders)
            if (c != null) c.enabled = false;

        // Snap posição para o ponto exato do spawn (Instantiate pode ter pequenas variações).
        if (rb != null)
        {
            rb.position = spawnInicial;
        }
        transform.position = new Vector3(spawnInicial.x, spawnInicial.y, transform.position.z);

        // Desabilita controller e mago — WallPasser controla movimento durante a entrada.
        // Assim o vilão NÃO persegue cartas até entrar no mapa.
        if (controller != null)
        {
            moveSpeedEntrada      = controller.moveSpeed;
            controllerEstavaAtivo = controller.enabled;
            controller.enabled    = false;

            layerOriginal            = controller.obstacleLayer;
            controller.obstacleLayer = 0;
            layerSalva               = true;
        }
        if (mago != null)
        {
            moveSpeedEntrada = mago.moveSpeed;
            magoEstavaAtivo  = mago.enabled;
            mago.enabled     = false;
        }

        ativo = true;
    }

    void FixedUpdate()
    {
        if (!ativo || rb == null) return;

        // Só avança se há ao menos uma carta jogada (alvo) no mapa.
        if (!ExisteAlvoNoMapa())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Move VERTICALMENTE em direção ao centro do mapa, mantendo X do spawn.
        float yCentro = (boundsMin.y + boundsMax.y) * 0.5f;
        float dirY    = Mathf.Sign(yCentro - rb.position.y);
        rb.linearVelocity = new Vector2(0f, dirY * moveSpeedEntrada);
    }

    void LateUpdate()
    {
        if (!ativo || rb == null) return;
        // Segurança: força X = spawn caso algo mude.
        if (rb.position.x != spawnPos.x)
            rb.position = new Vector2(spawnPos.x, rb.position.y);
    }

    void Update()
    {
        if (!ativo) return;

        Vector3 pos = transform.position;
        bool dentroBounds =
            pos.x >= boundsMin.x + margemRestauracao && pos.x <= boundsMax.x - margemRestauracao &&
            pos.y >= boundsMin.y + margemRestauracao && pos.y <= boundsMax.y - margemRestauracao;

        bool passouPorta = float.IsNegativeInfinity(yRestauracao) || rb.position.y <= yRestauracao;

        if (dentroBounds && passouPorta && !SobrepoeObstaculo())
        {
            Restaurar();
            ativo = false;
            Destroy(this);
        }
    }

    bool SobrepoeObstaculo()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, layerObstaculos);
        return hit != null && !hit.isTrigger;
    }

    static bool ExisteAlvoNoMapa()
    {
        if (LichHealth.Instance != null) return true;
        if (GameObject.FindWithTag("Player") != null) return true;
        if (Object.FindFirstObjectByType<GolemHealth>() != null) return true;
        if (Object.FindFirstObjectByType<MageHealth>() != null) return true;
        return false;
    }

    void OnDestroy()
    {
        if (ativo) Restaurar();
    }

    void Restaurar()
    {
        if (meusColliders != null)
        {
            foreach (var c in meusColliders)
                if (c != null) c.enabled = true;
        }

        if (controller != null)
        {
            controller.enabled = controllerEstavaAtivo;
            if (layerSalva)
            {
                controller.obstacleLayer = layerOriginal;
                layerSalva = false;
            }
        }
        if (mago != null) mago.enabled = magoEstavaAtivo;
    }
}
