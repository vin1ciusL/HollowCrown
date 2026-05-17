using UnityEngine;
using UnityEngine.InputSystem;

// Em Play Mode, clique na Game view (botão esquerdo) para logar a posição world
// (X, Y) onde clicou. Útil para descobrir coordenadas de portas, spawn points etc.
public class MousePosicao : MonoBehaviour
{
    private int contador;

    void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (Camera.main == null)
        {
            Debug.LogWarning("[MousePosicao] Camera.main não encontrada (verifique tag MainCamera).");
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        pos.z = 0f;

        contador++;
        Debug.Log($"[MousePosicao] Click #{contador}:  x = {pos.x:F3}   y = {pos.y:F3}");
    }
}
