using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Sistema unificado de invocação por cartas.
/// Gerencia TODAS as criaturas aliadas: Esqueleto, Golem, Lich (e futuras).
/// Não há limite de quantidade — a invocação é controlada apenas pelo custo de almas (SoulManager).
/// </summary>
public class CardSystem : MonoBehaviour
{
    // ─── Dados de cada carta ────────────────────────────────────
    [System.Serializable]
    public class CardEntry
    {
        [Tooltip("Nome da criatura (apenas para referência no Inspector)")]
        public string nome;

        [Tooltip("Prefab da criatura a ser invocada")]
        public GameObject prefab;

        [Tooltip("Imagem UI da carta (Button Image)")]
        public Image cardImage;

        [Tooltip("Custo em almas para invocar esta criatura")]
        public int custoAlmas = 50;

        [Tooltip("Tecla de atalho para selecionar esta carta (opcional)")]
        public KeyCode hotkey = KeyCode.None;
    }

    [Header("Cartas de Invocação")]
    [Tooltip("Lista de todas as cartas disponíveis. Adicione novos aliados aqui.")]
    public CardEntry[] cartas;

    [Header("Cores")]
    public Color colorDefault = Color.white;
    public Color colorSelected = Color.red;
    [Tooltip("Cor quando o jogador não tem almas suficientes")]
    public Color colorInsuficiente = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    [Header("Spawn")]
    [Tooltip("Raio de checagem para evitar spawn dentro de obstáculos")]
    public float spawnCheckRadius = 0.5f;
    public Camera mainCamera;

    [Header("Spawn Seguro — Limites do Mapa")]
    [Tooltip("Collider2D do mapa. Se atribuído, só permite spawn dentro dele.")]
    public Collider2D mapBounds;

    // ─── Estado interno ─────────────────────────────────────────
    /// <summary>
    /// Índice da carta selecionada (-1 = nenhuma).
    /// </summary>
    private int cartaSelecionada = -1;

    /// <summary>
    /// Lista de todas as instâncias invocadas (para referência futura, buffs, etc.)
    /// </summary>
    private List<GameObject> instanciasInvocadas = new List<GameObject>();

