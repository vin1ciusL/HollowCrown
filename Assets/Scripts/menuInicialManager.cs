using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicialManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void OpenHowToPlay()
    {
        SceneManager.LoadScene("ComoJogar");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("menuinicial");
    }
}