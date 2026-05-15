using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MageCardSystem : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject magePrefab;
    public Image cardImage;
    public Camera mainCamera;

    [Header("Cores")]
    public Color colorDefault = Color.white;
    public Color colorSelected = Color.red;

    [Header("Spawn")]
    public float spawnCheckRadius = 0.5f;

    [Header("Custo")]
    [Tooltip("Custo em almas para invocar o Mago")]
    public int custoInvocacao = 100;

    private bool cardSelected = false;
    private GameObject mageInstance = null;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (cardSelected)
                TrySpawnMage();
        }
    }

    public void OnCardClicked()
    {
        if (mageInstance != null && mageInstance.activeInHierarchy)
            return;

        mageInstance = null;
        cardSelected = !cardSelected;
        if (cardImage != null)
            cardImage.color = cardSelected ? colorSelected : colorDefault;
    }

    void TrySpawnMage()
    {
        if (magePrefab == null)
        {
            Debug.LogError("[MageCardSystem] magePrefab nao atribuido!");
            return;
        }

        if (SoulManager.Instance == null) return;

        if (SoulManager.Instance.AlmasAtuais < custoInvocacao)
        {
            Debug.Log($"[MageCardSystem] Almas insuficientes! Necessário: {custoInvocacao}, atual: {SoulManager.Instance.AlmasAtuais}");
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        float camZ = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));
        worldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
        if (hit != null)
        {
            Debug.Log("[MageCardSystem] Local bloqueado por: " + hit.gameObject.name);
            return;
        }

        if (!SoulManager.Instance.TentarGastar(custoInvocacao))
            return;

        if (mageInstance != null)
            Destroy(mageInstance);

        mageInstance = Instantiate(magePrefab, worldPos, Quaternion.identity);
        Debug.Log("[MageCardSystem] Mago invocado em " + worldPos);

        cardSelected = false;
        if (cardImage != null)
            cardImage.color = colorDefault;
    }
}
