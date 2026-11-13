using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;
using WaterLine.Domain.Geo;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Entidades;

/// <summary>Herança, polimorfismo e privacidade dos agentes; cálculo de Haversine.</summary>
public class AgenteTests
{
    [Fact]
    public void Polimorfismo_CadaTipoDeAgenteInformaOsChamadosQueAtende()
    {
        Agente gari = new Gari("Gari Teste", "GAR-T001");
        Agente equipe = new EquipeGaleria("Equipe Teste", "GAL-T001");

        Assert.True(gari.PodeAtender(TipoChamado.Preventivo));
        Assert.False(gari.PodeAtender(TipoChamado.Urgente));

        Assert.True(equipe.PodeAtender(TipoChamado.Manutencao));
        Assert.True(equipe.PodeAtender(TipoChamado.Urgente));
        Assert.False(equipe.PodeAtender(TipoChamado.Preventivo));
    }

    [Fact]
    public void SairDeServico_ApagaALocalizacao()
    {
        var gari = Fabrica.GariEmServico(1);

        gari.SairDeServico();

        Assert.False(gari.EmServico);
        Assert.Null(gari.Latitude);
        Assert.Null(gari.Longitude);
        Assert.False(gari.TemLocalizacao);
    }

    [Fact]
    public void AtualizarLocalizacao_ForaDeServico_LancaRegraNegocio()
    {
        var gari = new Gari("Gari Teste", "GAR-T001");

        Assert.Throws<RegraNegocioException>(() => gari.AtualizarLocalizacao(-23.55, -46.64));
    }

    [Fact]
    public void ToString_NaoExpoeMatriculaNemLocalizacao()
    {
        var gari = Fabrica.GariEmServico(7);

        var texto = gari.ToString();

        Assert.DoesNotContain(gari.Matricula, texto);
        Assert.DoesNotContain(gari.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), texto);
        Assert.Equal("Gari #7", texto);
    }

    [Fact]
    public void Haversine_UmMilesimoDeGrauDeLatitude_DaCercaDe111Metros()
    {
        var distancia = Haversine.DistanciaEmMetros(-23.550, -46.640, -23.551, -46.640);

        Assert.InRange(distancia, 110, 112);
    }

    [Fact]
    public void Haversine_MesmoPonto_DaZero()
    {
        Assert.Equal(0, Haversine.DistanciaEmMetros(-23.55, -46.64, -23.55, -46.64), precision: 6);
    }
}
