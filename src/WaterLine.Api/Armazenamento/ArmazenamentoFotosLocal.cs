namespace WaterLine.Api.Armazenamento;

/// <summary>
/// Guarda as fotos em disco, numa pasta por chamado, com nome gerado pelo servidor
/// (o nome original do arquivo é descartado para evitar caminhos maliciosos).
/// </summary>
public sealed class ArmazenamentoFotosLocal : IArmazenamentoFotos
{
    private static readonly IReadOnlyDictionary<string, string> ExtensaoPorTipo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly ArmazenamentoFotosOptions _opcoes;
    private readonly string _pastaRaiz;
    private readonly ILogger<ArmazenamentoFotosLocal> _logger;

    public ArmazenamentoFotosLocal(
        ArmazenamentoFotosOptions opcoes,
        IWebHostEnvironment ambiente,
        ILogger<ArmazenamentoFotosLocal> logger)
    {
        _opcoes = opcoes;
        _logger = logger;
        _pastaRaiz = Path.GetFullPath(Path.IsPathRooted(opcoes.PastaFotos)
            ? opcoes.PastaFotos
            : Path.Combine(ambiente.ContentRootPath, opcoes.PastaFotos));
    }

    public void Validar(IFormFile foto, string nomeCampo)
    {
        ArgumentNullException.ThrowIfNull(foto);

        if (foto.Length == 0)
        {
            throw new ArgumentException($"A foto em {nomeCampo} está vazia.", nomeCampo);
        }

        if (foto.Length > _opcoes.TamanhoMaximoFotoBytes)
        {
            throw new ArgumentException(
                $"A foto em {nomeCampo} passa do limite de {_opcoes.TamanhoMaximoFotoBytes / (1024 * 1024)} MB.",
                nomeCampo);
        }

        if (!ExtensaoPorTipo.ContainsKey(foto.ContentType))
        {
            throw new ArgumentException($"A foto em {nomeCampo} deve ser JPEG, PNG ou WebP.", nomeCampo);
        }
    }

    public async Task<string> SalvarAsync(int chamadoId, string etapa, IFormFile foto, CancellationToken cancellationToken)
    {
        Validar(foto, etapa);

        var pastaChamado = Path.Combine(_pastaRaiz, "chamados", chamadoId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(pastaChamado);

        var nomeArquivo = $"{etapa}-{Guid.NewGuid():N}{ExtensaoPorTipo[foto.ContentType]}";
        var caminhoCompleto = Path.Combine(pastaChamado, nomeArquivo);

        await using (var destino = new FileStream(caminhoCompleto, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await foto.CopyToAsync(destino, cancellationToken);
        }

        return Path.GetRelativePath(_pastaRaiz, caminhoCompleto).Replace('\\', '/');
    }

    public void Remover(string caminhoRelativo)
    {
        var caminhoCompleto = Path.GetFullPath(Path.Combine(_pastaRaiz, caminhoRelativo));

        // Defesa contra caminhos fora da pasta de fotos.
        if (!caminhoCompleto.StartsWith(_pastaRaiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            File.Delete(caminhoCompleto);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Não foi possível remover a foto {Caminho}.", caminhoRelativo);
        }
    }
}
