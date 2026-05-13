using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject painelPause;

    public void PauseGame()
    {
        Time.timeScale = 0f;
        painelPause.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        painelPause.SetActive(false);
    }
}