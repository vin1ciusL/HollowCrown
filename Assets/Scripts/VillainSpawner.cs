using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    private Camera cam;
    private int ondaAtual = 0;
    private int inimigosVivos = 0;
    private bool trocando = false;

    void OnEnable()
    {
        cam = Camera.main;
        ondaAtual = 0;
        inimigosVivos = 0;
        trocando = false;
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

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, 0));

        Vector3 spawnPos = new Vector3(Random.Range(min.x, max.x), min.y + spawnOffsetY, 0);

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
        GameObject hero = GameObject.FindWithTag("Player");
        hero?.SetActive(false);

        // Zera o target da câmera para ela não lutar contra o teleporte
        CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
        if (cf != null) cf.target = null;

        yield return new WaitForSeconds(delayAntesDeTrocar);

        if (proximoMapa != null)
        {
            // Desativa o light do mapa atual antes de ativar o próximo para evitar conflito de Global Light
            Light2D luz = mapaAtual?.GetComponentInChildren<Light2D>();
            if (luz != null) luz.enabled = false;

            proximoMapa.SetActive(true);
        }

        // Desativa o mapa atual POR ÚLTIMO para não matar esta coroutine antes de terminar
        mapaAtual?.SetActive(false);
    }
}