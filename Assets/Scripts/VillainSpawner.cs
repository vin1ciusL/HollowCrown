using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

public enum TipoMapa { Externo, Dungeon, Royal }

[System.Flags]
public enum LadoSpawn
{
    Nenhum   = 0,
    Cima     = 1 << 0,
    Baixo    = 1 << 1,
    Esquerda = 1 << 2,
    Direita  = 1 << 3
}

public class VillainSpawner : MonoBehaviour
{
    [Header("Mapa")]
    public TipoMapa tipoMapa = TipoMapa.Externo;

    [Header("Spawn")]
    public GameObject villainPrefab;
    public GameObject magoPrefab;
    public float spawnOffsetY = 1.5f;
    public float delayEntreSpawns = 0.3f;
    public float delayAntesDeTrocar = 2f;

    [Header("Limites do Mapa (deixe zerado para usar a câmera)")]
    [Tooltip("Canto inferior esquerdo do mapa. Se mapMin == mapMax, usa viewport da câmera.")]
    public Vector2 mapMin = Vector2.zero;
    public Vector2 mapMax = Vector2.zero;
    [Tooltip("Margem interna em unidades para evitar spawn colado na borda")]
    public float spawnMargin = 0.5f;

    [Header("Ondas")]
    public int[] inimigosPorturno = { 3, 4, 5, 6, 8, 10 };
    public int[] magosPorturno    = { 0, 0, 1, 1, 2,  2 };
    public float delayEntreOndas  = 3f;

    [Header("Elites")]
    [Tooltip("Chance (0-1) de qualquer inimigo virar elite. Definido automaticamente pelo TipoMapa via ContextMenu.")]
    [Range(0f, 1f)]
    public float chanceElite = 0.10f;
    [Tooltip("Máximo de elites por onda")]
    public int maxElitesPorOnda = 2;

    [Header("Próxima Fase")]
    public GameObject mapaAtual;
    public GameObject proximoMapa;
    public Vector2 posicaoCameraProximoMapa;

    [Header("Spawn Seguro")]
    [Tooltip("Y mínimo (mundo) que o inimigo deve atingir antes de ligar a IA. Deixe em -Infinity para usar apenas os bounds. Útil quando há portas interiores que o vilão precisa atravessar antes de agir.")]
    public float yRestauracaoEntrada = float.NegativeInfinity;
    [Tooltip("Lados (fora dos bounds) onde os inimigos podem spawnar. Pode selecionar múltiplos.")]
    public LadoSpawn ladosSpawn = LadoSpawn.Baixo;
    [Tooltip("Distância FORA dos bounds onde o inimigo aparece, em unidades")]
    [FormerlySerializedAs("offsetForaDoMapa")]
    [FormerlySerializedAs("spawnInset")]
    public float distanciaForaDoMapa = 1.5f;
    [Tooltip("Pontos fixos de spawn (ex: portas). Se preenchido, IGNORA ladosSpawn e sorteia uniformemente entre eles.")]
    public Transform[] pontosDeSpawn;
    [Tooltip("Layer dos obstáculos (paredes). Usado pelo WallPasser para evitar restaurar colisão dentro de uma parede.")]
    public LayerMask layerObstaculos = 1 << 7;
    [Tooltip("Raio para checar se o ponto de spawn está livre de obstáculos")]
    public float spawnCheckRadius = 0.5f;
    [Tooltip("Máximo de tentativas para encontrar posição válida")]
    public int maxSpawnAttempts = 15;
    [Tooltip("Collider2D do mapa — usado como bounds quando mapMin == mapMax")]
    public Collider2D mapBounds;

    private Camera cam;
    private int ondaAtual      = 0;
    private int inimigosVivos  = 0;
    private bool trocando      = false;
    private int elitesNestaOnda = 0;

    // Pool de tipos de elite fixo e alocado uma única vez
    private static readonly TipoElite[] POOL_ELITES =
    {
        TipoElite.Frenetico,
        TipoElite.Colosso,
        TipoElite.Sanguessuga,
        TipoElite.Volatil,
        TipoElite.Venenoso
    };

    // ─── Configuração automática via ContextMenu ────────────────────────────

    [ContextMenu("Auto-configurar: Externo (6 ondas / 10% elite)")]
    void ConfigurarExterno()
    {
        tipoMapa          = TipoMapa.Externo;
        chanceElite       = 0.10f;
        maxElitesPorOnda  = 2;
        inimigosPorturno  = new int[] { 3, 4, 5,  6,  8, 10 };
        magosPorturno     = new int[] { 0, 0, 1,  1,  2,  2 };
        ladosSpawn        = LadoSpawn.Baixo;
    }

    [ContextMenu("Auto-configurar: Dungeon (8 ondas / 20% elite)")]
    void ConfigurarDungeon()
    {
        tipoMapa          = TipoMapa.Dungeon;
        chanceElite       = 0.20f;
        maxElitesPorOnda  = 3;
        inimigosPorturno  = new int[] { 4, 5, 6,  7,  8,  9, 11, 13 };
        magosPorturno     = new int[] { 0, 1, 1,  2,  2,  2,  3,  3 };
    }

