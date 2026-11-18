using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dtos;

public sealed record LoginRequest
{
    [Required(ErrorMessage = MensagensValidacao.Obrigatorio)]
    [StringLength(Agente.TamanhoMaximoMatricula, ErrorMessage = "A matrícula aceita no máximo {1} caracteres.")]
    public string? Matricula { get; init; }

    /// <summary>Senha numérica do agente.</summary>
    [Required(ErrorMessage = MensagensValidacao.Obrigatorio)]
    [StringLength(64, ErrorMessage = "A senha aceita no máximo {1} caracteres.")]
    public string? Senha { get; init; }
}

public sealed record LoginResponse(string Token, DateTime ExpiraEm, AgenteServicoResponse Agente);
