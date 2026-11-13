using WaterLine.Domain.Comum;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Leitura enviada pelo ESP32: distâncias medidas pelos dois sensores e o indicador de chuva.
/// </summary>
public class Leitura : EntidadeBase
{
    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected Leitura()
    {
    }

    public Leitura(
        int bocaDeLoboId,
        DateTime dataHora,
        double distanciaExternaCm,
        double distanciaInternaCm,
        bool chovendo)
    {
        BocaDeLoboId = bocaDeLoboId;
        DataHora = Validacao.EmUtc(dataHora);
        DistanciaExternaCm = Validacao.NaoNegativo(distanciaExternaCm, nameof(DistanciaExternaCm));
        DistanciaInternaCm = Validacao.NaoNegativo(distanciaInternaCm, nameof(DistanciaInternaCm));
        Chovendo = chovendo;
    }

    public int BocaDeLoboId { get; private set; }

    public BocaDeLobo? BocaDeLobo { get; private set; }

    /// <summary>Momento da medição, em UTC.</summary>
    public DateTime DataHora { get; private set; }

    /// <summary>
    /// Distância medida pelo sensor externo, na horizontal, atravessando a abertura.
    /// Quanto menor que a largura da abertura, mais lixo tampando a entrada.
    /// </summary>
    public double DistanciaExternaCm { get; private set; }

    /// <summary>
    /// Distância da tampa até a superfície (água ou sedimento), medida pelo sensor interno.
    /// Quanto menor, mais cheia está a caixa.
    /// </summary>
    public double DistanciaInternaCm { get; private set; }

    public bool Chovendo { get; private set; }
}
