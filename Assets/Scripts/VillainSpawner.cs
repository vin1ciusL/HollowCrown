using UnityEngine;
using System.Collections;

public class VillainSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject villainPrefab;
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
    public float delayEntreOndas = 3f;

    [Header("Próxima Fase")]
    public GameObject mapaAtual;
    public GameObject proximoMapa;
    public Vector2 posicaoCameraProximoMapa;

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

        Vector2 min, max;

        // Se limites do mapa foram definidos no Inspector, usa eles
        if (mapMin != mapMax)
        {
            min = mapMin + Vector2.one * spawnMargin;
            max = mapMax - Vector2.one * spawnMargin;
        }
        else
        {
            // Fallback: usa viewport da câmera atual
            Vector3 vmin = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, 0));
            Vector3 vmax = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, 0));
            min = vmin;
            max = vmax;
        }

        float x = Random.Range(min.x, max.x);
        float y = Mathf.Clamp(min.y + spawnOffsetY, min.y, max.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject v = Instantiate(villainPrefab, spawnPos, Quaternion.identity);

        VillainHealth vh = v.GetComponent<VillainHealth>();
        if (vh != null)
            vh.OnMorte += OnInimigoMorreu;
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
        IniciarTurno();
    }

    IEnumerator FinalizarJogo()
    {
        yield return new WaitForSeconds(delayAntesDeTrocar);

        if (proximoMapa == null)
        {
            Debug.Log("[VillainSpawner] Vitória final — sem próximo mapa.");
            yield break;
        }

        // Destrói invocados e vilões remanescentes do mapa anterior
        foreach (var v in Object.FindObjectsByType<HeroHealth>())
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<GolemHealth>())
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<MageHealth>())
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<LichHealth>())
            if (v != null) Destroy(v.gameObject);
        foreach (var v in Object.FindObjectsByType<VillainHealth>())
            if (v != null) Destroy(v.gameObject);

        if (mapaAtual != null) mapaAtual.SetActive(false);
        proximoMapa.SetActive(true);
    }
}