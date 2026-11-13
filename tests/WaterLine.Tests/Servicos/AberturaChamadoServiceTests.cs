using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Servicos;

/// <summary>Abertura de chamado conforme o diagnóstico.</summary>
public class AberturaChamadoServiceTests
{
    private readonly AberturaChamadoService _servico = new();
    private readonly BocaDeLobo _boca = Fabrica.Boca();

    [Theory]
    [InlineData(TipoDiagnostico.EntradaBloqueada, TipoChamado.Preventivo)]
    [InlineData(TipoDiagnostico.SedimentoAcumulando, TipoChamado.Manutencao)]
    [InlineData(TipoDiagnostico.TuboEntupido, TipoChamado.Urgente)]
    public void AvaliarAbertura_DiagnosticoDeEntupimento_AbreChamadoDoTipoCerto(TipoDiagnostico diagnostico, TipoChamado esperado)
    {
        var chamado = _servico.AvaliarAbertura(_boca, Resultado(diagnostico), chovendo: false, Array.Empty<Chamado>(), Fabrica.Agora);

        Assert.NotNull(chamado);
        Assert.Equal(esperado, chamado!.Tipo);
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Equal(_boca.Id, chamado.BocaDeLoboId);
    }

    [Theory]
    [InlineData(TipoDiagnostico.Normal)]
    [InlineData(TipoDiagnostico.AguaSubindo)]
    [InlineData(TipoDiagnostico.NivelCritico)]
    public void AvaliarAbertura_DiagnosticoSemLimpeza_NaoAbreChamado(TipoDiagnostico diagnostico)
    {
        var chamado = _servico.AvaliarAbertura(_boca, Resultado(diagnostico), chovendo: true, Array.Empty<Chamado>(), Fabrica.Agora);

        Assert.Null(chamado);
    }

    [Fact]
    public void AvaliarAbertura_JaExisteChamadoAtivoDoMesmoTipo_NaoDuplica()
    {
        var existente = Fabrica.Chamado(1, TipoChamado.Preventivo, bocaDeLoboId: _boca.Id);

        var chamado = _servico.AvaliarAbertura(
            _boca, Resultado(TipoDiagnostico.EntradaBloqueada), chovendo: false, new[] { existente }, Fabrica.Agora);

        Assert.Null(chamado);
    }

    [Fact]
    public void AvaliarAbertura_ChamadoAnteriorJaConcluido_AbreNovo()
    {
        var gari = Fabrica.GariEmServico(10);
        var concluido = Fabrica.ChamadoAceito(1, gari, TipoChamado.Preventivo, bocaDeLoboId: _boca.Id);
        concluido.Concluir(gari.Id, "a.jpg", "b.jpg", entradaLivreConfirmada: true, Fabrica.Agora);

        var chamado = _servico.AvaliarAbertura(
            _boca, Resultado(TipoDiagnostico.EntradaBloqueada), chovendo: false, new[] { concluido }, Fabrica.Agora);

        Assert.NotNull(chamado);
    }

    [Fact]
    public void AvaliarAbertura_EntradaBloqueadaDuranteAChuva_NaoMandaGari()
    {
        var chamado = _servico.AvaliarAbertura(
            _boca, Resultado(TipoDiagnostico.EntradaBloqueada), chovendo: true, Array.Empty<Chamado>(), Fabrica.Agora);

        Assert.Null(chamado);
    }

    private static ResultadoDiagnostico Resultado(TipoDiagnostico tipo) =>
        new(tipo, NivelCm: 0, PercentualOcupacao: 0, VelocidadeSubidaCmPorMinuto: null, EntradaBloqueada: tipo == TipoDiagnostico.EntradaBloqueada);
}