    [ContextMenu("Auto-configurar: Royal (10 ondas / 30% elite)")]
    void ConfigurarRoyal()
    {
        tipoMapa          = TipoMapa.Royal;
        chanceElite       = 0.30f;
        maxElitesPorOnda  = 4;
        inimigosPorturno  = new int[] { 5, 6, 7,  8,  9, 10, 11, 13, 14, 16 };
        magosPorturno     = new int[] { 0, 1, 1,  2,  2,  3,  3,  3,  4,  4 };
    }

    // ─── Ciclo de vida ──────────────────────────────────────────────────────

    void OnEnable()
    {
        cam = Camera.main;
        ondaAtual = 0;
        trocando  = false;

        if (SoulManager.Instance != null)
            SoulManager.Instance.ResetarParaFase();

        IniciarTurno();
    }

    void IniciarTurno()
    {
        trocando = false;

        if (ondaAtual >= inimigosPorturno.Length)
        {
            Debug.Log("[VillainSpawner] Todas as ondas concluídas — iniciando fim de fase.");
            StartCoroutine(FinalizarFase());
            return;
        }

        int qtdVilao = inimigosPorturno[ondaAtual];
        int qtdMago  = (magosPorturno != null && ondaAtual < magosPorturno.Length) ? magosPorturno[ondaAtual] : 0;
        if (magoPrefab == null) qtdMago = 0;

        Debug.Log($"[VillainSpawner] Onda {ondaAtual + 1}/{inimigosPorturno.Length} — {qtdVilao} vilões + {qtdMago} magos");
        inimigosVivos = qtdVilao + qtdMago;
        StartCoroutine(SpawnarTurno(qtdVilao, qtdMago));
    }

    IEnumerator SpawnarTurno(int qtdVilao, int qtdMago)
    {
        elitesNestaOnda = 0;

        for (int i = 0; i < qtdVilao; i++)
        {
            SpawnInimigo(villainPrefab, podeSerElite: true);
            yield return new WaitForSeconds(delayEntreSpawns);
        }
        for (int i = 0; i < qtdMago; i++)
        {
            SpawnInimigo(magoPrefab, podeSerElite: false);
            yield return new WaitForSeconds(delayEntreSpawns);
        }
    }

    void SpawnInimigo(GameObject prefab, bool podeSerElite)
    {
        if (prefab == null) return;

        Vector3 spawnPos = EncontrarPosicaoSegura();
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[VillainSpawner] Não foi possível encontrar posição segura para spawn!");
            spawnPos   = cam.transform.position;
            spawnPos.z = 0f;
        }

        GameObject v = Instantiate(prefab, spawnPos, Quaternion.identity);

        // One-way: vilão atravessa qualquer obstáculo até entrar nos bounds,
        // descendo/subindo na linha X do ponto de spawn (= centro da porta).
        Vector2 boundsMin, boundsMax;
        ObterBounds(out boundsMin, out boundsMax);
        WallPasser passer = v.AddComponent<WallPasser>();
        passer.Configurar(boundsMin, boundsMax, layerObstaculos, (Vector2)spawnPos, yRestauracaoEntrada);

        if (podeSerElite && elitesNestaOnda < maxElitesPorOnda && Random.value < chanceElite)
            TornarElite(v);

