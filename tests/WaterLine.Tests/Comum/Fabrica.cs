using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;

namespace WaterLine.Tests.Comum;

/// <summary>
/// Cria entidades e configurações para os testes, com valores fictícios.
/// Boca de lobo padrão: 80 cm de profundidade e 50 cm de abertura.
/// </summary>
internal static class Fabrica
{
    public static readonly DateTime Agora = new(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);

    public const double LatitudeBase = -23.55000;
    public const double LongitudeBase = -46.64000;

    /// <summary>Aproximadamente 111 m por milésimo de grau de latitude.</summary>
    public const double GrausPorCemMetros = 0.0009;

    public static RegrasDiagnosticoOptions RegrasDiagnostico() => new();

    public static RegrasDespachoOptions RegrasDespacho(double raioMetros = 300) => new() { RaioMetros = raioMetros };

    public static RegrasPrevencaoOptions RegrasPrevencao() => new();

    public static BocaDeLobo Boca(
        int id = 1,
        double latitude = LatitudeBase,
        double longitude = LongitudeBase,
        double profundidadeCm = 80,
        double larguraAberturaCm = 50)
    {
        var boca = new BocaDeLobo($"BL-{id:0000}", "Rua Fictícia de Teste, 1", latitude, longitude, profundidadeCm, larguraAberturaCm);
        boca.DefinirId(id);
        return boca;
    }

    public static Leitura Leitura(
        int bocaDeLoboId = 1,
        DateTime? dataHora = null,
        double distanciaExternaCm = 50,
        double distanciaInternaCm = 80,
        bool chovendo = false)
    {
        var leitura = new Leitura(bocaDeLoboId, dataHora ?? Agora, distanciaExternaCm, distanciaInternaCm, chovendo);
        return leitura;
    }

    /// <summary>Gari em serviço a uma distância aproximada (em metros, para o norte) da boca padrão.</summary>
    public static Gari GariEmServico(int id, double metrosAoNorte = 0)
    {
        var gari = new Gari($"Gari Teste {id}", $"GAR-T{id:000}");
        gari.DefinirId(id);
        gari.EntrarEmServico(LatitudeBase + metrosAoNorte / 100 * GrausPorCemMetros, LongitudeBase);
        return gari;
    }

    public static EquipeGaleria EquipeEmServico(int id, double metrosAoNorte = 0)
    {
        var equipe = new EquipeGaleria($"Equipe Teste {id}", $"GAL-T{id:000}");
        equipe.DefinirId(id);
        equipe.EntrarEmServico(LatitudeBase + metrosAoNorte / 100 * GrausPorCemMetros, LongitudeBase);
        return equipe;
    }

    public static Chamado Chamado(
        int id,
        TipoChamado tipo = TipoChamado.Preventivo,
        int bocaDeLoboId = 1,
        int? prioridade = null,
        DateTime? abertoEm = null)
    {
        var chamado = new Chamado(bocaDeLoboId, tipo, abertoEm ?? Agora, prioridade);
        chamado.DefinirId(id);
        return chamado;
    }

    /// <summary>Chamado já aceito pelo agente informado.</summary>
    public static Chamado ChamadoAceito(int id, Agente agente, TipoChamado tipo = TipoChamado.Preventivo, int bocaDeLoboId = 1)
    {
        var chamado = Chamado(id, tipo, bocaDeLoboId);
        chamado.Aceitar(agente, Agora);
        return chamado;
    }
}
