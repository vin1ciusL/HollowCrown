using UnityEngine;

/// <summary>
/// Gerenciador global de Almas (Singleton).
/// Fonte única de verdade para a quantidade atual de almas do jogador.
/// Persiste entre cenas com DontDestroyOnLoad.
/// </summary>
public class SoulManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────
    public static SoulManager Instance { get; private set; }

    // ─── Configuração ────────────────────────────────────────────
    [Header("Configuração de Almas")]
    [Tooltip("Quantidade de almas no início de cada fase")]
    public int almasIniciais = 50;

    [Tooltip("Almas regeneradas por segundo")]
    public float regeneracaoPorSegundo = 10f;

    // ─── Estado ──────────────────────────────────────────────────
    [Header("Estado (somente leitura no Inspector)")]
    [SerializeField] private int _almasAtuais;

    /// <summary>
    /// Quantidade atual de almas. Leitura pública, escrita privada.
    /// Disparar OnAlmasChanged ao ser alterado.
    /// </summary>
    public int AlmasAtuais
    {
        get => _almasAtuais;
        private set
        {
            if (_almasAtuais == value) return;
            _almasAtuais = value;
            OnAlmasChanged?.Invoke(_almasAtuais);
        }
    }

    /// <summary>
    /// Evento disparado sempre que a quantidade de almas muda.
    /// Útil para atualizar UI (TextMeshPro, etc.) no futuro.
    /// Parâmetro: novo valor de almas.
    /// </summary>
    public event System.Action<int> OnAlmasChanged;

    // ─── Acumulador interno para regeneração fracionária ─────────
    private float _acumuladorRegenFracionario;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    void Awake()
    {
        // Singleton: se já existe uma instância, destrói esta cópia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializa almas para a primeira fase
        _almasAtuais = almasIniciais;
    }

    void Update()
    {
        RegenerarAlmas();
    }

    // ─── Regeneração Passiva ─────────────────────────────────────

    /// <summary>
    /// Regenera almas continuamente a cada frame.
    /// Usa acumulador fracionário para precisão sub-segundo.
    /// </summary>
    private void RegenerarAlmas()
    {
        _acumuladorRegenFracionario += regeneracaoPorSegundo * Time.deltaTime;

        if (_acumuladorRegenFracionario >= 1f)
        {
            int ganho = Mathf.FloorToInt(_acumuladorRegenFracionario);
            _acumuladorRegenFracionario -= ganho;
            AlmasAtuais += ganho;
        }
    }


    /// <summary>
    /// Gasta uma quantidade arbitrária de almas.
    /// Retorna true se o gasto foi realizado.
    /// </summary>
    public bool TentarGastar(int quantidade)
    {
        if (quantidade <= 0) return false;
        if (AlmasAtuais < quantidade) return false;

        AlmasAtuais -= quantidade;
        return true;
    }

    /// <summary>
    /// Adiciona almas ao jogador (matar inimigos, itens, recompensas, etc.).
    /// Pode ser chamado por qualquer script.
    /// </summary>
    public void AdicionarAlmas(int quantidade)
    {
        if (quantidade <= 0) return;
        AlmasAtuais += quantidade;
        Debug.Log($"[SoulManager] +{quantidade} almas. Total: {AlmasAtuais}");
    }

    /// <summary>
    /// Reseta as almas para o valor inicial da fase.
    /// Deve ser chamado ao carregar uma nova fase/cena.
    /// </summary>
    public void ResetarParaFase()
    {
        _acumuladorRegenFracionario = 0f;
        AlmasAtuais = almasIniciais;
        Debug.Log($"[SoulManager] Almas resetadas para {almasIniciais} (início de fase)");
    }
}