    // ─── Unity Lifecycle ────────────────────────────────────────

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        AtualizarCores();
    }

    void Update()
    {
        // Atalhos de teclado
        if (cartas != null)
        {
            for (int i = 0; i < cartas.Length; i++)
            {
                if (cartas[i].hotkey != KeyCode.None && Input.GetKeyDown(cartas[i].hotkey))
                {
                    SelecionarCarta(i);
                    break;
                }
            }
        }

        // Clique para invocar
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (cartaSelecionada >= 0)
                TentarInvocar();
        }

        // Botão direito / Escape para cancelar seleção
        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            cartaSelecionada = -1;
            AtualizarCores();
        }

        // Atualiza visual das cartas (custo insuficiente)
        AtualizarCoresInsuficiente();

        // Limpa instâncias destruídas da lista
        LimparInstanciasDestruidas();
    }

    // ─── Seleção de Cartas ──────────────────────────────────────

    /// <summary>
    /// Chamado pelo botão UI de cada carta (via OnClick no Inspector).
    /// Passe o índice da carta no array 'cartas'.
    /// </summary>
    public void SelecionarCarta(int indice)
    {
        if (cartas == null || indice < 0 || indice >= cartas.Length) return;

        // Toggle: se já está selecionada, deseleciona
        if (cartaSelecionada == indice)
        {
            cartaSelecionada = -1;
        }
        else
        {
            // Verifica se tem almas suficientes antes de selecionar
            if (SoulManager.Instance != null && SoulManager.Instance.AlmasAtuais < cartas[indice].custoAlmas)
            {
                Debug.Log($"[CardSystem] Almas insuficientes para {cartas[indice].nome}! Necessário: {cartas[indice].custoAlmas}, Atual: {SoulManager.Instance.AlmasAtuais}");
                return;
            }
            cartaSelecionada = indice;
        }

        AtualizarCores();
    }

    // Métodos de conveniência para conectar botões no Inspector sem precisar de índice:
    // Configure o OnClick de cada botão chamando SelecionarCarta(0), SelecionarCarta(1), etc.

    // ─── Invocação ──────────────────────────────────────────────

    void TentarInvocar()
    {
        if (cartas == null || cartaSelecionada < 0 || cartaSelecionada >= cartas.Length) return;

        CardEntry carta = cartas[cartaSelecionada];

        if (carta.prefab == null)
        {
            Debug.LogError($"[CardSystem] Prefab não atribuído para a carta '{carta.nome}'!");
            return;
        }

        // Verifica SoulManager
        if (SoulManager.Instance == null)
        {
            Debug.LogError("[CardSystem] SoulManager.Instance é null! Certifique-se de ter um SoulManager na cena.");
            return;
        }

        // Verifica custo
        if (SoulManager.Instance.AlmasAtuais < carta.custoAlmas)
        {
            Debug.Log($"[CardSystem] Almas insuficientes para {carta.nome}! Necessário: {carta.custoAlmas}, Atual: {SoulManager.Instance.AlmasAtuais}");
            cartaSelecionada = -1;
            AtualizarCores();
            return;
        }

        // Calcula posição de spawn
        Vector2 screenPos = Mouse.current.position.ReadValue();
        float camZ = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));
        worldPos.z = 0f;

        // Verifica se está dentro do mapa
        if (mapBounds != null && !mapBounds.OverlapPoint(worldPos))
        {
            Debug.Log("[CardSystem] Posição fora dos limites do mapa!");
            return;
        }

        // Verifica obstáculos
        Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius);
        if (hit != null && !hit.isTrigger)
        {
            Debug.Log("[CardSystem] Local bloqueado por: " + hit.gameObject.name);
            return;
        }

        // Gasta almas
        if (!SoulManager.Instance.TentarGastar(carta.custoAlmas))
        {
            Debug.Log($"[CardSystem] Falha ao gastar almas para {carta.nome}.");
            return;
        }

        // Instancia a criatura
        GameObject instancia = Instantiate(carta.prefab, worldPos, Quaternion.identity);
        instanciasInvocadas.Add(instancia);

        // Aplica buffs acumulados de fases anteriores
        if (WaveBuffUI.Instance != null)
            WaveBuffUI.Instance.AplicarBuffsA(instancia);

        Debug.Log($"[CardSystem] {carta.nome} invocado em {worldPos} (custo: {carta.custoAlmas} almas)");

        // Deseleciona a carta após invocar
        cartaSelecionada = -1;
        AtualizarCores();
    }

    // ─── Visual ─────────────────────────────────────────────────

    void AtualizarCores()
    {
        if (cartas == null) return;

        for (int i = 0; i < cartas.Length; i++)
        {
            if (cartas[i].cardImage == null) continue;
            cartas[i].cardImage.color = (i == cartaSelecionada) ? colorSelected : colorDefault;
        }
    }

    /// <summary>
    /// Atualiza visualmente as cartas cujo custo excede as almas atuais.
    /// </summary>
    void AtualizarCoresInsuficiente()
    {
        if (cartas == null || SoulManager.Instance == null) return;

        for (int i = 0; i < cartas.Length; i++)
        {
            if (cartas[i].cardImage == null) continue;
            if (i == cartaSelecionada) continue; // não sobrescreve a cor de seleção

            if (SoulManager.Instance.AlmasAtuais < cartas[i].custoAlmas)
                cartas[i].cardImage.color = colorInsuficiente;
            else
                cartas[i].cardImage.color = colorDefault;
        }
    }

    // ─── Utilitários ────────────────────────────────────────────

    void LimparInstanciasDestruidas()
    {
        instanciasInvocadas.RemoveAll(inst => inst == null);
    }

    /// <summary>
    /// Retorna todas as instâncias invocadas que ainda estão vivas.
    /// Útil para aplicar buffs, contar aliados, etc.
    /// </summary>
    public List<GameObject> ObterInstanciasVivas()
    {
        LimparInstanciasDestruidas();
        return new List<GameObject>(instanciasInvocadas);
    }

    /// <summary>
    /// Retorna o número total de aliados vivos invocados.
    /// </summary>
    public int TotalAliadosVivos()
    {
        LimparInstanciasDestruidas();
        return instanciasInvocadas.Count;
    }
}