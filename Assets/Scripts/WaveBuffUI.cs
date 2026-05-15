using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Sistema de escolha de buff aplicado ao FIM de cada fase (após todas as ondas).
/// Os buffs são persistentes durante todo o jogo e se aplicam a TODAS as cartas
/// invocadas (existentes e futuras). Por isso são mais fortes — você só ganha 1 por fase.
///
/// Fluxo esperado:
///   VillainSpawner.FinalizarFase() → MostrarEscolha(callback) → callback continua transição
///   CardSystem.TentarInvocar() → AplicarBuffsA(novoAliado) para herdar os bônus
/// </summary>
public class WaveBuffUI : MonoBehaviour
{
    public static WaveBuffUI Instance { get; private set; }

    [Header("Painel de Buffs")]
    [Tooltip("Painel raiz que contém as opções de buff. Será ativado/desativado.")]
    public GameObject buffPanel;

    [Header("Botões de Buff (3 opções)")]
    public Button[] buffButtons = new Button[3];

    [Header("Textos dos Botões")]
    public TextMeshProUGUI[] buffTexts = new TextMeshProUGUI[3];

    [Header("Ícones (opcional)")]
    public Image[] buffIcons = new Image[3];

    [Header("Persistência")]
    [Tooltip("Se true, o objeto não é destruído ao trocar de cena. Bons buffs precisam disso.")]
    public bool persistirEntreCenas = true;

    // ─── Estado interno ──────────────────────────────────────────
    private System.Action onBuffChosen;
    private BuffOption[] currentOptions;

    // ─── Buffs acumulados (aplicados a TODAS as cartas) ─────────
    // Aditivos antes dos multiplicadores: final = (base + bonus) * mult
    [HideInInspector] public float bonusDamage = 0f;
    [HideInInspector] public float bonusHealth = 0f;
    [HideInInspector] public float bonusSpeed = 0f;
    [HideInInspector] public float multDamage = 1f;
    [HideInInspector] public float multHealth = 1f;
    [HideInInspector] public float multSpeed = 1f;
    [HideInInspector] public float bonusAttackSpeed = 0f; // reduz cooldown / aumenta fireRate

    // ─── Definição dos buffs possíveis ───────────────────────────

    public struct BuffOption
    {
        public string nome;
        public string descricao;
        public System.Action aplicar;
    }

    private List<BuffOption> todosOsBuffs;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (persistirEntreCenas) DontDestroyOnLoad(gameObject);

        AutoEncontrarReferencias();

        if (buffPanel != null) buffPanel.SetActive(false);

