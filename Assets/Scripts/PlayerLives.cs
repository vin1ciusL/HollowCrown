using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerLives : MonoBehaviour
{
    public static PlayerLives Instance { get; private set; }

    [Tooltip("Container com as células (hpgrid). Se vazio, busca por nome 'GridDeVida'.")]
    public Transform gridDeVida;

    [Tooltip("Nome da cena de Game Over (deve estar habilitada no Build Settings)")]
    public string cenaGameOver = "GameOver";

    private int vidasRestantes;
    private bool gameOverDisparado = false;

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

        if (vidasRestantes <= 0 && !gameOverDisparado)
        {
            gameOverDisparado = true;
            StartCoroutine(DispararGameOver());
        }
    }

    IEnumerator DispararGameOver()
    {
        Debug.Log("[PlayerLives] Game Over — carregando cena GameOver.");

        // Restaura timescale caso esteja pausado por algum painel
        Time.timeScale = 1f;

        if (PhaseTransition.Instance != null)
        {
            // Usa o fade do PhaseTransition (que é DontDestroyOnLoad e sobrevive ao LoadScene).
            // Roda a coroutine no próprio PhaseTransition pra não morrer se este GO for destruído.
            PhaseTransition.Instance.StartCoroutine(
                PhaseTransition.Instance.FadeOutInRoutine(() => SceneManager.LoadScene(cenaGameOver)));
            yield break;
        }

        // Fallback sem fade
        SceneManager.LoadScene(cenaGameOver);
    }
}
