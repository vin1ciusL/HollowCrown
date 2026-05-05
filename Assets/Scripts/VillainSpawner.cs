using UnityEngine;

public class VillainSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject villainPrefab;
    public float spawnInterval = 5f;

    [Header("Offset da tela")]
    [Tooltip("Quão longe abaixo da tela o vilão spawna")]
    public float spawnOffsetY = 1.5f;

    private Camera cam;
    private float timer;

    void Start()
    {
        cam = Camera.main;
        timer = spawnInterval; // já spawna no primeiro ciclo
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnVillain();
        }
    }

    void SpawnVillain()
    {
        if (villainPrefab == null)
        {
            Debug.LogWarning("VillainSpawner: villainPrefab não atribuído!");
            return;
        }

        // Pega os limites da tela em world space
        Vector3 bottomLeft  = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0, 0));

        // X aleatório ao longo da borda inferior
        float randomX = Random.Range(bottomLeft.x, bottomRight.x);

        // Y abaixo da tela
        float spawnY = bottomLeft.y - spawnOffsetY;

        Vector3 spawnPos = new Vector3(randomX, spawnY, 0f);

        Instantiate(villainPrefab, spawnPos, Quaternion.identity);
    }
}
