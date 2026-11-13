using WaterLine.Domain.Comum;
using WaterLine.Domain.Enums;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Alerta gerado quando a água sobe rápido (alerta precoce) ou chega ao nível crítico (emergência).
/// </summary>
public class Alerta : EntidadeBase
{
    public const int TamanhoMaximoMensagem = 500;

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected Alerta()
    {
        Mensagem = string.Empty;
    }

    public Alerta(int bocaDeLoboId, NivelAlerta nivel, string mensagem, DateTime criadoEm)
    {
        BocaDeLoboId = bocaDeLoboId;
        Nivel = nivel;
        Mensagem = Validacao.TextoObrigatorio(mensagem, nameof(Mensagem), TamanhoMaximoMensagem);
        CriadoEm = Validacao.EmUtc(criadoEm);
    }

    public int BocaDeLoboId { get; private set; }

    public BocaDeLobo? BocaDeLobo { get; private set; }

    public NivelAlerta Nivel { get; private set; }

    public string Mensagem { get; private set; }

    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Cria o alerta correspondente a um diagnóstico de água subindo ou de nível crítico.
    /// Retorna null para diagnósticos que não geram alerta.
    /// </summary>
    public static Alerta? CriarPorDiagnostico(BocaDeLobo boca, TipoDiagnostico diagnostico, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(boca);

        return diagnostico switch
        {
            TipoDiagnostico.AguaSubindo => new Alerta(
                boca.Id,
                NivelAlerta.AlertaPrecoce,
                $"Água subindo rápido na boca de lobo {boca.Codigo} ({boca.Endereco}). " +
                "A rua pode alagar em breve: retire veículos e proteja mercadorias.",
                agora),
            TipoDiagnostico.NivelCritico => new Alerta(
                boca.Id,
                NivelAlerta.Emergencia,
                $"Nível crítico na boca de lobo {boca.Codigo} ({boca.Endereco}). " +
                "Alagamento em curso: evite a área. A central da cidade foi acionada.",
                agora),
            _ => null
        };
    }
}
