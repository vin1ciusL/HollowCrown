using UnityEngine;
using System.Collections;

public class VillainSpawner : MonoBehaviour
{
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
    public int[] inimigosPorturno = { 2, 3, 4 };
    public int[] magosPorturno = { 0, 1, 2 };
    public float delayEntreOndas = 3f;

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
    private int ondaAtual = 0;
    private int inimigosVivos = 0;
    private bool trocando = false;

    void OnEnable()
    {
        cam = Camera.main;
        ondaAtual = 0;
        trocando = false;

        // Reseta almas para o valor inicial da fase (já considera bônus de buffs)
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
        int qtdMago = (magosPorturno != null && ondaAtual < magosPorturno.Length) ? magosPorturno[ondaAtual] : 0;
        if (magoPrefab == null) qtdMago = 0;

        Debug.Log($"[VillainSpawner] Onda {ondaAtual + 1}/{inimigosPorturno.Length} — {qtdVilao} vilões + {qtdMago} magos");
        inimigosVivos = qtdVilao + qtdMago;
        StartCoroutine(SpawnarTurno(qtdVilao, qtdMago));
    }

    IEnumerator SpawnarTurno(int qtdVilao, int qtdMago)
    {
        for (int i = 0; i < qtdVilao; i++)
        {
            SpawnInimigo(villainPrefab);
            yield return new WaitForSeconds(delayEntreSpawns);
        }
        for (int i = 0; i < qtdMago; i++)
        {
            SpawnInimigo(magoPrefab);
            yield return new WaitForSeconds(delayEntreSpawns);
        }
    }

    void SpawnInimigo(GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 spawnPos = EncontrarPosicaoSegura();
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[VillainSpawner] Não foi possível encontrar posição segura para spawn!");
            spawnPos = cam.transform.position;
            spawnPos.z = 0f;
        }

        GameObject v = Instantiate(prefab, spawnPos, Quaternion.identity);

        VillainHealth vh = v.GetComponent<VillainHealth>();
        if (vh != null)
            vh.OnMorte += OnInimigoMorreu;
    }

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
                worldPos = cam.ViewportToWorldPoint(new Vector3(vx, vy, 0));
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

    /// <summary>
    /// Avança para a próxima onda. NÃO mostra mais buff entre ondas —
    /// o buff é dado apenas ao final da fase (em FinalizarFase).
    /// </summary>
    IEnumerator FinalizarTurno()
    {
        yield return new WaitForSeconds(delayEntreOndas);
        ondaAtual++;
        IniciarTurno();
    }

    /// <summary>
    /// Chamado quando todas as ondas da fase terminam.
    /// Fluxo: pequeno delay → escolha de buff → fade out → troca de mapa → fade in.
    /// </summary>
    IEnumerator FinalizarFase()
    {
        yield return new WaitForSecondsRealtime(delayAntesDeTrocar);

        // 1) Buff de fim de fase (espera o jogador escolher)
        if (WaveBuffUI.Instance != null)
        {
            bool buffEscolhido = false;
            WaveBuffUI.Instance.MostrarEscolha(() => buffEscolhido = true);
            yield return new WaitUntil(() => buffEscolhido);
        }

        // 2) Se não há próximo mapa, vitória final — sem transição
        if (proximoMapa == null)
        {
            Debug.Log("[VillainSpawner] Vitória final — sem próximo mapa.");
            yield break;
        }

        // 3) Transição com fade. IMPORTANTE: a coroutine roda no PhaseTransition (singleton
        //    persistente) porque ExecutarTrocaDeMapa desativa mapaAtual — se a coroutine
        //    rodasse neste VillainSpawner (filho de mapaAtual), morreria antes do fade in.
        if (PhaseTransition.Instance != null)
        {
            PhaseTransition.Instance.StartCoroutine(
                PhaseTransition.Instance.FadeOutInRoutine(ExecutarTrocaDeMapa));
        }
        else
        {
            // Fallback sem fade
            ExecutarTrocaDeMapa();
        }
    }

    void ExecutarTrocaDeMapa()
    {
        // Destrói invocados e vilões remanescentes do mapa anterior
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
