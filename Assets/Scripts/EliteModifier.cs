using UnityEngine;

public enum TipoElite
{
    Frenetico,
    Colosso,
    Sanguessuga,
    Volatil,
    Venenoso
}

[DisallowMultipleComponent]
public class EliteModifier : MonoBehaviour
{
    // Preenchido pelo VillainSpawner antes de chamar AplicarModificadores()
    public TipoElite[] modificadores = new TipoElite[0];

    [Header("Explosão (Volátil)")]
    public float explosaoRaio = 2.5f;
    public float explosaoDano = 15f;

    [Header("DoT (Venenoso)")]
    public float dotDanoPorSegundo = 5f;
    public float dotDuracao = 3f;

    [Header("Cura (Sanguessuga)")]
    [Range(0f, 1f)]
    public float porcentagemCura = 0.20f;

    private VillainHealth vh;
    private VillainController vc;
    private SpriteRenderer[] srs;

    void Awake()
    {
        vh = GetComponent<VillainHealth>();
        vc = GetComponent<VillainController>();
        srs = GetComponentsInChildren<SpriteRenderer>();
    }

    // Chamado pelo VillainSpawner logo após AddComponent + configuração de modificadores[]
    public void AplicarModificadores()
    {
        if (modificadores == null || modificadores.Length == 0) return;

        foreach (var mod in modificadores)
            AplicarEstatisticas(mod);

        // Almas proporcionais à quantidade de modifiers
        if (vh != null)
            vh.almasRecompensa = Mathf.RoundToInt(vh.almasRecompensa * (1f + 0.5f * modificadores.Length));

        AplicarVisuais();
    }

    // ─── Efeitos por tipo ───────────────────────────────────────────────────

    void AplicarEstatisticas(TipoElite mod)
    {
        switch (mod)
        {
            case TipoElite.Frenetico:
                if (vc != null) vc.moveSpeed    *= 1.5f;
                if (vh != null) vh.attackCooldown *= 0.5f;
                break;

            case TipoElite.Colosso:
                if (vh != null)
                {
                    vh.maxHealth     *= 3f;
                    vh.currentHealth  = vh.maxHealth;
                }
                if (vc != null)
                {
                    vc.moveSpeed           *= 0.8f;
                    vc.resistenciaKnockback = 0.15f;
                }
                break;

            case TipoElite.Sanguessuga:
                if (vh != null)
                {
                    vh.attackDamage   *= 1.2f;
                    vh.OnDanoCausado  += CurarAoAtacar;
                }
                break;

            case TipoElite.Volatil:
                if (vh != null)
                {
                    vh.maxHealth     *= 1.2f;
                    vh.currentHealth  = vh.maxHealth;
                    vh.OnMorte       += Explodir;
                }
                break;

            case TipoElite.Venenoso:
                if (vh != null)
                    vh.OnDanoCausado += AplicarDoT;
                break;
        }
    }

    // ─── Visuais ────────────────────────────────────────────────────────────

    void AplicarVisuais()
    {
        // Cor: 1 mod = cor do tipo | 2 mods = dourado | 3 mods = branco
        Color cor;
        switch (modificadores.Length)
        {
            case 1:  cor = CorPorTipo(modificadores[0]); break;
            case 2:  cor = new Color(1f, 0.85f, 0.1f);  break; // dourado
            default: cor = Color.white;                   break; // branco/prateado
        }
        SetColor(cor);

        // Escala: Colosso já aumenta; mods extras somam um pouco mais
        bool temColosso = System.Array.Exists(modificadores, m => m == TipoElite.Colosso);
        float escala = temColosso ? 1.3f : 1f;
        if (modificadores.Length == 2) escala += 0.10f;
        if (modificadores.Length >= 3) escala += 0.20f;
        if (escala > 1f) transform.localScale *= escala;
    }

    static Color CorPorTipo(TipoElite mod)
    {
        switch (mod)
        {
            case TipoElite.Frenetico:   return new Color(1f,    0.5f,  0f);
            case TipoElite.Colosso:     return new Color(0.65f, 0f,    0f);
            case TipoElite.Sanguessuga: return new Color(0.7f,  0f,    0.85f);
            case TipoElite.Volatil:     return new Color(1f,    0.15f, 0f);
            case TipoElite.Venenoso:    return new Color(0.1f,  0.85f, 0.1f);
            default:                    return Color.white;
        }
    }

    // ─── Callbacks especiais ────────────────────────────────────────────────

    void CurarAoAtacar(float dano, MonoBehaviour alvo)
    {
        if (vh == null) return;
        vh.currentHealth = Mathf.Min(vh.currentHealth + dano * porcentagemCura, vh.maxHealth);
    }

    void AplicarDoT(float dano, MonoBehaviour alvo)
    {
        if (alvo == null) return;
        DotEffect dot = alvo.GetComponent<DotEffect>();
        if (dot != null) { dot.Renovar(dotDuracao); return; }
        dot = alvo.gameObject.AddComponent<DotEffect>();
        dot.Inicializar(dotDanoPorSegundo, dotDuracao);
    }

    void Explodir()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosaoRaio);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            HeroHealth hh = hit.GetComponent<HeroHealth>();
            if (hh != null) { hh.TakeDamage(explosaoDano); continue; }
            LichHealth lh = hit.GetComponent<LichHealth>();
            if (lh != null) { lh.TakeDamage(explosaoDano); continue; }
            MageHealth mh = hit.GetComponent<MageHealth>();
            if (mh != null) { mh.TakeDamage(explosaoDano); }
        }
    }

    void SetColor(Color cor)
    {
        foreach (var sr in srs)
            if (sr != null) sr.color = cor;
    }

    void OnDestroy()
    {
        if (vh == null) return;
        vh.OnDanoCausado -= CurarAoAtacar;
        vh.OnDanoCausado -= AplicarDoT;
        vh.OnMorte       -= Explodir;
    }

    void OnDrawGizmosSelected()
    {
        if (System.Array.Exists(modificadores, m => m == TipoElite.Volatil))
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, explosaoRaio);
        }
    }
}
