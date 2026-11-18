using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WaterLine.Api.Autenticacao;
using WaterLine.Tests.Comum;

namespace WaterLine.Tests.Autenticacao;

/// <summary>Token JWT do login: conteúdo, validade e assinatura.</summary>
public class TokenServiceTests
{
    private static readonly JwtOptions Opcoes = new()
    {
        Chave = "chave-ficticia-usada-somente-nos-testes-0001",
        ExpiracaoHoras = 12
    };

    private static TokenService CriarServico(JwtOptions? opcoes = null) =>
        new(opcoes ?? Opcoes, new RelogioFixo(Fabrica.Agora));

    private static TokenValidationParameters Parametros(JwtOptions opcoes) => new()
    {
        ValidIssuer = opcoes.Emissor,
        ValidAudience = opcoes.Audiencia,
        IssuerSigningKey = TokenService.CriarChave(opcoes),
        // O relógio dos testes é fixo, então a validade é conferida à parte.
        ValidateLifetime = false
    };

    [Fact]
    public async Task Gerar_TokenAssinado_IdentificaOAgentePeloIdEPelaFuncao()
    {
        var gari = Fabrica.GariEmServico(7);

        var gerado = CriarServico().Gerar(gari);
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(gerado.Token, Parametros(Opcoes));

        Assert.True(resultado.IsValid);
        Assert.Equal("7", resultado.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("Gari", resultado.Claims[TokenService.ClaimFuncao]);
    }

    [Fact]
    public void Gerar_NaoColocaNomeNemMatriculaNoToken()
    {
        var gari = Fabrica.GariEmServico(7);

        var token = new JsonWebToken(CriarServico().Gerar(gari).Token);

        Assert.DoesNotContain(token.Claims, claim => claim.Value == gari.Nome || claim.Value == gari.Matricula);
    }

    [Fact]
    public void Gerar_ExpiraDepoisDasHorasConfiguradas()
    {
        var gerado = CriarServico().Gerar(Fabrica.GariEmServico(1));

        Assert.Equal(Fabrica.Agora.AddHours(12), gerado.ExpiraEm);
        Assert.Equal(Fabrica.Agora.AddHours(12), new JsonWebToken(gerado.Token).ValidTo);
    }

    [Fact]
    public async Task Validar_TokenAssinadoComOutraChave_ERecusado()
    {
        var outraChave = new JwtOptions { Chave = "outra-chave-ficticia-usada-nos-testes-0002" };
        var token = CriarServico(outraChave).Gerar(Fabrica.GariEmServico(1)).Token;

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parametros(Opcoes));

        Assert.False(resultado.IsValid);
    }

    /// <summary>Relógio parado num instante, para o teste não depender da hora da máquina.</summary>
    private sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset _agora;

        public RelogioFixo(DateTime agoraUtc)
        {
            _agora = new DateTimeOffset(agoraUtc, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _agora;
    }
}
