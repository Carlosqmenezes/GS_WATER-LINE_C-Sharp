using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Servicos;

/// <summary>
/// Tabela de diagnóstico. Boca padrão: 80 cm de profundidade e 50 cm de abertura.
/// Distância interna 80 = caixa vazia; cada 8 cm a menos = +10% de ocupação.
/// </summary>
public class DiagnosticoServiceTests
{
    private readonly DiagnosticoService _servico = new(Fabrica.RegrasDiagnostico());
    private readonly BocaDeLobo _boca = Fabrica.Boca();

    [Fact]
    public void Diagnosticar_EntradaLivreEFundoVazio_RetornaNormal()
    {
        var leitura = Fabrica.Leitura(distanciaExternaCm: 50, distanciaInternaCm: 80);

        var resultado = _servico.Diagnosticar(_boca, leitura, Array.Empty<Leitura>());

        Assert.Equal(TipoDiagnostico.Normal, resultado.Tipo);
        Assert.Equal(0, resultado.PercentualOcupacao);
        Assert.False(resultado.EntradaBloqueada);
        Assert.Null(resultado.VelocidadeSubidaCmPorMinuto);
    }

    [Theory]
    [InlineData(34.9)]
    [InlineData(10)]
    [InlineData(0)]
    public void Diagnosticar_DistanciaExternaAbaixoDe70PorCentoDaAbertura_RetornaEntradaBloqueada(double distanciaExterna)
    {
        var leitura = Fabrica.Leitura(distanciaExternaCm: distanciaExterna);

        var resultado = _servico.Diagnosticar(_boca, leitura, Array.Empty<Leitura>());

        Assert.Equal(TipoDiagnostico.EntradaBloqueada, resultado.Tipo);
        Assert.True(resultado.EntradaBloqueada);
    }

    [Fact]
    public void Diagnosticar_DistanciaExternaExatamenteNoLimite_ConsideraEntradaLivre()
    {
        // 70% de 50 cm = 35 cm: a regra é "abaixo de 70%".
        var leitura = Fabrica.Leitura(distanciaExternaCm: 35);

        var resultado = _servico.Diagnosticar(_boca, leitura, Array.Empty<Leitura>());

        Assert.Equal(TipoDiagnostico.Normal, resultado.Tipo);
    }

    [Theory]
    [InlineData(56, 30)] // 24 cm de nível = 30%
    [InlineData(48, 40)] // 32 cm = 40%
    [InlineData(33, 58.75)] // 47 cm = 58,75%
    public void Diagnosticar_SemChuvaEOcupacaoEntre30E60PorCento_RetornaSedimentoAcumulando(double distanciaInterna, double percentualEsperado)
    {
        var leitura = Fabrica.Leitura(distanciaInternaCm: distanciaInterna);

        var resultado = _servico.Diagnosticar(_boca, leitura, Array.Empty<Leitura>());

        Assert.Equal(TipoDiagnostico.SedimentoAcumulando, resultado.Tipo);
        Assert.Equal(percentualEsperado, resultado.PercentualOcupacao, precision: 2);
    }

    [Fact]
    public void Diagnosticar_SemChuvaEAguaParadaAcimaDe60PorCentoHaMaisDe24Horas_RetornaTuboEntupido()
    {
        // 70% de ocupação (distância interna 24) em todas as leituras das últimas 25 horas.
        var historico = Enumerable.Range(1, 25)
            .Select(h => Fabrica.Leitura(dataHora: Fabrica.Agora.AddHours(-h), distanciaInternaCm: 24))
            .ToList();
        var atual = Fabrica.Leitura(distanciaInternaCm: 24);

        var resultado = _servico.Diagnosticar(_boca, atual, historico);

        Assert.Equal(TipoDiagnostico.TuboEntupido, resultado.Tipo);
    }

    [Fact]
    public void Diagnosticar_AguaParadaHaMenosDe24Horas_NaoDiagnosticaTuboEntupido()
    {
        // Só 10 horas de água parada: pode ser a última chuva escoando.
        var historico = Enumerable.Range(1, 10)
            .Select(h => Fabrica.Leitura(dataHora: Fabrica.Agora.AddHours(-h), distanciaInternaCm: 24))
            .ToList();
        var atual = Fabrica.Leitura(distanciaInternaCm: 24);

        var resultado = _servico.Diagnosticar(_boca, atual, historico);

        Assert.NotEqual(TipoDiagnostico.TuboEntupido, resultado.Tipo);
        Assert.Equal(TipoDiagnostico.Normal, resultado.Tipo);
    }

