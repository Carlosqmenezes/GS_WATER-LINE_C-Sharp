using WaterLine.Domain.Comum;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Agente público que atende chamados. Classe abstrata: cada subclasse informa,
/// por polimorfismo, quais tipos de chamado consegue atender.
/// </summary>
/// <remarks>
/// Privacidade do trabalhador: a localização só existe enquanto o agente está em serviço
/// e é apagada quando ele sai. Nome, matrícula, senha e localização nunca aparecem em
/// <see cref="ToString"/> para não vazarem em logs.
/// </remarks>
public abstract class Agente : EntidadeBase
{
    public const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoMatricula = 20;
    public const int TamanhoMaximoSenhaHash = 256;

    private readonly List<Chamado> _chamados = new();

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected Agente()
    {
        Nome = string.Empty;
        Matricula = string.Empty;
        SenhaHash = string.Empty;
    }

    protected Agente(string nome, string matricula)
    {
        Nome = Validacao.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Matricula = Validacao.TextoObrigatorio(matricula, nameof(Matricula), TamanhoMaximoMatricula).ToUpperInvariant();
        SenhaHash = string.Empty;
        EmServico = false;
    }

    public string Nome { get; private set; }

    public string Matricula { get; private set; }

    /// <summary>
    /// Hash da senha de acesso ao app. A senha em si nunca é guardada: quem calcula o hash
    /// é a camada de API. Vazio enquanto o agente não tem senha (e, portanto, não consegue entrar).
    /// </summary>
    public string SenhaHash { get; private set; }

    /// <summary>Indica se o agente já tem senha definida.</summary>
    public bool TemSenha => SenhaHash.Length > 0;

    public bool EmServico { get; private set; }

    /// <summary>Preenchida só enquanto o agente está em serviço.</summary>
    public double? Latitude { get; private set; }

    /// <summary>Preenchida só enquanto o agente está em serviço.</summary>
    public double? Longitude { get; private set; }

    public IReadOnlyCollection<Chamado> Chamados => _chamados;

    /// <summary>Tipos de chamado que este agente pode atender (definido por cada subclasse).</summary>
    public abstract IReadOnlyCollection<TipoChamado> TiposDeChamadoAtendidos { get; }

    /// <summary>Nome da função, para exibição (ex.: "Gari").</summary>
    public abstract string Funcao { get; }

    /// <summary>Indica se o agente está em serviço e com localização conhecida.</summary>
    public bool TemLocalizacao => EmServico && Latitude.HasValue && Longitude.HasValue;

    public bool PodeAtender(TipoChamado tipo) => TiposDeChamadoAtendidos.Contains(tipo);

    /// <summary>Guarda o hash da senha já calculado. Nunca recebe a senha em texto puro.</summary>
    public void DefinirSenhaHash(string senhaHash)
    {
        SenhaHash = Validacao.TextoObrigatorio(senhaHash, nameof(SenhaHash), TamanhoMaximoSenhaHash);
    }

    public void EntrarEmServico(double latitude, double longitude)
    {
        Validacao.Coordenadas(latitude, longitude);
        EmServico = true;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void AtualizarLocalizacao(double latitude, double longitude)
    {
        if (!EmServico)
        {
            throw new RegraNegocioException("A localização só pode ser enviada enquanto o agente está em serviço.");
        }

        Validacao.Coordenadas(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Sai de serviço e apaga a última localização conhecida.</summary>
    public void SairDeServico()
    {
        EmServico = false;
        Latitude = null;
        Longitude = null;
    }

    /// <summary>Representação segura para logs: só a função e o Id.</summary>
    public override string ToString() => $"{Funcao} #{Id}";
}