        InicializarBuffs();
    }

    /// <summary>
    /// Se buffPanel/buffButtons/buffTexts não foram atribuídos no Inspector,
    /// tenta encontrá-los por nome na cena. Procura por:
    ///   - GameObject chamado "buffPanel" (inclusive inativos)
    ///   - Filhos chamados "BuffButton1", "BuffButton2", "BuffButton3"
    ///   - Dentro de cada botão, um TextMeshProUGUI (usa o primeiro encontrado)
    /// </summary>
    void AutoEncontrarReferencias()
    {
        if (buffPanel == null)
        {
            // Procura inclusive em objetos inativos
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || t.hideFlags != HideFlags.None) continue;
                if (t.name == "buffPanel" && t.gameObject.scene.IsValid())
                {
                    buffPanel = t.gameObject;
                    break;
                }
            }
            if (buffPanel != null) Debug.Log("[WaveBuffUI] buffPanel encontrado automaticamente.");
        }

        if (buffPanel == null)
        {
            Debug.LogWarning("[WaveBuffUI] buffPanel não encontrado — buffs não funcionarão.");
            return;
        }

        bool botoesVazios = buffButtons == null || buffButtons.Length < 3 || buffButtons[0] == null;
        bool textosVazios = buffTexts == null || buffTexts.Length < 3 || buffTexts[0] == null;

        if (botoesVazios) buffButtons = new Button[3];
        if (textosVazios) buffTexts = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            Transform bt = buffPanel.transform.Find($"BuffButton{i + 1}");
            if (bt == null) continue;

            if (botoesVazios)
                buffButtons[i] = bt.GetComponent<Button>();

            if (textosVazios)
                buffTexts[i] = bt.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (botoesVazios || textosVazios)
            Debug.Log("[WaveBuffUI] Referências de botões/textos preenchidas automaticamente.");
    }

    void InicializarBuffs()
    {
        // Buffs balanceados para fim-de-fase (1 escolha por fase, ~2-3 buffs ao longo do jogo).
        // Aproximadamente 2x mais fortes que os buffs de fim-de-onda anteriores.
        todosOsBuffs = new List<BuffOption>
        {
            new BuffOption {
                nome = "Lâmina Afiada",
                descricao = "+10 Dano",
                aplicar = () => bonusDamage += 10f
            },
            new BuffOption {
                nome = "Pele de Ferro",
                descricao = "+75 HP",
                aplicar = () => bonusHealth += 75f
            },
            new BuffOption {
                nome = "Botas de Vento",
                descricao = "+1.5 Velocidade",
                aplicar = () => bonusSpeed += 1.5f
            },
            new BuffOption {
                nome = "Fúria Crescente",
                descricao = "Dano x1.30",
                aplicar = () => multDamage *= 1.30f
            },
            new BuffOption {
                nome = "Vitalidade",
                descricao = "Vida x1.40",
                aplicar = () => multHealth *= 1.40f
            },
            new BuffOption {
                nome = "Adrenalina",
                descricao = "Velocidade x1.25",
                aplicar = () => multSpeed *= 1.25f
            },
            new BuffOption {
                nome = "Coleta de Almas",
                descricao = "+8 Almas/s",
                aplicar = () => {
                    if (SoulManager.Instance != null)
                        SoulManager.Instance.regeneracaoPorSegundo += 8f;
                }
            },
            new BuffOption {
                nome = "Frenesi",
                descricao = "Ataque 25% mais rápido",
                aplicar = () => bonusAttackSpeed += 0.25f
            },
            new BuffOption {
                nome = "Tesouro Real",
                descricao = "+60 Almas iniciais por fase",
                aplicar = () => {
                    if (SoulManager.Instance != null) {
                        SoulManager.Instance.bonusAlmasIniciais += 60;
                        SoulManager.Instance.AdicionarAlmas(60); // efeito imediato também
                    }
                }
            },
            new BuffOption {
                nome = "Pacto Sombrio",
                descricao = "+15 Dano e +50 HP",
                aplicar = () => { bonusDamage += 15f; bonusHealth += 50f; }
            },
        };
    }

    /// <summary>
    /// Chamado pelo VillainSpawner ao fim de todas as ondas de uma fase.
    /// Pausa o jogo, mostra 3 buffs aleatórios e espera a escolha.
    /// Quando o jogador escolhe, <paramref name="aoEscolher"/> é disparado.
    /// </summary>
    public void MostrarEscolha(System.Action aoEscolher)
    {
        onBuffChosen = aoEscolher;

        currentOptions = EscolherBuffsAleatorios(3);

        for (int i = 0; i < buffButtons.Length; i++)
        {
            if (buffButtons[i] == null) continue;

            if (i >= currentOptions.Length) { buffButtons[i].gameObject.SetActive(false); continue; }

            buffButtons[i].gameObject.SetActive(true);

            if (buffTexts != null && i < buffTexts.Length && buffTexts[i] != null)
                buffTexts[i].text = $"{currentOptions[i].nome}\n<size=70%>{currentOptions[i].descricao}</size>";

            int idx = i;
            buffButtons[i].onClick.RemoveAllListeners();
            buffButtons[i].onClick.AddListener(() => SelecionarBuff(idx));
        }

        if (buffPanel != null)
        {
            buffPanel.SetActive(true);
            Canvas canvas = buffPanel.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = true;
        }

        Time.timeScale = 0f;
    }

    void SelecionarBuff(int index)
    {
        if (index < 0 || index >= currentOptions.Length) return;

        currentOptions[index].aplicar?.Invoke();
        Debug.Log($"[WaveBuffUI] Buff escolhido: {currentOptions[index].nome}");

        // Aplica aos aliados existentes (geralmente o campo é limpo logo após na transição,
        // mas mantemos para casos em que o buff é chamado fora do fluxo de transição).
        AplicarBuffsAosAliadosExistentes();

        if (buffPanel != null) buffPanel.SetActive(false);
        Time.timeScale = 1f;

        onBuffChosen?.Invoke();
    }

    BuffOption[] EscolherBuffsAleatorios(int quantidade)
    {
        List<BuffOption> pool = new List<BuffOption>(todosOsBuffs);
        List<BuffOption> escolhidos = new List<BuffOption>();

        for (int i = 0; i < quantidade && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            escolhidos.Add(pool[r]);
            pool.RemoveAt(r);
        }

        return escolhidos.ToArray();
    }

    // ─── APLICAÇÃO DE BUFFS ──────────────────────────────────────

    /// <summary>
    /// Aplica os buffs acumulados a uma carta recém-invocada.
    /// DEVE ser chamado pelo CardSystem imediatamente após o Instantiate
    /// para que a nova carta herde os bônus.
    /// </summary>
    public void AplicarBuffsA(GameObject aliado)
    {
        if (aliado == null) return;

        HeroHealth hero = aliado.GetComponent<HeroHealth>();
        if (hero != null)
        {
            HeroController hc = aliado.GetComponent<HeroController>();
            hero.attackDamage = (hero.attackDamage + bonusDamage) * multDamage;
            hero.maxHealth = (hero.maxHealth + bonusHealth) * multHealth;
            hero.currentHealth = hero.maxHealth;
            if (hc != null) hc.moveSpeed = (hc.moveSpeed + bonusSpeed) * multSpeed;
            if (bonusAttackSpeed > 0f) hero.attackCooldown *= (1f - Mathf.Min(bonusAttackSpeed, 0.8f));
            return;
        }

        GolemHealth golem = aliado.GetComponent<GolemHealth>();
        if (golem != null)
        {
            GolemController gc = aliado.GetComponent<GolemController>();
            golem.attackDamage = (golem.attackDamage + bonusDamage) * multDamage;
            golem.maxHealth = (golem.maxHealth + bonusHealth) * multHealth;
            golem.currentHealth = golem.maxHealth;
            if (gc != null) gc.moveSpeed = (gc.moveSpeed + bonusSpeed) * multSpeed;
            if (bonusAttackSpeed > 0f) golem.attackCooldown *= (1f - Mathf.Min(bonusAttackSpeed, 0.8f));
            return;
        }

        LichHealth lich = aliado.GetComponent<LichHealth>();
        if (lich != null)
        {
            LichAttack la = aliado.GetComponent<LichAttack>();
            lich.maxHealth = (lich.maxHealth + bonusHealth) * multHealth;
            lich.currentHealth = lich.maxHealth;
            if (la != null)
            {
                la.damage = (la.damage + bonusDamage) * multDamage;
                la.moveSpeed = (la.moveSpeed + bonusSpeed) * multSpeed;
                if (bonusAttackSpeed > 0f) la.fireRate *= (1f + bonusAttackSpeed);
            }
            return;
        }
    }

    /// <summary>
    /// Aplica buffs a todas as cartas atualmente na cena.
    /// Útil ao escolher um buff mid-game (caso o fluxo seja alterado no futuro).
    /// </summary>
    public void AplicarBuffsAosAliadosExistentes()
    {
        foreach (HeroHealth hero in FindObjectsByType<HeroHealth>(FindObjectsSortMode.None))
            if (hero != null && hero.gameObject.activeInHierarchy)
                AplicarBuffsA(hero.gameObject);

        foreach (GolemHealth golem in FindObjectsByType<GolemHealth>(FindObjectsSortMode.None))
            if (golem != null && golem.gameObject.activeInHierarchy)
                AplicarBuffsA(golem.gameObject);

        LichHealth lich = LichHealth.Instance;
        if (lich != null) AplicarBuffsA(lich.gameObject);
    }

    // ──────────────────────────────────────────────────────────────
    // DEBUG
    // ──────────────────────────────────────────────────────────────

    public void DebugTogglePainel()
    {
        if (buffPanel == null) { Debug.LogError("[WaveBuffUI] buffPanel NÃO ESTÁ ATRIBUÍDO!"); return; }
        buffPanel.SetActive(!buffPanel.activeInHierarchy);
    }

    public void DebugMostrarBuffs()
    {
        MostrarEscolha(() => Debug.Log("[WaveBuffUI] Buff escolhido (debug)!"));
    }

    /// <summary>
    /// Reset COMPLETO: zera todos os bônus acumulados.
    /// Chame ao iniciar uma partida nova (do menu ou retry no Game Over).
    /// </summary>
    public void ResetCompleto()
    {
        bonusDamage = 0f;
        bonusHealth = 0f;
        bonusSpeed = 0f;
        multDamage = 1f;
        multHealth = 1f;
        multSpeed = 1f;
        bonusAttackSpeed = 0f;
        Debug.Log("[WaveBuffUI] Reset COMPLETO — partida nova.");
    }
}
