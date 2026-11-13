using System.ComponentModel.DataAnnotations;

namespace WaterLine.Api.Armazenamento;

/// <summary>Configuração do armazenamento das fotos dos chamados (seção "Armazenamento").</summary>
public sealed class ArmazenamentoFotosOptions
{
    public const string Secao = "Armazenamento";

    /// <summary>Pasta das fotos. Caminho relativo é resolvido a partir da raiz do projeto da API.</summary>
    [Required]
    public string PastaFotos { get; set; } = "App_Data/fotos";

    /// <summary>Tamanho máximo de cada foto, em bytes (padrão: 5 MB).</summary>
    [Range(1024, 50 * 1024 * 1024)]
    public long TamanhoMaximoFotoBytes { get; set; } = 5 * 1024 * 1024;
}
