using UnityEngine;

/// <summary>
/// Toca uma música em loop durante a cena em que vive.
/// NÃO usa DontDestroyOnLoad — quando a cena descarrega (game over, voltar
/// ao menu), o GameObject morre e a música para naturalmente.
///
/// SETUP:
///   1) Coloque este componente num GameObject root da cena Game
///   2) Arraste o AudioClip no campo "Clip"
///   3) Pronto — toca ao Awake, em loop
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Tooltip("Música a tocar em loop. Se vazio, carrega de Resources usando 'resourceName'.")]
    public AudioClip clip;

    [Tooltip("Nome do arquivo dentro de Assets/Resources (sem extensão). Usado como fallback se 'clip' for vazio.")]
    public string resourceName = "hollowcrown-v2_Master";

    [Range(0f, 1f)]
    [Tooltip("Volume (0-1).")]
    public float volume = 0.6f;

    [Tooltip("Toca automaticamente ao Awake.")]
    public bool playOnAwake = true;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();

        // Carrega o clip de Resources se não foi atribuído no Inspector
        if (clip == null && !string.IsNullOrEmpty(resourceName))
        {
            clip = Resources.Load<AudioClip>(resourceName);
            if (clip == null)
                Debug.LogWarning($"[BackgroundMusic] Não encontrei '{resourceName}' em Assets/Resources/.");
        }

        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.playOnAwake = playOnAwake;

        if (clip == null) return;
        if (playOnAwake) source.Play();
    }
}
