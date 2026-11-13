using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Entidades;

/// <summary>Ciclo de vida do chamado: aceite e conclusão com fotos e confirmação do sensor.</summary>
public class ChamadoTests
{
    private const string FotoAntes = "chamados/1/antes.jpg";
    private const string FotoDepois = "chamados/1/depois.jpg";

    [Fact]
    public void Concluir_ComAsDuasFotosEEntradaLivre_ConcluiOChamado()
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.ChamadoAceito(1, gari);

        chamado.Concluir(gari.Id, FotoAntes, FotoDepois, entradaLivreConfirmada: true, Fabrica.Agora.AddMinutes(15));

        Assert.Equal(StatusChamado.Concluido, chamado.Status);
        Assert.Equal(FotoAntes, chamado.FotoAntes);
        Assert.Equal(FotoDepois, chamado.FotoDepois);
        Assert.Equal(Fabrica.Agora.AddMinutes(15), chamado.ConcluidoEm);
    }

    [Theory]
    [InlineData(null, FotoDepois)]
    [InlineData(FotoAntes, null)]
    [InlineData(null, null)]
    [InlineData("", FotoDepois)]
    [InlineData(FotoAntes, "   ")]
    public void Concluir_SemAlgumaDasFotos_LancaRegraNegocioENaoConclui(string? fotoAntes, string? fotoDepois)
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.ChamadoAceito(1, gari);

        var erro = Assert.Throws<RegraNegocioException>(
            () => chamado.Concluir(gari.Id, fotoAntes, fotoDepois, entradaLivreConfirmada: true, Fabrica.Agora));

        Assert.Contains("foto", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StatusChamado.Aceito, chamado.Status);
        Assert.Null(chamado.ConcluidoEm);
    }

    [Fact]
    public void Concluir_SensorAindaMostrandoEntradaBloqueada_LancaRegraNegocio()
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.ChamadoAceito(1, gari);

        Assert.Throws<RegraNegocioException>(
            () => chamado.Concluir(gari.Id, FotoAntes, FotoDepois, entradaLivreConfirmada: false, Fabrica.Agora));

        Assert.Equal(StatusChamado.Aceito, chamado.Status);
    }

    [Fact]
    public void Concluir_ChamadoQueNaoFoiAceito_LancaConflito()
    {
        var chamado = Fabrica.Chamado(1);

        Assert.Throws<ConflitoException>(
            () => chamado.Concluir(10, FotoAntes, FotoDepois, entradaLivreConfirmada: true, Fabrica.Agora));
    }

    [Fact]
    public void Concluir_PorOutroAgente_LancaRegraNegocio()
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.ChamadoAceito(1, gari);

        Assert.Throws<RegraNegocioException>(
            () => chamado.Concluir(99, FotoAntes, FotoDepois, entradaLivreConfirmada: true, Fabrica.Agora));
    }

    [Fact]
    public void Aceitar_GariEmServico_MudaParaAceito()
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);

        chamado.Aceitar(gari, Fabrica.Agora);

        Assert.Equal(StatusChamado.Aceito, chamado.Status);
        Assert.Equal(gari.Id, chamado.AgenteId);
        Assert.Equal(Fabrica.Agora, chamado.AceitoEm);
    }

    [Fact]
    public void Aceitar_AgenteDeOutroTipo_LancaRegraNegocio()
    {
        var equipe = Fabrica.EquipeEmServico(20);
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);

        Assert.Throws<RegraNegocioException>(() => chamado.Aceitar(equipe, Fabrica.Agora));
    }

    [Fact]
    public void Aceitar_AgenteForaDeServico_LancaRegraNegocio()
    {
        var gari = Fabrica.GariEmServico(10);
        gari.SairDeServico();
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);

        Assert.Throws<RegraNegocioException>(() => chamado.Aceitar(gari, Fabrica.Agora));
    }

    [Fact]
    public void Aceitar_ChamadoDesignadoAOutroAgente_LancaConflito()
    {
        var designado = Fabrica.GariEmServico(10);
        var outro = Fabrica.GariEmServico(11);
        var chamado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        chamado.AtribuirAgente(designado);

        Assert.Throws<ConflitoException>(() => chamado.Aceitar(outro, Fabrica.Agora));
    }

    [Fact]
    public void Aceitar_ChamadoJaAceito_LancaConflito()
    {
        var gari = Fabrica.GariEmServico(10);
        var chamado = Fabrica.ChamadoAceito(1, gari);

        Assert.Throws<ConflitoException>(() => chamado.Aceitar(gari, Fabrica.Agora));
    }

    [Fact]
    public void ElevarPrioridade_NaoPassaDoTeto()
    {
        var chamado = Fabrica.Chamado(1, TipoChamado.Urgente); // prioridade padrão 3

        chamado.ElevarPrioridade(1);
        chamado.ElevarPrioridade(1);
        chamado.ElevarPrioridade(1);

        Assert.Equal(Domain.Entidades.Chamado.PrioridadeMaxima, chamado.Prioridade);
    }
}
