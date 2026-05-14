using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CardSystem : MonoBehaviour
{
    [Header("Referências")]
    public GameObject heroPrefab;
    public Image cardImage;
    public Camera mainCamera;

    [Header("Cores")]
    public Color colorDefault = Color.white;
    public Color colorSelected = Color.red;

    [Header("Spawn")]
    [Tooltip("Raio para checar se o local está livre de colisores")]
    public float spawnCheckRadius = 0.5f;

    private bool cardSelected = false;
    private GameObject heroInstance = null;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (cardSelected)
                TrySpawnHero();
        }
    }

    public void OnCardClicked()
    {
        if (heroInstance != null && heroInstance.activeInHierarchy)
            return;

        cardSelected = !cardSelected;
        cardImage.color = cardSelected ? colorSelected : colorDefault;
    }

    void TrySpawnHero()
    {
        // Verifica se há almas suficientes antes de qualquer coisa
        if (SoulManager.Instance == null || !SoulManager.Instance.PodeInvocar())
        {
            Debug.Log("[CardSystem] Almas insuficientes para invocar!");
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        float camZ = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));
        worldPos.z = 0f;

        // Checa se tem algum colisor no local
        Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
        if (hit != null)
        {
            Debug.Log("Local bloqueado por: " + hit.gameObject.name);
            return; // Não spawna
        }

        // Gasta as almas (double-check interno)
        if (!SoulManager.Instance.TentarGastarParaInvocacao())
            return;

        if (heroInstance != null)
            Destroy(heroInstance);

        heroInstance = Instantiate(heroPrefab, worldPos, Quaternion.identity);

        cardSelected = false;
        cardImage.color = colorDefault;
    }
}