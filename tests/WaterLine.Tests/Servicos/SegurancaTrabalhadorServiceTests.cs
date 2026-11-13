using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Servicos;

/// <summary>Cancelamento dos chamados de gari quando começa a chover.</summary>
public class SegurancaTrabalhadorServiceTests
{
    private readonly SegurancaTrabalhadorService _servico = new();

    [Fact]
    public void LeituraComChuva_CancelaChamadosDeGariAbertosEAceitosDaquelaBoca()
    {
        var gari = Fabrica.GariEmServico(10);
        var aberto = Fabrica.Chamado(1, TipoChamado.Preventivo, bocaDeLoboId: 1);
        var aceito = Fabrica.ChamadoAceito(2, gari, TipoChamado.Preventivo, bocaDeLoboId: 1);
        var leitura = Fabrica.Leitura(bocaDeLoboId: 1, chovendo: true);

        var cancelados = _servico.CancelarChamadosDeGariPorChuva(leitura, new[] { aberto, aceito });

        Assert.Equal(2, cancelados.Count);
        Assert.Equal(StatusChamado.Cancelado, aberto.Status);
        Assert.Equal(StatusChamado.Cancelado, aceito.Status);
    }

    [Fact]
    public void LeituraSemChuva_NaoCancelaNada()
    {
        var aberto = Fabrica.Chamado(1, TipoChamado.Preventivo);
        var leitura = Fabrica.Leitura(chovendo: false);

        var cancelados = _servico.CancelarChamadosDeGariPorChuva(leitura, new[] { aberto });

        Assert.Empty(cancelados);
        Assert.Equal(StatusChamado.Aberto, aberto.Status);
    }

    [Theory]
    [InlineData(TipoChamado.Manutencao)]
    [InlineData(TipoChamado.Urgente)]
    public void LeituraComChuva_NaoCancelaChamadosDaEquipeDeGaleria(TipoChamado tipo)
    {
        var chamadoGaleria = Fabrica.Chamado(1, tipo);
        var leitura = Fabrica.Leitura(chovendo: true);

        var cancelados = _servico.CancelarChamadosDeGariPorChuva(leitura, new[] { chamadoGaleria });

        Assert.Empty(cancelados);
        Assert.Equal(StatusChamado.Aberto, chamadoGaleria.Status);
    }

    [Fact]
    public void LeituraComChuva_NaoCancelaChamadoDeOutraBocaDeLobo()
    {
        var outraBoca = Fabrica.Chamado(1, TipoChamado.Preventivo, bocaDeLoboId: 2);
        var leitura = Fabrica.Leitura(bocaDeLoboId: 1, chovendo: true);

        var cancelados = _servico.CancelarChamadosDeGariPorChuva(leitura, new[] { outraBoca });

        Assert.Empty(cancelados);
        Assert.Equal(StatusChamado.Aberto, outraBoca.Status);
    }

    [Fact]
    public void LeituraComChuva_NaoMexeEmChamadoJaCancelado()
    {
        var jaCancelado = Fabrica.Chamado(1, TipoChamado.Preventivo);
        jaCancelado.Cancelar();
        var leitura = Fabrica.Leitura(chovendo: true);

        var cancelados = _servico.CancelarChamadosDeGariPorChuva(leitura, new[] { jaCancelado });

        Assert.Empty(cancelados);
    }
}
