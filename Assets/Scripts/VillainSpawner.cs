using UnityEngine;
using System.Collections;

public class VillainSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject villainPrefab;
    public float spawnOffsetY = 1.5f;
    public float delayEntreSpawns = 0.3f;
    public float delayAntesDeTrocar = 2f;

    [Header("Ondas")]
    public int[] inimigosPorturno = { 2, 3, 4 };
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

        // Reseta almas para o valor inicial da fase
        if (SoulManager.Instance != null)
            SoulManager.Instance.ResetarParaFase();

        IniciarTurno();
    }

    void IniciarTurno()
    {
        trocando = false;

        if (ondaAtual >= inimigosPorturno.Length)
        {
            Debug.Log("Vitória! Todas as ondas concluídas.");
            StartCoroutine(FinalizarJogo());
            return;
        }

        int quantidade = inimigosPorturno[ondaAtual];
        Debug.Log($"Onda {ondaAtual + 1} — {quantidade} inimigos");
        inimigosVivos = quantidade;
        StartCoroutine(SpawnarTurno(quantidade));
    }

    IEnumerator SpawnarTurno(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            SpawnVillain();
            yield return new WaitForSeconds(delayEntreSpawns);
        }
    }

    void SpawnVillain()
    {
        if (villainPrefab == null) return;

        Vector3 spawnPos = EncontrarPosicaoSegura();
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[VillainSpawner] Não foi possível encontrar posição segura para spawn!");
            // Fallback: spawna no centro da câmera
            spawnPos = cam.transform.position;
            spawnPos.z = 0f;
        }

        GameObject v = Instantiate(villainPrefab, spawnPos, Quaternion.identity);

        VillainHealth vh = v.GetComponent<VillainHealth>();
        if (vh != null)
            vh.OnMorte += OnInimigoMorreu;
    }

    /// <summary>
    /// Tenta encontrar uma posição de spawn que esteja:
    /// 1) Dentro dos limites do viewport da câmera (com margem)
    /// 2) Dentro do Collider do mapa (se atribuído)
    /// 3) Livre de obstáculos físicos
    /// </summary>
    Vector3 EncontrarPosicaoSegura()
    {
        for (int tentativa = 0; tentativa < maxSpawnAttempts; tentativa++)
        {
            // Gera ponto aleatório dentro do viewport com margem
            float vx = Random.Range(viewportMargin, 1f - viewportMargin);
            float vy = Random.Range(viewportMargin, 1f - viewportMargin);
            Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vx, vy, 0));
            worldPos.z = 0f;

            // Verifica se está dentro dos limites do mapa
            if (mapBounds != null && !mapBounds.OverlapPoint(worldPos))
                continue;

            // Verifica se a posição está livre de obstáculos sólidos
            Collider2D obstruction = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
            if (obstruction != null && !obstruction.isTrigger)
                continue;

            return worldPos;
        }

        return Vector3.zero; // Nenhuma posição válida encontrada
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

    IEnumerator FinalizarTurno()
    {
        yield return new WaitForSeconds(delayEntreOndas);
        ondaAtual++;

        // Se ainda há ondas restantes, mostra a tela de buff
        if (ondaAtual < inimigosPorturno.Length)
        {
            if (WaveBuffUI.Instance != null)
            {
                // Mostra UI de buff e espera o jogador escolher
                WaveBuffUI.Instance.MostrarEscolha(() =>
                {
                    // Callback: após o jogador escolher, inicia o próximo turno
                    IniciarTurno();
                });
            }
            else
            {
                // Sem sistema de buff configurado — prossegue normalmente
                IniciarTurno();
            }
        }
        else
        {
            IniciarTurno(); // vai chamar FinalizarJogo()
        }
    }

    IEnumerator FinalizarJogo()
    {
        foreach (GameObject hero in GameObject.FindGameObjectsWithTag("Player"))
            hero.SetActive(false);

        yield return new WaitForSeconds(delayAntesDeTrocar);

        if (mapaAtual != null) mapaAtual.SetActive(false);
        if (proximoMapa != null)
        {
            proximoMapa.SetActive(true);
            Camera.main.transform.position = new Vector3(
                posicaoCameraProximoMapa.x,
                posicaoCameraProximoMapa.y,
                -10f
            );
        }
    }
}