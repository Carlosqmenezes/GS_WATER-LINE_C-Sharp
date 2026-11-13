using System.ComponentModel.DataAnnotations;

namespace WaterLine.Domain.Configuracoes;

/// <summary>Parâmetros do despacho (seção "Regras:Despacho" do appsettings.json).</summary>
public sealed class RegrasDespachoOptions
{
    public const string Secao = "Regras:Despacho";

    /// <summary>Distância máxima, em metros, entre o agente e a boca de lobo para despachar.</summary>
    [Range(10, 10_000)]
    public double RaioMetros { get; set; } = 300;
}
