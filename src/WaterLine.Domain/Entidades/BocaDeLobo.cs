using WaterLine.Domain.Comum;
using WaterLine.Domain.Enums;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Boca de lobo monitorada por um ESP32 com sensor externo (entrada) e interno (fundo e água).
/// </summary>
public class BocaDeLobo : EntidadeBase
{
    public const int TamanhoMaximoCodigo = 20;
    public const int TamanhoMaximoEndereco = 200;

    private readonly List<Leitura> _leituras = new();
    private readonly List<Chamado> _chamados = new();
    private readonly List<Alerta> _alertas = new();

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected BocaDeLobo()
    {
        Codigo = string.Empty;
        Endereco = string.Empty;
    }

    public BocaDeLobo(
        string codigo,
        string endereco,
        double latitude,
        double longitude,
        double profundidadeCm,
        double larguraAberturaCm)
    {
        Codigo = string.Empty;
        Endereco = string.Empty;
        AtualizarDados(codigo, endereco, latitude, longitude, profundidadeCm, larguraAberturaCm);
        Status = TipoDiagnostico.Normal;
        Ativa = true;
    }

    /// <summary>Código de identificação usado no campo (ex.: BL-0001).</summary>
    public string Codigo { get; private set; }

    public string Endereco { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>Distância da tampa até o fundo da caixa, em centímetros.</summary>
    public double ProfundidadeCm { get; private set; }

    /// <summary>Largura da abertura atravessada pelo sensor externo, em centímetros.</summary>
    public double LarguraAberturaCm { get; private set; }

    /// <summary>Último diagnóstico calculado para esta boca de lobo.</summary>
    public TipoDiagnostico Status { get; private set; }

    /// <summary>Bocas de lobo inativas não recebem leituras nem chamados.</summary>
    public bool Ativa { get; private set; }

    public IReadOnlyCollection<Leitura> Leituras => _leituras;

    public IReadOnlyCollection<Chamado> Chamados => _chamados;

    public IReadOnlyCollection<Alerta> Alertas => _alertas;

    public void AtualizarDados(
        string codigo,
        string endereco,
        double latitude,
        double longitude,
        double profundidadeCm,
        double larguraAberturaCm)
    {
        Validacao.Coordenadas(latitude, longitude);

        Codigo = Validacao.TextoObrigatorio(codigo, nameof(Codigo), TamanhoMaximoCodigo).ToUpperInvariant();
        Endereco = Validacao.TextoObrigatorio(endereco, nameof(Endereco), TamanhoMaximoEndereco);
        Latitude = latitude;
        Longitude = longitude;
        ProfundidadeCm = Validacao.Positivo(profundidadeCm, nameof(ProfundidadeCm));
        LarguraAberturaCm = Validacao.Positivo(larguraAberturaCm, nameof(LarguraAberturaCm));
    }

    public void AtualizarStatus(TipoDiagnostico status) => Status = status;

    public void Ativar() => Ativa = true;

    public void Desativar() => Ativa = false;
}
