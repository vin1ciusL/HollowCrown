using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LichCardSystem : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject lichPrefab;
    public Image cardImage;
    public Camera mainCamera;

    [Header("Cores")]
    public Color colorDefault = Color.white;
    public Color colorSelected = Color.red;

    [Header("Spawn")]
    public float spawnCheckRadius = 0.5f;

    private bool cardSelected = false;
    private GameObject lichInstance = null;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (cardSelected)
                TrySpawnLich();
        }
    }

    public void OnCardClicked()
    {
        cardSelected = !cardSelected;
        cardImage.color = cardSelected ? colorSelected : colorDefault;
    }

    void TrySpawnLich()
    {
        if (lichPrefab == null)
        {
            Debug.LogError("[LichCardSystem] lichPrefab nao atribuido!");
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0f;

        if (lichInstance != null)
            Destroy(lichInstance);

        lichInstance = Instantiate(lichPrefab, worldPos, Quaternion.identity);
        Debug.Log("[LichCardSystem] Lich invocado em " + worldPos);

        cardSelected = false;
        cardImage.color = colorDefault;
    }
}