    [Fact]
    public void Diagnosticar_ChuvaNoMeioDaAguaParada_ReiniciaAContagemDo24Horas()
    {
        // 30 h de água parada, mas choveu há 5 h: a contagem recomeça depois da chuva.
        var historico = Enumerable.Range(1, 30)
            .Select(h => Fabrica.Leitura(
                dataHora: Fabrica.Agora.AddHours(-h),
                distanciaInternaCm: 24,
                chovendo: h == 5))
            .ToList();
        var atual = Fabrica.Leitura(distanciaInternaCm: 24);

        var resultado = _servico.Diagnosticar(_boca, atual, historico);

        Assert.NotEqual(TipoDiagnostico.TuboEntupido, resultado.Tipo);
    }

    [Fact]
    public void Diagnosticar_ChovendoEAguaSubindo5CmPorMinutoOuMais_RetornaAguaSubindo()
    {
        // Anterior: 20 cm de nível. Atual, 1 minuto depois: 30 cm. Velocidade = 10 cm/min; ocupação 37,5%.
        var anterior = Fabrica.Leitura(dataHora: Fabrica.Agora.AddMinutes(-1), distanciaInternaCm: 60, chovendo: true);
        var atual = Fabrica.Leitura(distanciaInternaCm: 50, chovendo: true);

        var resultado = _servico.Diagnosticar(_boca, atual, new[] { anterior });

        Assert.Equal(TipoDiagnostico.AguaSubindo, resultado.Tipo);
        Assert.Equal(10, resultado.VelocidadeSubidaCmPorMinuto!.Value, precision: 2);
        Assert.True(resultado.GeraAlerta);
    }

    [Fact]
    public void Diagnosticar_ChovendoESubidaLenta_NaoRetornaAguaSubindo()
    {
        // 4 cm em 2 minutos = 2 cm/min, abaixo do limite de 5 cm/min.
        var anterior = Fabrica.Leitura(dataHora: Fabrica.Agora.AddMinutes(-2), distanciaInternaCm: 60, chovendo: true);
        var atual = Fabrica.Leitura(distanciaInternaCm: 56, chovendo: true);

        var resultado = _servico.Diagnosticar(_boca, atual, new[] { anterior });

        Assert.Equal(TipoDiagnostico.Normal, resultado.Tipo);
        Assert.Equal(2, resultado.VelocidadeSubidaCmPorMinuto!.Value, precision: 2);
    }

    [Fact]
    public void Diagnosticar_UsaALeituraImediatamenteAnteriorParaAVelocidade()
    {
        var maisAntiga = Fabrica.Leitura(dataHora: Fabrica.Agora.AddMinutes(-10), distanciaInternaCm: 80, chovendo: true);
        var anterior = Fabrica.Leitura(dataHora: Fabrica.Agora.AddMinutes(-1), distanciaInternaCm: 52, chovendo: true);
        var atual = Fabrica.Leitura(distanciaInternaCm: 50, chovendo: true);

        // Ordem do histórico propositalmente embaralhada.
        var resultado = _servico.Diagnosticar(_boca, atual, new[] { anterior, maisAntiga });

        Assert.Equal(2, resultado.VelocidadeSubidaCmPorMinuto!.Value, precision: 2);
        Assert.Equal(TipoDiagnostico.Normal, resultado.Tipo);
    }

    [Fact]
    public void Diagnosticar_ChovendoEOcupacao85PorCentoOuMais_RetornaNivelCritico()
    {
        // 72 cm de nível = 90%.
        var atual = Fabrica.Leitura(distanciaInternaCm: 8, chovendo: true);

        var resultado = _servico.Diagnosticar(_boca, atual, Array.Empty<Leitura>());

        Assert.Equal(TipoDiagnostico.NivelCritico, resultado.Tipo);
        Assert.True(resultado.GeraAlerta);
    }

    [Fact]
    public void Diagnosticar_NivelCriticoTemPrecedenciaSobreAguaSubindo()
    {
        var anterior = Fabrica.Leitura(dataHora: Fabrica.Agora.AddMinutes(-1), distanciaInternaCm: 30, chovendo: true);
        var atual = Fabrica.Leitura(distanciaInternaCm: 8, chovendo: true);

        var resultado = _servico.Diagnosticar(_boca, atual, new[] { anterior });

        Assert.Equal(TipoDiagnostico.NivelCritico, resultado.Tipo);
    }

    [Fact]
    public void Diagnosticar_SemChuvaComOcupacaoAlta_NaoGeraNivelCritico()
    {
        var atual = Fabrica.Leitura(distanciaInternaCm: 8, chovendo: false);

        var resultado = _servico.Diagnosticar(_boca, atual, Array.Empty<Leitura>());

        Assert.NotEqual(TipoDiagnostico.NivelCritico, resultado.Tipo);
        Assert.False(resultado.GeraAlerta);
    }

    [Fact]
    public void CalcularNivelCm_SensorMedindoAlemDoFundo_NaoRetornaNivelNegativo()
    {
        var leitura = Fabrica.Leitura(distanciaInternaCm: 95);

        Assert.Equal(0, _servico.CalcularNivelCm(_boca, leitura));
    }
}
