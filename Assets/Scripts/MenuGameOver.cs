using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuGameOver : MonoBehaviour
{
    public void TentarNovamente()
    {
        // Mude de "Mapa_Externo" para "Game"
        // Este é o nome que aparece na sua Build Settings (índice 1)
        SceneManager.LoadScene("Game");
    }

    public void IrParaMenu()
    {
        SceneManager.LoadScene("menuinicial");
    }
}