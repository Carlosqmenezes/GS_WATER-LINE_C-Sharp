using WaterLine.Domain.Comum;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Chamado de limpeza ou manutenção de uma boca de lobo.
/// As transições de status só acontecem pelos métodos desta classe, que aplicam as regras.
/// </summary>
public class Chamado : EntidadeBase
{
    public const int PrioridadeMinima = 1;
    public const int PrioridadeMaxima = 5;
    public const int TamanhoMaximoCaminhoFoto = 300;

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected Chamado()
    {
    }

    public Chamado(int bocaDeLoboId, TipoChamado tipo, DateTime abertoEm, int? prioridade = null)
    {
        BocaDeLoboId = bocaDeLoboId;
        Tipo = tipo;
        Status = StatusChamado.Aberto;
        Prioridade = Math.Clamp(prioridade ?? PrioridadePadrao(tipo), PrioridadeMinima, PrioridadeMaxima);
        AbertoEm = Validacao.EmUtc(abertoEm);
    }

    public int BocaDeLoboId { get; private set; }

    public BocaDeLobo? BocaDeLobo { get; private set; }

    /// <summary>Agente designado. Fica vazio enquanto ninguém do tipo certo estiver no raio.</summary>
    public int? AgenteId { get; private set; }

    public Agente? Agente { get; private set; }

    public TipoChamado Tipo { get; private set; }

    public StatusChamado Status { get; private set; }

    /// <summary>De 1 (baixa) a 5 (máxima). Quanto maior, mais cedo o chamado aparece para o agente.</summary>
    public int Prioridade { get; private set; }

    public DateTime AbertoEm { get; private set; }

    public DateTime? AceitoEm { get; private set; }

    public DateTime? ConcluidoEm { get; private set; }

    /// <summary>Caminho relativo da foto tirada antes da limpeza.</summary>
    public string? FotoAntes { get; private set; }

    /// <summary>Caminho relativo da foto tirada depois da limpeza.</summary>
    public string? FotoDepois { get; private set; }

    /// <summary>Um chamado está ativo enquanto está aberto ou aceito.</summary>
    public bool EstaAtivo => Status is StatusChamado.Aberto or StatusChamado.Aceito;

    /// <summary>Indica se o chamado é trabalho de gari (e, portanto, deve ser cancelado se chover).</summary>
    public bool EhDeGari => Gari.EhTipoDeGari(Tipo);

    /// <summary>Prioridade inicial de cada tipo de chamado.</summary>
    public static int PrioridadePadrao(TipoChamado tipo) => tipo switch
    {
        TipoChamado.Manutencao => 1,
        TipoChamado.Preventivo => 2,
        TipoChamado.Urgente => 3,
        TipoChamado.Emergencia => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de chamado desconhecido.")
    };

    /// <summary>Designa o agente escolhido pelo despacho, sem aceitar o chamado por ele.</summary>
    public void AtribuirAgente(Agente agente)
    {
        ArgumentNullException.ThrowIfNull(agente);
        GarantirStatus(StatusChamado.Aberto, "atribuir agente");
        GarantirQuePodeAtender(agente);
        AgenteId = agente.Id;
    }

    /// <summary>O agente aceita o chamado e passa a ser o responsável por ele.</summary>
    public void Aceitar(Agente agente, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(agente);
        GarantirStatus(StatusChamado.Aberto, "aceitar");
        GarantirQuePodeAtender(agente);

        if (!agente.EmServico)
        {
            throw new RegraNegocioException("O agente precisa estar em serviço para aceitar um chamado.");
        }

        if (AgenteId.HasValue && AgenteId.Value != agente.Id)
        {
            throw new ConflitoException("Este chamado já foi designado a outro agente.");
        }

        AgenteId = agente.Id;
        Status = StatusChamado.Aceito;
        AceitoEm = Validacao.EmUtc(agora);
    }

    /// <summary>
    /// Verifica, sem alterar nada, se o chamado pode ser concluído.
    /// Permite à API validar antes de gravar as fotos em disco.
    /// </summary>
    public void ValidarConclusao(int agenteId, bool temFotoAntes, bool temFotoDepois, bool entradaLivreConfirmada)
    {
        GarantirStatus(StatusChamado.Aceito, "concluir");

        if (AgenteId != agenteId)
        {
            throw new RegraNegocioException("Somente o agente que aceitou o chamado pode concluí-lo.");
        }

        if (!temFotoAntes || !temFotoDepois)
        {
            throw new RegraNegocioException("O chamado só pode ser concluído com a foto de antes e a foto de depois.");
        }

        if (!entradaLivreConfirmada)
        {
            throw new RegraNegocioException(
                "A última leitura do sensor ainda não confirma a entrada livre. Aguarde uma nova leitura e tente de novo.");
        }
    }

    /// <summary>Conclui o chamado com as duas fotos e a confirmação do sensor.</summary>
    public void Concluir(int agenteId, string? fotoAntes, string? fotoDepois, bool entradaLivreConfirmada, DateTime agora)
    {
        ValidarConclusao(
            agenteId,
            !string.IsNullOrWhiteSpace(fotoAntes),
            !string.IsNullOrWhiteSpace(fotoDepois),
            entradaLivreConfirmada);

        FotoAntes = Validacao.TextoObrigatorio(fotoAntes, nameof(FotoAntes), TamanhoMaximoCaminhoFoto);
        FotoDepois = Validacao.TextoObrigatorio(fotoDepois, nameof(FotoDepois), TamanhoMaximoCaminhoFoto);
        Status = StatusChamado.Concluido;
        ConcluidoEm = Validacao.EmUtc(agora);
    }

    /// <summary>Cancela um chamado ativo (ex.: por segurança, quando começa a chover).</summary>
    public void Cancelar()
    {
        if (!EstaAtivo)
        {
            throw new ConflitoException($"Não é possível cancelar um chamado com status {Status}.");
        }

        Status = StatusChamado.Cancelado;
    }

    /// <summary>Eleva a prioridade sem passar do teto informado.</summary>
    public void ElevarPrioridade(int incremento, int prioridadeMaxima = PrioridadeMaxima)
    {
        if (incremento <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(incremento), incremento, "O incremento deve ser positivo.");
        }

        var teto = Math.Clamp(prioridadeMaxima, PrioridadeMinima, PrioridadeMaxima);
        Prioridade = Math.Min(Prioridade + incremento, teto);
    }

    private void GarantirStatus(StatusChamado esperado, string operacao)
    {
        if (Status != esperado)
        {
            throw new ConflitoException(
                $"Não é possível {operacao} um chamado com status {Status}. Status esperado: {esperado}.");
        }
    }

    private void GarantirQuePodeAtender(Agente agente)
    {
        if (!agente.PodeAtender(Tipo))
        {
            throw new RegraNegocioException($"{agente.Funcao} não atende chamados do tipo {Tipo}.");
        }
    }
}
