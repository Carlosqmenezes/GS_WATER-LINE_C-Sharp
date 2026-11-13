using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Domain.Servicos;

/// <summary>
/// Regra de abertura de chamado a partir do diagnóstico:
/// EntradaBloqueada → Preventivo (gari); SedimentoAcumulando → Manutencao (galeria);
/// TuboEntupido → Urgente (galeria). AguaSubindo e NivelCritico geram alerta, não chamado.
/// </summary>
public sealed class AberturaChamadoService : IAberturaChamadoService
{
    public TipoChamado? TipoChamadoPara(TipoDiagnostico diagnostico) => diagnostico switch
    {
        TipoDiagnostico.EntradaBloqueada => TipoChamado.Preventivo,
        TipoDiagnostico.SedimentoAcumulando => TipoChamado.Manutencao,
        TipoDiagnostico.TuboEntupido => TipoChamado.Urgente,
        _ => null
    };

    public Chamado? AvaliarAbertura(
        BocaDeLobo boca,
        ResultadoDiagnostico diagnostico,
        bool chovendo,
        IEnumerable<Chamado> chamadosDaBoca,
        DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(boca);
        ArgumentNullException.ThrowIfNull(diagnostico);
        ArgumentNullException.ThrowIfNull(chamadosDaBoca);

        if (TipoChamadoPara(diagnostico.Tipo) is not { } tipo)
        {
            return null;
        }

        // Segurança do trabalhador: não manda gari para a rua enquanto está chovendo.
        if (chovendo && Gari.EhTipoDeGari(tipo))
        {
            return null;
        }

        // Não abre chamado duplicado: já existe um do mesmo tipo aberto ou aceito para esta boca.
        var jaExisteAtivo = chamadosDaBoca.Any(c => c.BocaDeLoboId == boca.Id && c.EstaAtivo && c.Tipo == tipo);
        if (jaExisteAtivo)
        {
            return null;
        }

        return new Chamado(boca.Id, tipo, agora);
    }
}
