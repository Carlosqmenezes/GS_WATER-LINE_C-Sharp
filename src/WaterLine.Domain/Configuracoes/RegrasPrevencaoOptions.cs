using System.ComponentModel.DataAnnotations;

namespace WaterLine.Domain.Configuracoes;

/// <summary>Parâmetros da camada de prevenção (seção "Regras:Prevencao" do appsettings.json).</summary>
public sealed class RegrasPrevencaoOptions
{
    public const string Secao = "Regras:Prevencao";

    /// <summary>Quanto a prioridade dos chamados ativos sobe quando há previsão de chuva forte.</summary>
    [Range(1, 4)]
    public int IncrementoPrioridade { get; set; } = 1;

    /// <summary>Janela, em dias, usada para dizer que uma boca de lobo tem histórico de entupimento.</summary>
    [Range(1, 365)]
    public int DiasHistoricoEntupimento { get; set; } = 30;
}
