using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuGameOver : MonoBehaviour
{
    public void TentarNovamente()
    {
        ResetEstadoPersistente();
        SceneManager.LoadScene("Game");
    }

    public void IrParaMenu()
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
