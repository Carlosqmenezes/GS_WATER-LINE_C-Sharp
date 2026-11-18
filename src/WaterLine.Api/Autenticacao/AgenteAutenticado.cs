using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using WaterLine.Api.Erros;

namespace WaterLine.Api.Autenticacao;

public static class AgenteAutenticado
{
    public static int ObterAgenteId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var agenteId)
            ? agenteId
            : throw new AcessoNegadoException("O token não identifica um agente.");
    }

    public static void GarantirMesmoAgente(this ClaimsPrincipal usuario, int agenteId)
    {
        if (usuario.ObterAgenteId() != agenteId)
        {
            throw new AcessoNegadoException("O login usado não pertence a este agente.");
        }
    }
}
