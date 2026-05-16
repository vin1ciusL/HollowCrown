using UnityEngine;
using TMPro;

/// <summary>
/// Liga um TextMeshProUGUI à quantidade atual de almas do SoulManager.
/// Se inscreve no evento OnAlmasChanged e atualiza o texto automaticamente.
///
/// SETUP NO UNITY:
///   1) Crie um GameObject UI → Text - TextMeshPro (no Canvas do HUD)
///   2) Adicione este componente ao mesmo GameObject
///   3) (Opcional) Arraste o TMP no campo "Texto Almas" se ele não estiver no mesmo objeto
///   4) Ajuste o "Formato" se quiser outro texto (ex: "{0}", "Almas: {0}", "💀 {0}")
/// </summary>
public class SoulCounterUI : MonoBehaviour
{
    [Tooltip("TextMeshProUGUI que mostrará a quantidade de almas. Se vazio, busca neste GameObject.")]
    public TextMeshProUGUI textoAlmas;

    [Tooltip("Formato do texto. Use {0} no lugar do número.")]
    public string formato = "{0}";

    private bool subscrito = false;

    void Awake()
    {
        if (textoAlmas == null) textoAlmas = GetComponent<TextMeshProUGUI>();
        if (textoAlmas == null)
            Debug.LogWarning("[SoulCounterUI] Nenhum TextMeshProUGUI atribuído ou encontrado neste GameObject.");
    }

    void OnEnable()
    {
        TentarInscrever();
    }

    void OnDisable()
    {
        if (subscrito && SoulManager.Instance != null)
        {
            SoulManager.Instance.OnAlmasChanged -= AtualizarTexto;
            subscrito = false;
        }
    }

    void Update()
    {
        // Fallback: caso o SoulManager seja criado depois deste UI
        if (!subscrito) TentarInscrever();
    }

    void TentarInscrever()
    {
        if (subscrito || SoulManager.Instance == null) return;

        AtualizarTexto(SoulManager.Instance.AlmasAtuais);
        SoulManager.Instance.OnAlmasChanged += AtualizarTexto;
        subscrito = true;
    }

    void AtualizarTexto(int novaQtd)
    {
        if (textoAlmas != null)
            textoAlmas.text = string.Format(formato, novaQtd);
    }
}
