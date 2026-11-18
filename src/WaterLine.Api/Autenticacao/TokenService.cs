using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Autenticacao;

public sealed record TokenGerado(string Token, DateTime ExpiraEm);

public interface ITokenService
{
    TokenGerado Gerar(Agente agente);
}

public sealed class TokenService : ITokenService
{
    public const string ClaimFuncao = "funcao";

    private readonly JwtOptions _opcoes;
    private readonly TimeProvider _relogio;

    public TokenService(JwtOptions opcoes, TimeProvider relogio)
    {
        _opcoes = opcoes;
        _relogio = relogio;
    }

    public TokenGerado Gerar(Agente agente)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddHours(_opcoes.ExpiracaoHoras);

        var descricao = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Audiencia,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, agente.Id.ToString()),
                new Claim(ClaimFuncao, agente.Funcao)
            }),
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(CriarChave(_opcoes), SecurityAlgorithms.HmacSha256)
        };

        return new TokenGerado(new JsonWebTokenHandler().CreateToken(descricao), expiraEm);
    }

    public static SymmetricSecurityKey CriarChave(JwtOptions opcoes) => new(Encoding.UTF8.GetBytes(opcoes.Chave));
}
