using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Decide se um diagnóstico deve abrir um chamado de limpeza e de que tipo.
/// </summary>
public interface IAberturaChamadoService
{
    /// <summary>
    /// Tipo de chamado correspondente ao diagnóstico, ou null quando o diagnóstico não abre chamado
    /// (Normal, AguaSubindo e NivelCritico).
    /// </summary>
    TipoChamado? TipoChamadoPara(TipoDiagnostico diagnostico);

    /// <summary>
    /// Cria o chamado que o diagnóstico exige, ou devolve null se não deve abrir
    /// (diagnóstico sem chamado, chamado do mesmo tipo já ativo, ou chamado de gari durante a chuva).
    /// </summary>
    /// <param name="boca">Boca de lobo diagnosticada.</param>
    /// <param name="diagnostico">Resultado do diagnóstico.</param>
    /// <param name="chovendo">Se está chovendo na leitura atual.</param>
    /// <param name="chamadosDaBoca">Chamados existentes da boca de lobo (ativos ou não).</param>
    /// <param name="agora">Momento da abertura.</param>
    Chamado? AvaliarAbertura(
        BocaDeLobo boca,
        ResultadoDiagnostico diagnostico,
        bool chovendo,
        IEnumerable<Chamado> chamadosDaBoca,
        DateTime agora);
}
