using UnityEngine;

public class PlayerLives : MonoBehaviour
{
    public static PlayerLives Instance { get; private set; }

    [Tooltip("Container com as células (hpgrid). Se vazio, busca por nome 'GridDeVida'.")]
    public Transform gridDeVida;

    private int vidasRestantes;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gridDeVida == null)
        {
            GameObject go = GameObject.Find("GridDeVida");
            if (go != null) gridDeVida = go.transform;
        }

        vidasRestantes = gridDeVida != null ? gridDeVida.childCount : 0;
    }

    public void PerderVida()
    {
        if (gridDeVida == null) return;
        if (vidasRestantes <= 0) return;

        vidasRestantes--;
        Transform cell = gridDeVida.GetChild(vidasRestantes);
        if (cell != null) cell.gameObject.SetActive(false);

        if (vidasRestantes <= 0)
            Debug.Log("[PlayerLives] Game Over — sem vidas restantes.");
    }
}
