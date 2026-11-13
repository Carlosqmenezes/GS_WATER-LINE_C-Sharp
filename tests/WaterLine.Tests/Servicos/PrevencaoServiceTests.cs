using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Servicos;

/// <summary>Camada de prevenção: previsão de chuva forte.</summary>
public class PrevencaoServiceTests
{
    private readonly PrevencaoService _servico = new(Fabrica.RegrasPrevencao());

    [Fact]
    public void ChuvaForte_ElevaPrioridadeDosChamadosAtivosECriaPreventivos()
    {
        var previsao = new PrevisaoChuva(Fabrica.Agora, horizonteHoras: 3, probabilidade: 0.82, chuvaForte: true);
        var manutencao = Fabrica.Chamado(1, TipoChamado.Manutencao, bocaDeLoboId: 1); // prioridade 1
        var bocaComHistorico = Fabrica.Boca(id: 2);

        var resultado = _servico.AplicarPrevisao(previsao, new[] { manutencao }, new[] { bocaComHistorico }, Fabrica.Agora);

        Assert.Equal(2, manutencao.Prioridade);
        Assert.Single(resultado.ChamadosPriorizados);
        var criado = Assert.Single(resultado.ChamadosCriados);
        Assert.Equal(TipoChamado.Preventivo, criado.Tipo);
        Assert.Equal(bocaComHistorico.Id, criado.BocaDeLoboId);
        Assert.Equal(Chamado.PrioridadePadrao(TipoChamado.Preventivo) + 1, criado.Prioridade);
    }

    [Fact]
    public void ChuvaForte_BocaQueJaTemPreventivoAtivo_NaoDuplica()
    {
        var previsao = new PrevisaoChuva(Fabrica.Agora, 3, 0.9, chuvaForte: true);
        var boca = Fabrica.Boca(id: 1);
        var preventivoAtivo = Fabrica.Chamado(1, TipoChamado.Preventivo, bocaDeLoboId: 1);

        var resultado = _servico.AplicarPrevisao(previsao, new[] { preventivoAtivo }, new[] { boca }, Fabrica.Agora);

        Assert.Empty(resultado.ChamadosCriados);
    }

    [Fact]
    public void ChuvaForte_BocaInativa_NaoRecebePreventivo()
    {
        var previsao = new PrevisaoChuva(Fabrica.Agora, 3, 0.9, chuvaForte: true);
        var boca = Fabrica.Boca(id: 1);
        boca.Desativar();

        var resultado = _servico.AplicarPrevisao(previsao, Array.Empty<Chamado>(), new[] { boca }, Fabrica.Agora);

        Assert.Empty(resultado.ChamadosCriados);
    }

    [Fact]
    public void SemChuvaForte_NaoAlteraNada()
    {
        var previsao = new PrevisaoChuva(Fabrica.Agora, 3, 0.2, chuvaForte: false);
        var chamado = Fabrica.Chamado(1, TipoChamado.Manutencao);

        var resultado = _servico.AplicarPrevisao(previsao, new[] { chamado }, new[] { Fabrica.Boca(id: 2) }, Fabrica.Agora);

        Assert.Equal(1, chamado.Prioridade);
        Assert.Empty(resultado.ChamadosPriorizados);
        Assert.Empty(resultado.ChamadosCriados);
    }
}
