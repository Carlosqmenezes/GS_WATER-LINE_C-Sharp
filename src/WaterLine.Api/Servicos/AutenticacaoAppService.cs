using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Autenticacao;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Api.Erros;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Servicos;

public interface IAutenticacaoAppService
{
    Task<LoginResponse> EntrarAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AutenticacaoAppService : IAutenticacaoAppService
{
    private readonly WaterLineDbContext _db;
    private readonly IPasswordHasher<Agente> _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AutenticacaoAppService> _logger;

    public AutenticacaoAppService(
        WaterLineDbContext db,
        IPasswordHasher<Agente> hasher,
        ITokenService tokens,
        ILogger<AutenticacaoAppService> logger)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<LoginResponse> EntrarAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var matricula = request.Matricula!.Trim().ToUpperInvariant();
        var agente = await _db.Agentes.FirstOrDefaultAsync(a => a.Matricula == matricula, cancellationToken);

        if (agente is null || !agente.TemSenha)
        {
            _hasher.HashPassword(null!, request.Senha!);
            throw Recusar();
        }

        var resultado = _hasher.VerifyHashedPassword(agente, agente.SenhaHash, request.Senha!);
        if (resultado == PasswordVerificationResult.Failed)
        {
            throw Recusar();
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            agente.DefinirSenhaHash(_hasher.HashPassword(agente, request.Senha!));
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Privacidade: nunca registrar a matrícula, só o id.
        _logger.LogInformation("Agente {AgenteId} entrou no app.", agente.Id);

        var token = _tokens.Gerar(agente);
        return new LoginResponse(token.Token, token.ExpiraEm, AgenteServicoResponse.De(agente));
    }

    private CredenciaisInvalidasException Recusar()
    {
        _logger.LogInformation("Tentativa de login recusada.");
        return new CredenciaisInvalidasException();
    }
}
