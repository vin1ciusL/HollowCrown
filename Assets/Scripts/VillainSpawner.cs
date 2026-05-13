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

    private Camera cam;
    private int ondaAtual = 0;
    private int inimigosVivos = 0;
    private bool trocando = false;

    void Start()
    {
        cam = Camera.main;
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

        // Spawna nas bordas da câmera, dentro da área visível
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, 0));

        Vector3 spawnPos;
        int borda = Random.Range(0, 4);
        switch (borda)
        {
            case 0: spawnPos = new Vector3(Random.Range(min.x, max.x), min.y + spawnOffsetY, 0); break; // baixo
            case 1: spawnPos = new Vector3(Random.Range(min.x, max.x), max.y - spawnOffsetY, 0); break; // cima
            case 2: spawnPos = new Vector3(min.x + spawnOffsetY, Random.Range(min.y, max.y), 0); break; // esquerda
            default: spawnPos = new Vector3(max.x - spawnOffsetY, Random.Range(min.y, max.y), 0); break; // direita
        }

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
        if (hero != null)
            hero.SetActive(false);

        yield return new WaitForSeconds(2f);

        if (mapaAtual != null)  mapaAtual.SetActive(false);
        if (proximoMapa != null) proximoMapa.SetActive(true);
    }
}
