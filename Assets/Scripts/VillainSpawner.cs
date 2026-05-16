using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TipoMapa { Externo, Dungeon, Royal }

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
    [Tooltip("Margem interna do viewport para não spawnar nas bordas")]
    public float viewportMargin = 0.10f;
    [Tooltip("Raio para checar se o ponto de spawn está livre de obstáculos")]
    public float spawnCheckRadius = 0.5f;
    [Tooltip("Máximo de tentativas para encontrar posição válida")]
    public int maxSpawnAttempts = 15;
    [Tooltip("Collider2D do mapa — se atribuído, spawn só dentro dele")]
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

    Vector3 EncontrarPosicaoSegura()
    {
        bool useMapBounds = mapMin != mapMax;
        Vector2 boundsMin = useMapBounds ? mapMin + Vector2.one * spawnMargin : Vector2.zero;
        Vector2 boundsMax = useMapBounds ? mapMax - Vector2.one * spawnMargin : Vector2.zero;

        for (int tentativa = 0; tentativa < maxSpawnAttempts; tentativa++)
        {
            Vector3 worldPos;
            if (useMapBounds)
            {
                worldPos = new Vector3(
                    Random.Range(boundsMin.x, boundsMax.x),
                    Random.Range(boundsMin.y, boundsMax.y),
                    0f);
            }
            else
            {
                float vx = Random.Range(viewportMargin, 1f - viewportMargin);
                float vy = Random.Range(viewportMargin, 1f - viewportMargin);
                worldPos   = cam.ViewportToWorldPoint(new Vector3(vx, vy, 0));
                worldPos.z = 0f;
            }

            if (mapBounds != null && !mapBounds.OverlapPoint(worldPos))
                continue;

            Collider2D obstruction = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
            if (obstruction != null && !obstruction.isTrigger)
                continue;

            return worldPos;
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
}
