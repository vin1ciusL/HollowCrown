using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Sistema de escolha de buff ao final de cada onda.
/// Gerencia a UI de seleção e aplica os buffs escolhidos globalmente.
/// Deve ser colocado em um GameObject com um Canvas (ou filho de Canvas).
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

    // ─── Estado interno ──────────────────────────────────────────
    private System.Action onBuffChosen;
    private BuffOption[] currentOptions;

    // ─── Buffs acumulados (multiplicadores globais) ──────────────
    [HideInInspector] public float bonusDamage = 0f;      // +flat damage
    [HideInInspector] public float bonusHealth = 0f;       // +flat HP
    [HideInInspector] public float bonusSpeed = 0f;        // +flat speed
    [HideInInspector] public float multDamage = 1f;        // multiplicador dano
    [HideInInspector] public float multHealth = 1f;        // multiplicador vida
    [HideInInspector] public float multSpeed = 1f;         // multiplicador velocidade
    [HideInInspector] public float bonusRegenAlmas = 0f;   // +regen almas/s
    [HideInInspector] public float bonusAttackSpeed = 0f;  // -cooldown %

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

        if (buffPanel != null) buffPanel.SetActive(false);

        InicializarBuffs();
    }

    void InicializarBuffs()
    {
        todosOsBuffs = new List<BuffOption>
        {
            new BuffOption {
                nome = "Lâmina Afiada",
                descricao = "+5 Dano para aliados",
                aplicar = () => bonusDamage += 5f
            },
            new BuffOption {
                nome = "Pele de Ferro",
                descricao = "+30 Vida máx. para aliados",
                aplicar = () => bonusHealth += 30f
            },
            new BuffOption {
                nome = "Botas de Vento",
                descricao = "+1 Velocidade para aliados",
                aplicar = () => bonusSpeed += 1f
            },
            new BuffOption {
                nome = "Fúria Crescente",
                descricao = "Dano x1.15",
                aplicar = () => multDamage *= 1.15f
            },
            new BuffOption {
                nome = "Vitalidade",
                descricao = "Vida x1.2",
                aplicar = () => multHealth *= 1.2f
            },
            new BuffOption {
                nome = "Adrenalina",
                descricao = "Velocidade x1.15",
                aplicar = () => multSpeed *= 1.15f
            },
            new BuffOption {
                nome = "Coleta de Almas",
                descricao = "+5 Almas/seg",
                aplicar = () => {
                    bonusRegenAlmas += 5f;
                    if (SoulManager.Instance != null)
                        SoulManager.Instance.regeneracaoPorSegundo += 5f;
                }
            },
            new BuffOption {
                nome = "Frenesi",
                descricao = "Ataque 15% mais rápido",
                aplicar = () => bonusAttackSpeed += 0.15f
            },
            new BuffOption {
                nome = "Cura Instantânea",
                descricao = "Cura todos os aliados em 50%",
                aplicar = () => CurarTodosAliados(0.5f)
            },
            new BuffOption {
                nome = "Reforço de Almas",
                descricao = "+80 Almas imediatas",
                aplicar = () => {
                    if (SoulManager.Instance != null)
                        SoulManager.Instance.AdicionarAlmas(80);
                }
            },
        };
    }

    /// <summary>
    /// Chamado pelo VillainSpawner ao fim de cada onda.
    /// Pausa o jogo, mostra 3 buffs aleatórios e espera a escolha.
    /// </summary>
    public void MostrarEscolha(System.Action aoEscolher)
    {
        onBuffChosen = aoEscolher;

        // Escolhe 3 buffs aleatórios sem repetição
        currentOptions = EscolherBuffsAleatorios(3);

        for (int i = 0; i < buffButtons.Length; i++)
        {
            if (i >= currentOptions.Length) { buffButtons[i].gameObject.SetActive(false); continue; }

            buffButtons[i].gameObject.SetActive(true);

            if (buffTexts != null && i < buffTexts.Length && buffTexts[i] != null)
                buffTexts[i].text = $"<b>{currentOptions[i].nome}</b>\n{currentOptions[i].descricao}";

            int idx = i; // captura local para closure
            buffButtons[i].onClick.RemoveAllListeners();
            buffButtons[i].onClick.AddListener(() => SelecionarBuff(idx));
        }

        if (buffPanel != null) buffPanel.SetActive(true);
        Time.timeScale = 0f; // Pausa o jogo
    }

    void SelecionarBuff(int index)
    {
        if (index < 0 || index >= currentOptions.Length) return;

        // Aplica o buff
        currentOptions[index].aplicar?.Invoke();

        Debug.Log($"[WaveBuffUI] Buff escolhido: {currentOptions[index].nome}");

        // Aplica stats aos aliados existentes
        AplicarBuffsAosAliadosExistentes();

        // Fecha UI e retorna o jogo
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

    // ─── Aplicação de buffs aos aliados existentes ───────────────

    /// <summary>
    /// Recalcula os stats de todos os aliados ativos na cena
    /// baseado nos multiplicadores acumulados.
    /// </summary>
    public void AplicarBuffsAosAliadosExistentes()
    {
        // Heróis (Esqueleto)
        foreach (HeroHealth hero in FindObjectsByType<HeroHealth>(FindObjectsSortMode.None))
        {
            if (hero == null || !hero.gameObject.activeInHierarchy) continue;
            HeroController hc = hero.GetComponent<HeroController>();

            hero.attackDamage = (hero.attackDamage + bonusDamage) * multDamage;
            hero.maxHealth = (hero.maxHealth + bonusHealth) * multHealth;
            if (hero.currentHealth > hero.maxHealth) hero.currentHealth = hero.maxHealth;
            if (hc != null) hc.moveSpeed = (hc.moveSpeed + bonusSpeed) * multSpeed;
            if (bonusAttackSpeed > 0f) hero.attackCooldown *= (1f - bonusAttackSpeed);
        }

        // Golem
        foreach (GolemHealth golem in FindObjectsByType<GolemHealth>(FindObjectsSortMode.None))
        {
            if (golem == null || !golem.gameObject.activeInHierarchy) continue;
            GolemController gc = golem.GetComponent<GolemController>();

            golem.attackDamage = (golem.attackDamage + bonusDamage) * multDamage;
            golem.maxHealth = (golem.maxHealth + bonusHealth) * multHealth;
            if (golem.currentHealth > golem.maxHealth) golem.currentHealth = golem.maxHealth;
            if (gc != null) gc.moveSpeed = (gc.moveSpeed + bonusSpeed) * multSpeed;
            if (bonusAttackSpeed > 0f) golem.attackCooldown *= (1f - bonusAttackSpeed);
        }

        // Lich
        LichHealth lich = LichHealth.Instance;
        if (lich != null)
        {
            LichAttack la = lich.GetComponent<LichAttack>();
            lich.maxHealth = (lich.maxHealth + bonusHealth) * multHealth;
            if (lich.currentHealth > lich.maxHealth) lich.currentHealth = lich.maxHealth;
            if (la != null)
            {
                la.damage = (la.damage + bonusDamage) * multDamage;
                la.moveSpeed = (la.moveSpeed + bonusSpeed) * multSpeed;
            }
        }
    }

    void CurarTodosAliados(float percentual)
    {
        foreach (HeroHealth hero in FindObjectsByType<HeroHealth>(FindObjectsSortMode.None))
        {
            if (hero != null && hero.gameObject.activeInHierarchy)
                hero.currentHealth = Mathf.Min(hero.maxHealth, hero.currentHealth + hero.maxHealth * percentual);
        }

        foreach (GolemHealth golem in FindObjectsByType<GolemHealth>(FindObjectsSortMode.None))
        {
            if (golem != null && golem.gameObject.activeInHierarchy)
                golem.currentHealth = Mathf.Min(golem.maxHealth, golem.currentHealth + golem.maxHealth * percentual);
        }

        LichHealth lich = LichHealth.Instance;
        if (lich != null)
            lich.currentHealth = Mathf.Min(lich.maxHealth, lich.currentHealth + lich.maxHealth * percentual);
    }
}
