# HollowCrown — Documentação Técnica

Visão geral da base de código para uso em desenvolvimento. Para o sistema de invocação de aliados especificamente, veja `PlayerControls.md`.

---

## Índice

1. [O Jogo em Uma Frase](#1-o-jogo-em-uma-frase)
2. [Arquitetura Geral](#2-arquitetura-geral)
3. [Singletons e Estado Global](#3-singletons-e-estado-global)
4. [Sistemas por Área](#4-sistemas-por-área)
   - [Inimigos](#41-inimigos)
   - [Aliados](#42-aliados)
   - [Ondas e Fases](#43-ondas-e-fases)
   - [Elites](#44-elites)
   - [Economia (Almas)](#45-economia-almas)
   - [Buffs Entre Fases](#46-buffs-entre-fases)
   - [UI e Menus](#47-ui-e-menus)
   - [Efeitos e Câmera](#48-efeitos-e-câmera)
5. [Como os Sistemas se Comunicam](#5-como-os-sistemas-se-comunicam)
6. [Tags e Layers Esperadas](#6-tags-e-layers-esperadas)
7. [Scripts Fantasma e Redundâncias](#7-scripts-fantasma-e-redundâncias)

---

## 1. O Jogo em Uma Frase

Jogo de defesa 2D top-down: o jogador invoca aliados (Esqueleto, Golem, Lich) usando almas para sobreviver a ondas de inimigos em três mapas progressivos.

---

## 2. Arquitetura Geral

```
Singletons (DontDestroyOnLoad)
├── SoulManager        → economia de almas
├── PlayerLives        → vidas do jogador
├── PhaseTransition    → fade entre fases
└── WaveBuffUI         → buffs persistentes entre ondas

Por Cena
├── VillainSpawner     → controla ondas e spawns de inimigos
├── CardSystem         → input do jogador para invocar aliados
└── CameraFollow       → câmera segue aliado com tag "Player"

Entidades (Instanciadas em Runtime)
├── Aliados: HeroController+Health+Animator / GolemController+Health+Animator / LichAttack+Health+Animator
├── Inimigos: VillainController+Health+Animator / MageAttack+Health+Animator
└── Projéteis: Fireball / LichProjectile
```

O padrão central é simples: **Health scripts fazem o combate e notificam a morte via eventos; Controller scripts fazem a movimentação/IA; Animator scripts sincronizam sprites**. Os três coexistem no mesmo GameObject.

---

## 3. Singletons e Estado Global

| Singleton | Persiste entre cenas? | Reset em game over? |
|---|---|---|
| `SoulManager` | Sim (`DontDestroyOnLoad`) | Sim — `ResetCompleto()` |
| `PlayerLives` | Sim | Implicitamente (cena reinicia) |
| `PhaseTransition` | Sim | N/A — só gerencia fade |
| `WaveBuffUI` | Sim | Sim — `ResetCompleto()` |
| `LichHealth` | Não | N/A — é uma entidade de jogo |

> `MenuGameOver` e `MenuInicialManager` chamam `SoulManager.ResetCompleto()` e `WaveBuffUI.ResetCompleto()` antes de recarregar a cena. Se adicionar estado global novo, lembre de resetar aqui também.

---

## 4. Sistemas por Área

### 4.1 Inimigos

Dois tipos de inimigo: **vilão melee** e **mago ranged**.

| Script | Função |
|---|---|
| `VillainController` | IA de movimento: persegue o aliado mais próximo (Hero/Golem/Lich), desvio de obstáculos, anti-softlock, knockback recebido |
| `VillainHealth` | Vida, ataque melee, recompensa de almas ao morrer. Mantém `static List<VillainHealth> All` — todos os aliados consultam essa lista para encontrar inimigos |
| `VillainAnimator` | Walk 4 direções + attack. Dispara evento `OnImpactFrame` no frame do hit (usado por VillainHealth para aplicar dano no timing certo) |
| `MageAttack` | IA do mago: mantém distância segura, atira `Fireball` em aliados |
| `MageHealth` | Vida + invulnerabilidade pós-hit. Morte chama `PlayerLives.PerderVida()` |
| `MageAnimator` | Idle loop + animação de cast |

**Ponto crítico:** `VillainHealth.All` é o hub central de detecção. Todo aliado varre essa lista para saber onde estão os inimigos. Se um vilão não estiver nessa lista (ex: erro de `OnEnable`/`OnDisable`), os aliados vão ignorá-lo.

---

### 4.2 Aliados

Três criaturas invocáveis pelo jogador via `CardSystem`. Cada uma tem o trio Controller + Health + Animator.

| Criatura | Papel | Destaque |
|---|---|---|
| **Esqueleto** (`Hero*`) | Melee DPS | Mais rápido, atacante primário |
| **Golem** (`Golem*`) | Tank AoE | Mais vida, ataque em área com knockback |
| **Lich** (`Lich*`) | Ranged Caster | Mantém distância, atira `LichProjectile`. Único com Singleton (`LichHealth.Instance`) |

Todos os Controllers atribuem `gameObject.tag = "Player"` no Awake. A câmera e outros scripts buscam aliados por essa tag.

Os Controllers buscam alvos em `VillainHealth.All` (não por tag ou FindObjects). Golem e Hero leem a lista diretamente; o Lich (via `LichAttack`) faz o mesmo.

**Buffs de onda** são aplicados diretamente nos campos públicos (`maxHealth`, `attackDamage`, `moveSpeed`, `attackCooldown`) dos Health/Controller scripts. Aliados invocados depois da escolha de buff **não recebem os buffs acumulados** — é o comportamento esperado atualmente.

Para adicionar uma nova criatura, siga o pipeline em `PlayerControls.md` (seção 4).

---

### 4.3 Ondas e Fases

`VillainSpawner` é o maestro de cada fase. Funciona assim:

```
OnEnable → IniciarTurno()
    → SpawnarTurno() [Coroutine]
        → SpawnInimigo() × N (vilões + magos)
            → TornarElite() se rolar elite
        → cada VillainHealth.OnMorte → OnInimigoMorreu()
    → inimigosVivos == 0 → FinalizarTurno()
        → ondaAtual++ → IniciarTurno() [próxima onda]
    → todas as ondas → FinalizarFase()
        → WaveBuffUI.MostrarEscolha() [aguarda input]
        → PhaseTransition.FadeOutInRoutine()
            → ExecutarTrocaDeMapa() [destrói entidades, troca mapa]
```

Há três mapas configuráveis por ContextMenu no Inspector do VillainSpawner:
- **Externo** — 6 ondas, 10% chance de elite
- **Dungeon** — 8 ondas, 20% chance de elite
- **Royal** — 10 ondas, 30% chance de elite

> A transição de mapa roda em `PhaseTransition` (não no VillainSpawner) porque `ExecutarTrocaDeMapa` desativa o GameObject pai do VillainSpawner — se a coroutine rodasse no próprio spawner, morreria no meio do fade.

---

### 4.4 Elites

`EliteModifier` é adicionado via `AddComponent` pelo `VillainSpawner` logo após o spawn. Não existe no prefab — é 100% runtime.

Cinco tipos: `Frenetico`, `Colosso`, `Sanguessuga`, `Volatil`, `Venenoso`.

Um elite pode ter 1, 2 ou 3 modificadores, dependendo da fase:

| Fase | Max mods | Chance de 2º mod | Chance de 3º mod |
|---|---|---|---|
| Externo | 1 | — | — |
| Dungeon | 2 | 50% | — |
| Royal | 3 | 50% | 50% do anterior |

Modificadores são selecionados de um pool de 5 sem repetição. Cor muda por tipo (1 mod), dourado (2 mods) ou branco (3 mods).

`DotEffect` é um componente separado adicionado ao **alvo** quando o Venenoso acerta. Ticks a cada 1s, bypassa `invulnTimer` via `TakeDotDamage()`.

---

### 4.5 Economia (Almas)

`SoulManager` é a única fonte de verdade de almas. Regeneração passiva contínua; gasto via `TentarGastar()` (retorna bool — não gasta se insuficiente).

Fluxo de almas:
- **Entrada**: regeneração passiva + `AdicionarAlmas()` quando inimigo morre (`VillainHealth.TakeDamage`)
- **Saída**: `CardSystem.TentarGastar()` ao invocar aliado
- **Reset por fase**: `VillainSpawner.OnEnable` chama `ResetarParaFase()` — volta para o valor inicial configurado no Inspector
- **Reset total**: `MenuGameOver` / `MenuInicialManager` chamam `ResetCompleto()`

`SoulCounterUI` escuta o evento `OnAlmasChanged` e atualiza o texto. Nada mais precisa fazer poll de almas.

---

### 4.6 Buffs Entre Fases

`WaveBuffUI` sorteia 3 opções de um pool e exibe ao fim de cada fase (não entre ondas individuais). O jogador escolhe uma. Buffs acumulam em campos internos (`bonusDamage`, `multDamage`, etc.) e são reaplicados a todos os aliados existentes no momento da escolha.

Para um novo tipo de aliado receber buffs, adicione-o em `AplicarBuffsAosAliadosExistentes()` e `CurarTodosAliados()` — o pipeline está detalhado em `PlayerControls.md`.

---

### 4.7 UI e Menus

| Script | Onde fica | O que faz |
|---|---|---|
| `CardSystem` | Cena de jogo | Botões de invocação, hotkeys 1/2/3, spawn seguro |
| `SoulCounterUI` | Canvas da cena de jogo | Mostra almas via TextMeshPro |
| `PauseManager` | Canvas da cena de jogo | `Time.timeScale = 0` ao pausar |
| `MenuGameOver` | Cena Game Over | Restart / Menu. Reseta singletons |
| `MenuInicialManager` | Cena Menu | Start / How to Play. Reseta singletons |

---

### 4.8 Efeitos e Câmera

| Script | Observação |
|---|---|
| `CameraFollow` | Busca `FindWithTag("Player")` se `target` for nulo. Se múltiplos aliados existirem, segue o primeiro encontrado |
| `BackgroundMusic` | **Não** usa `DontDestroyOnLoad` — a música para ao trocar de cena. Se quiser música contínua, adicione `DontDestroyOnLoad` e lógica para não duplicar |
| `Fireball` | Usado por **ambos** inimigos (Mago) e aliados futuros. Tem flag `isAlly` que inverte quem é alvo |
| `LichProjectile` | Projétil exclusivo do Lich (aliado). Lógica similar ao Fireball mas sem flag |
| `SwordEffect` | Efeito visual de slash. Posicionado e rotacionado pelo `VillainAnimator` no frame de ataque |

---

## 5. Como os Sistemas se Comunicam

```
VillainSpawner
    │ instancia
    ▼
VillainHealth ──── OnMorte event ────► VillainSpawner (conta mortos)
    │                                  SoulManager.AdicionarAlmas()
    │ OnDanoCausado event
    ▼
EliteModifier (Sanguessuga/Venenoso)
    │ adiciona componente
    ▼
DotEffect (no alvo)

HeroHealth / GolemHealth / LichHealth
    │ TakeDamage → currentHealth <= 0
    ▼
PlayerLives.PerderVida()
    │ vidas == 0
    ▼
PhaseTransition.FadeOut → carrega cena GameOver

CardSystem
    │ clique do jogador
    ├─► SoulManager.TentarGastar()
    └─► Instantiate(prefab) → aliado na cena

WaveBuffUI
    │ MostrarEscolha() ← VillainSpawner (fim de fase)
    │ jogador escolhe
    └─► AplicarBuffsAosAliadosExistentes() → modifica campos de Health/Controller
```

---

## 6. Tags e Layers Esperadas

| Tag | Quem usa | Quem recebe |
|---|---|---|
| `Player` | CameraFollow (busca alvo), LichAttack (segue aliado), VillainController (busca herói) | HeroController.Awake, GolemController.Awake, Lich (manual no prefab) |

| Layer | Para quê |
|---|---|
| `Allies` | Organização — aliados |
| `Enemies` | Organização — inimigos |
| `Obstacles` | `LayerMask obstacleLayer` nos Controllers. Raycasts de desvio verificam essa layer |

---

## 7. Scripts Fantasma e Redundâncias

### Scripts sem referência conhecida

**`SoldierAnimator.cs`** — Anima um "soldado" com loop contínuo de sprites. Nenhum prefab, nenhum script o instancia ou referencia. Candidato a remoção ou era placeholder de uma criatura não implementada.

**`Projectile.cs`** — Projétil genérico que se move apenas no eixo X. Destrói ao colidir com tag `"Player"`. Não encontrado em nenhum ponto de instanciação no codebase atual. Provavelmente substituído por `Fireball` e `LichProjectile`.

### Código duplicado entre scripts

**Anti-softlock** — A mesma lógica de detecção de travamento (timer de posição, impulso de escape, raycast de obstáculo) está copiada em `HeroController`, `GolemController` e `VillainController`. Funciona, mas qualquer ajuste precisa ser feito nos três lugares.

**`Fireball` vs `LichProjectile`** — Mesma responsabilidade (projétil que causa dano ao colidir), implementações separadas. `Fireball` é mais geral (flag `isAlly`). `LichProjectile` é só para o Lich aliado. Poderiam ser unificados no `Fireball` com a flag, mas não há urgência.

**Animadores de inimigo** (`VillainAnimator`, `MageAnimator`) e de aliados (`HeroAnimator`, `GolemAnimator`, `LichAnimator`) têm estrutura idêntica (array de sprites, frameRate, TriggerAttack). Não são problema agora, mas se adicionar mais criaturas, vale considerar uma base comum.

### Comportamento a confirmar

**`BackgroundMusic`** não usa `DontDestroyOnLoad`. Se a intenção é música contínua entre fases, isso vai reiniciar a música a cada troca de cena. Se for intencional (música diferente por fase), está correto como está.

**Buffs não retroativos** — aliados invocados depois da escolha de buff não recebem os buffs acumulados de ondas anteriores. Pode ser intencional (balanceamento) ou bug latente dependendo do design.
