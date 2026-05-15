using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Singleton de transição entre fases — escurece a tela, executa a troca de mapa,
/// e depois esclarece.
///
/// Uso:
///   StartCoroutine(PhaseTransition.Instance.FadeOutInRoutine(() => {
///       // ações executadas enquanto a tela está preta (troca de mapa, etc.)
///   }));
///
/// Se nenhum Image for atribuído no Inspector, o componente cria automaticamente
/// um Canvas fullscreen preto na sortingOrder mais alta — basta colocar o script
/// num GameObject vazio na cena inicial.
/// </summary>
public class PhaseTransition : MonoBehaviour
{
    public static PhaseTransition Instance { get; private set; }

    [Tooltip("Image fullscreen usada para o fade. Se vazia, é criada automaticamente.")]
    public Image fadeImage;

    [Header("Tempos")]
    [Tooltip("Duração do fade out (tela escurece)")]
    public float fadeOutDuration = 0.6f;

    [Tooltip("Tempo que a tela permanece preta (durante a troca de mapa)")]
    public float blackHoldDuration = 0.35f;

    [Tooltip("Duração do fade in (tela esclarece)")]
    public float fadeInDuration = 0.6f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage == null) CriarOverlayAutomatico();
        SetAlpha(0f);
    }

    void CriarOverlayAutomatico()
    {
        GameObject canvasGO = new GameObject("PhaseTransitionCanvas");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // acima de tudo

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false; // não bloqueia cliques quando invisível

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
        // Quando totalmente transparente, deixa o overlay passar cliques.
        fadeImage.raycastTarget = a > 0.01f;
    }

    /// <summary>
    /// Coroutine completa: escurece → executa <paramref name="duranteEscuro"/> → esclarece.
    /// Usa Time.unscaledDeltaTime, então funciona mesmo com Time.timeScale = 0.
    /// </summary>
    public IEnumerator FadeOutInRoutine(System.Action duranteEscuro)
    {
        yield return Fade(0f, 1f, fadeOutDuration);

        duranteEscuro?.Invoke();

        if (blackHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackHoldDuration);

        yield return Fade(1f, 0f, fadeInDuration);
    }

    IEnumerator Fade(float from, float to, float dur)
    {
        if (fadeImage == null || dur <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(to);
    }
}
