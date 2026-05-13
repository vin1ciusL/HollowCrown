using UnityEngine;
using System.Collections;

public class VillainSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject villainPrefab;
    public float spawnOffsetY = 1.5f;
    public float delayEntreSpawns = 0.3f;
    public float delayAntesDeTrocar = 2f;

    [Header("Turnos")]
    public int[] inimigosPorturno = { 1, 1, 1 };
    [Tooltip("Quantos turnos por mapa antes de trocar")]
    public int turnosPorMapa = 3;

    [Header("Mapas")]
    public GameObject[] mapas;

    private Camera cam;
    private int turnoAtual = 0;   // turno global (0, 1, 2, 3, ...)
    private int mapaAtual  = 0;   // índice do mapa ativo
    private int inimigosVivos = 0;
    private bool trocando = false;

    void Start()
    {
        cam = Camera.main;

        for (int i = 0; i < mapas.Length; i++)
            mapas[i].SetActive(i == 0);

        IniciarTurno();
    }

    void IniciarTurno()
    {
        trocando = false;

        int quantidade;
        if (turnoAtual < inimigosPorturno.Length)
            quantidade = inimigosPorturno[turnoAtual];
        else
            quantidade = inimigosPorturno[inimigosPorturno.Length - 1] + (turnoAtual - inimigosPorturno.Length + 1);

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

        Vector3 bottomLeft  = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0, 0));

        float randomX = Random.Range(bottomLeft.x, bottomRight.x);
        float spawnY  = bottomLeft.y - spawnOffsetY;

        GameObject v = Instantiate(villainPrefab, new Vector3(randomX, spawnY, 0f), Quaternion.identity);

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
        yield return new WaitForSeconds(delayAntesDeTrocar);

        turnoAtual++;

        int t = Mathf.Max(1, turnosPorMapa);
        bool deveTracar = turnoAtual % t == 0;

        if (deveTracar)
        {
            GameObject hero = GameObject.FindWithTag("Player");
            if (hero != null) Destroy(hero);

            int novoMapa = turnoAtual / t;

            if (mapaAtual < mapas.Length)
                mapas[mapaAtual].SetActive(false);

            if (novoMapa < mapas.Length)
            {
                mapas[novoMapa].SetActive(true);
                mapaAtual = novoMapa;
            }
            else
            {
                Debug.Log("Fim de jogo!");
                yield break;
            }
        }

        IniciarTurno();
    }
}
