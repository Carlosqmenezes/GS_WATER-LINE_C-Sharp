using System.ComponentModel.DataAnnotations;

namespace WaterLine.Api.Armazenamento;


public sealed class ArmazenamentoFotosOptions
{
    public const string Secao = "Armazenamento";

   
    [Required]
    public string PastaFotos { get; set; } = "App_Data/fotos";

   
    [Range(1024, 50 * 1024 * 1024)]
    public long TamanhoMaximoFotoBytes { get; set; } = 5 * 1024 * 1024;
}
