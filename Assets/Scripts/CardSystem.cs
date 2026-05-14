using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CardSystem : MonoBehaviour
{
    [Header("Carta 1 - Esqueleto")]
    public GameObject heroPrefab;
    public Image cardImageHero;

    [Header("Carta 2 - Golem")]
    public GameObject golemPrefab;
    public Image cardImageGolem;

    [Header("Cores")]
    public Color colorDefault = Color.white;
    public Color colorSelected = Color.red;

    [Header("Spawn")]
    public float spawnCheckRadius = 0.5f;
    public Camera mainCamera;

    private int cardSelecionada = 0;
    private GameObject skeletonInstance = null;
    private GameObject golemInstance = null;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (cardSelecionada != 0)
                TrySpawnUnit();
        }
    }

    public void OnCardHeroClicked()
    {
        if (skeletonInstance != null && skeletonInstance.activeInHierarchy)
            return;

        skeletonInstance = null;
        cardSelecionada = cardSelecionada == 1 ? 0 : 1;
        AtualizarCores();
    }

    public void OnCardGolemClicked()
    {
        if (golemInstance != null && golemInstance.activeInHierarchy)
            return;

        golemInstance = null;
        cardSelecionada = cardSelecionada == 2 ? 0 : 2;
        AtualizarCores();
    }

    void AtualizarCores()
    {
        if (cardImageHero != null)
            cardImageHero.color = cardSelecionada == 1 ? colorSelected : colorDefault;

        if (cardImageGolem != null)
            cardImageGolem.color = cardSelecionada == 2 ? colorSelected : colorDefault;
    }

    void TrySpawnUnit()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
        if (hit != null)
        {
            Debug.Log("Local bloqueado por: " + hit.gameObject.name);
            return;
        }

        if (cardSelecionada == 1)
        {
            if (skeletonInstance != null)
                Destroy(skeletonInstance);
            skeletonInstance = Instantiate(heroPrefab, worldPos, Quaternion.identity);
        }
        else if (cardSelecionada == 2)
        {
            if (golemInstance != null)
                Destroy(golemInstance);
            golemInstance = Instantiate(golemPrefab, worldPos, Quaternion.identity);
        }

        cardSelecionada = 0;
        AtualizarCores();
    }
}