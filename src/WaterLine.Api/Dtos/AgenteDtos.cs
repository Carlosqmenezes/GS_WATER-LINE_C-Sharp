using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dtos;

/// <summary>Entrada ou saída de serviço do agente.</summary>
public sealed record AlterarServicoRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe se o agente está entrando (true) ou saindo (false) de serviço.")]
    public bool? EmServico { get; init; }

    [Range(-90.0, 90.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? Latitude { get; init; }

    [Range(-180.0, 180.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? Longitude { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmServico == true && (Latitude is null || Longitude is null))
        {
            yield return new ValidationResult(
                "Latitude e longitude são obrigatórias para entrar em serviço.",
                new[] { nameof(Latitude), nameof(Longitude) });
        }
    }
}

public sealed record AgenteServicoResponse(int Id, string Nome, string Funcao, bool EmServico)
{
    public static AgenteServicoResponse De(Agente agente) => new(agente.Id, agente.Nome, agente.Funcao, agente.EmServico);
}
