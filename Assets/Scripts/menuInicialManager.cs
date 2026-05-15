using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicialManager : MonoBehaviour
{
    public void StartGame()
    {
        ResetEstadoPersistente();
        SceneManager.LoadScene("Game");
    }

    public void OpenHowToPlay()
    {
        SceneManager.LoadScene("ComoJogar");
    }

    public void BackToMenu()
    {
        ResetEstadoPersistente();
        SceneManager.LoadScene("menuinicial");
    }

    void ResetEstadoPersistente()
    {
        SoulManager.Instance?.ResetCompleto();
        WaveBuffUI.Instance?.ResetCompleto();
    }
}