        VillainHealth vh = v.GetComponent<VillainHealth>();
        if (vh != null)
            vh.OnMorte += OnInimigoMorreu;
    }

    // ─── Sistema de elite ───────────────────────────────────────────────────

    void TornarElite(GameObject inimigo)
    {
        int maxMods = tipoMapa switch
        {
            TipoMapa.Dungeon => 2,
            TipoMapa.Royal   => 3,
            _                => 1
        };

        // Seleciona modificadores únicos aleatoriamente
        var pool      = new List<TipoElite>(POOL_ELITES);
        var escolhidos = new List<TipoElite>();

        // 1º modifier: garantido
        int idx = Random.Range(0, pool.Count);
        escolhidos.Add(pool[idx]);
        pool.RemoveAt(idx);

        // Modifiers extras: 50% de chance cada, até o limite da fase
        for (int i = 1; i < maxMods && pool.Count > 0; i++)
        {
            if (Random.value >= 0.5f) break;
            idx = Random.Range(0, pool.Count);
            escolhidos.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        EliteModifier elite = inimigo.AddComponent<EliteModifier>();
        elite.modificadores = escolhidos.ToArray();
        elite.AplicarModificadores();
        elitesNestaOnda++;

        Debug.Log($"[VillainSpawner] Elite onda {ondaAtual + 1} — {escolhidos.Count} mod(s): {string.Join("+", escolhidos)}");
    }

    // ─── Posição segura ─────────────────────────────────────────────────────

    void ObterBounds(out Vector2 boundsMin, out Vector2 boundsMax)
    {
        if (mapMin != mapMax)
        {
            boundsMin = mapMin;
            boundsMax = mapMax;
        }
        else if (mapBounds != null)
        {
            var b = mapBounds.bounds;
            boundsMin = b.min;
            boundsMax = b.max;
        }
        else
        {
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
            boundsMin = new Vector2(bl.x, bl.y);
            boundsMax = new Vector2(tr.x, tr.y);
        }
    }

    Vector3 EncontrarPosicaoSegura()
    {
        // Se há pontos de spawn fixos (ex: portas), sorteia entre eles.
        if (pontosDeSpawn != null && pontosDeSpawn.Length > 0)
        {
            // Filtra refs vazias para não enviesar o sorteio em slots não preenchidos.
            var validos = new List<Transform>(pontosDeSpawn.Length);
            foreach (var t in pontosDeSpawn)
                if (t != null) validos.Add(t);

            if (validos.Count > 0)
            {
                Transform escolhido = validos[Random.Range(0, validos.Count)];
                Vector3 p = escolhido.position;
                p.z = 0f;
                return p;
            }
        }

        Vector2 boundsMin, boundsMax;
        ObterBounds(out boundsMin, out boundsMax);

        var ladosAtivos = new List<LadoSpawn>(4);
        if ((ladosSpawn & LadoSpawn.Cima)     != 0) ladosAtivos.Add(LadoSpawn.Cima);
        if ((ladosSpawn & LadoSpawn.Baixo)    != 0) ladosAtivos.Add(LadoSpawn.Baixo);
        if ((ladosSpawn & LadoSpawn.Esquerda) != 0) ladosAtivos.Add(LadoSpawn.Esquerda);
        if ((ladosSpawn & LadoSpawn.Direita)  != 0) ladosAtivos.Add(LadoSpawn.Direita);

        if (ladosAtivos.Count == 0)
        {
            Debug.LogWarning("[VillainSpawner] Nenhum lado de spawn habilitado em 'ladosSpawn'.");
            return Vector3.zero;
        }

        // Sempre retorna uma posição fora dos bounds no lado sorteado.
        // O WallPasser cuida de fazer o vilão atravessar qualquer obstáculo até entrar.
        LadoSpawn lado = ladosAtivos[Random.Range(0, ladosAtivos.Count)];
        switch (lado)
        {
            case LadoSpawn.Cima:
                return new Vector3(Random.Range(boundsMin.x, boundsMax.x), boundsMax.y + distanciaForaDoMapa, 0f);
            case LadoSpawn.Baixo:
                return new Vector3(Random.Range(boundsMin.x, boundsMax.x), boundsMin.y - distanciaForaDoMapa, 0f);
            case LadoSpawn.Esquerda:
                return new Vector3(boundsMin.x - distanciaForaDoMapa, Random.Range(boundsMin.y, boundsMax.y), 0f);
            case LadoSpawn.Direita:
                return new Vector3(boundsMax.x + distanciaForaDoMapa, Random.Range(boundsMin.y, boundsMax.y), 0f);
        }
        return Vector3.zero;
    }

    // ─── Controle de ondas ──────────────────────────────────────────────────

    void OnInimigoMorreu()
    {
        if (trocando) return;

        inimigosVivos--;
        if (inimigosVivos <= 0)
        {
            trocando = true;
            StartCoroutine(FinalizarTurno());
        }
    }

    IEnumerator FinalizarTurno()
    {
        yield return new WaitForSeconds(delayEntreOndas);
        ondaAtual++;
        IniciarTurno();
    }

    IEnumerator FinalizarFase()
    {
        yield return new WaitForSecondsRealtime(delayAntesDeTrocar);

        if (WaveBuffUI.Instance != null)
        {
            bool buffEscolhido = false;
            WaveBuffUI.Instance.MostrarEscolha(() => buffEscolhido = true);
            yield return new WaitUntil(() => buffEscolhido);
        }

        if (proximoMapa == null)
        {
            Debug.Log("[VillainSpawner] Vitória final — sem próximo mapa.");
            yield break;
        }

        if (PhaseTransition.Instance != null)
        {
            PhaseTransition.Instance.StartCoroutine(
                PhaseTransition.Instance.FadeOutInRoutine(ExecutarTrocaDeMapa));
        }
        else
        {
            ExecutarTrocaDeMapa();
        }
    }

    void ExecutarTrocaDeMapa()
    {
        foreach (var v in Object.FindObjectsByType<HeroHealth>(FindObjectsSortMode.None))
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<GolemHealth>(FindObjectsSortMode.None))
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<LichHealth>(FindObjectsSortMode.None))
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<VillainHealth>(FindObjectsSortMode.None))
            if (v != null) Destroy(v.gameObject);

        if (mapaAtual != null) mapaAtual.SetActive(false);
        proximoMapa.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        if (pontosDeSpawn == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        foreach (var t in pontosDeSpawn)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.4f);
            Gizmos.DrawLine(t.position + Vector3.up * 0.4f, t.position + Vector3.down * 0.4f);
            Gizmos.DrawLine(t.position + Vector3.left * 0.4f, t.position + Vector3.right * 0.4f);
        }
    }
}
