using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Servicos;

/// <summary>Despacho por raio de 300 m, tipo de agente e ordenação por prioridade e distância.</summary>
public class DespachoServiceTests
{
    private readonly DespachoService _servico = new(Fabrica.RegrasDespacho(raioMetros: 300));
    private readonly BocaDeLobo _boca = Fabrica.Boca();

    [Fact]
    public void EscolherAgente_EscolheOGariMaisProximoDentroDoRaio()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var longe = Fabrica.GariEmServico(10, metrosAoNorte: 250);
        var perto = Fabrica.GariEmServico(11, metrosAoNorte: 80);

        var escolhido = _servico.EscolherAgente(chamado, _boca, new Agente[] { longe, perto });

        Assert.Same(perto, escolhido);
    }

    [Fact]
    public void EscolherAgente_GariForaDoRaioDe300Metros_NaoDespacha()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var foraDoRaio = Fabrica.GariEmServico(10, metrosAoNorte: 450);

        var escolhido = _servico.EscolherAgente(chamado, _boca, new Agente[] { foraDoRaio });

        Assert.Null(escolhido);
    }

    [Fact]
    public void EscolherAgente_IgnoraAgenteForaDeServico()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var gari = Fabrica.GariEmServico(10, metrosAoNorte: 50);
        gari.SairDeServico();

        var escolhido = _servico.EscolherAgente(chamado, _boca, new Agente[] { gari });

        Assert.Null(escolhido);
    }

    [Fact]
    public void EscolherAgente_ChamadoDeGari_IgnoraEquipeDeGaleriaMesmoMaisProxima()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var equipe = Fabrica.EquipeEmServico(20, metrosAoNorte: 10);
        var gari = Fabrica.GariEmServico(10, metrosAoNorte: 200);

        var escolhido = _servico.EscolherAgente(chamado, _boca, new Agente[] { equipe, gari });

        Assert.Same(gari, escolhido);
    }

    [Theory]
    [InlineData(TipoChamado.Manutencao)]
    [InlineData(TipoChamado.Urgente)]
    public void EscolherAgente_ChamadoDeGaleria_EscolheEquipeDeGaleria(TipoChamado tipo)
    {
        var chamado = Fabrica.Chamado(1, tipo);
        var gari = Fabrica.GariEmServico(10, metrosAoNorte: 10);
        var equipe = Fabrica.EquipeEmServico(20, metrosAoNorte: 150);

        var escolhido = _servico.EscolherAgente(chamado, _boca, new Agente[] { gari, equipe });

        Assert.Same(equipe, escolhido);
    }

    [Fact]
    public void Despachar_DesignaOsChamadosDeMaiorPrioridadePrimeiro()
    {
        var bocaPerto = Fabrica.Boca(id: 1);
        var chamadoBaixa = Fabrica.Chamado(1, TipoChamado.Preventivo, bocaDeLoboId: 1, prioridade: 2);
        var chamadoAlta = Fabrica.Chamado(2, TipoChamado.Preventivo, bocaDeLoboId: 1, prioridade: 4);
        var gari = Fabrica.GariEmServico(10, metrosAoNorte: 100);

        var designacoes = _servico.Despachar(
            new[] { new ChamadoComBoca(chamadoBaixa, bocaPerto), new ChamadoComBoca(chamadoAlta, bocaPerto) },
            new Agente[] { gari });

        Assert.Equal(new[] { 2, 1 }, designacoes.Select(d => d.Chamado.Id));
        Assert.All(designacoes, d => Assert.Equal(gari.Id, d.Chamado.AgenteId));
    }

    [Fact]
    public void Despachar_ChamadoSemAgenteNoRaio_FicaSemAgente()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var longe = Fabrica.GariEmServico(10, metrosAoNorte: 1000);

        var designacoes = _servico.Despachar(new[] { new ChamadoComBoca(chamado, _boca) }, new Agente[] { longe });

        Assert.Empty(designacoes);
        Assert.Null(chamado.AgenteId);
    }

    [Fact]
    public void ListarChamadosProximos_FiltraPeloRaioEOrdenaPorPrioridadeEDepoisDistancia()
    {
        var bocaA = Fabrica.Boca(id: 1, latitude: Fabrica.LatitudeBase + 0.0018); // ~200 m
        var bocaB = Fabrica.Boca(id: 2, latitude: Fabrica.LatitudeBase + 0.0009); // ~100 m
        var bocaC = Fabrica.Boca(id: 3, latitude: Fabrica.LatitudeBase + 0.0090); // ~1 km (fora)

        var prioridade3Longe = Fabrica.Chamado(1, bocaDeLoboId: 1, prioridade: 3);
        var prioridade3Perto = Fabrica.Chamado(2, bocaDeLoboId: 2, prioridade: 3);
        var prioridade5 = Fabrica.Chamado(3, bocaDeLoboId: 1, prioridade: 5);
        var foraDoRaio = Fabrica.Chamado(4, bocaDeLoboId: 3, prioridade: 5);

        var proximos = _servico.ListarChamadosProximos(
            Fabrica.LatitudeBase,
            Fabrica.LongitudeBase,
            new[]
            {
                new ChamadoComBoca(prioridade3Longe, bocaA),
                new ChamadoComBoca(prioridade3Perto, bocaB),
                new ChamadoComBoca(prioridade5, bocaA),
                new ChamadoComBoca(foraDoRaio, bocaC)
            });

        Assert.Equal(new[] { 3, 2, 1 }, proximos.Select(p => p.Chamado.Id));
        Assert.All(proximos, p => Assert.True(p.DistanciaMetros <= 300));
    }

    [Fact]
    public void ListarChamadosProximos_IgnoraChamadosQueNaoEstaoAbertos()
    {
        var gari = Fabrica.GariEmServico(10);
        var aceito = Fabrica.ChamadoAceito(1, gari);
        var cancelado = Fabrica.Chamado(2);
        cancelado.Cancelar();

        var proximos = _servico.ListarChamadosProximos(
            Fabrica.LatitudeBase,
            Fabrica.LongitudeBase,
            new[] { new ChamadoComBoca(aceito, _boca), new ChamadoComBoca(cancelado, _boca) });

        Assert.Empty(proximos);
    }

    [Fact]
    public void EstaNoRaio_RespeitaOLimiteConfigurado()
    {
        Assert.True(_servico.EstaNoRaio(Fabrica.GariEmServico(1, metrosAoNorte: 290), _boca));
        Assert.False(_servico.EstaNoRaio(Fabrica.GariEmServico(2, metrosAoNorte: 320), _boca));
    }
}
