using UnityEngine;

// Componente adicionado ao alvo quando atingido por inimigo Venenoso.
// Ticks a cada 1 segundo; renovável se o alvo levar outro hit do mesmo tipo.
public class DotEffect : MonoBehaviour
{
    private float danoPorTick;
    private float duracao;
    private float timerDuracao;
    private float timerTick;

    private const float TICK_INTERVAL = 1f;

    public void Inicializar(float dps, float dur)
    {
        danoPorTick = dps * TICK_INTERVAL;
        duracao = dur;
        timerDuracao = dur;
        timerTick = TICK_INTERVAL;
    }

    public void Renovar(float novaDuracao)
    {
        timerDuracao = Mathf.Max(timerDuracao, novaDuracao);
    }

    void Update()
    {
        timerDuracao -= Time.deltaTime;
        if (timerDuracao <= 0f)
        {
            Destroy(this);
            return;
        }

        timerTick -= Time.deltaTime;
        if (timerTick <= 0f)
        {
            timerTick = TICK_INTERVAL;
            AplicarDano();
        }
    }

    void AplicarDano()
    {
        // TakeDotDamage bypassa invulnerabilidade para evitar que o dot seja bloqueado
        HeroHealth hh = GetComponent<HeroHealth>();
        if (hh != null) { hh.TakeDotDamage(danoPorTick); return; }

        LichHealth lh = GetComponent<LichHealth>();
        if (lh != null) { lh.TakeDamage(danoPorTick); return; }

        MageHealth mh = GetComponent<MageHealth>();
        if (mh != null) { mh.TakeDamage(danoPorTick); }
    }
}
