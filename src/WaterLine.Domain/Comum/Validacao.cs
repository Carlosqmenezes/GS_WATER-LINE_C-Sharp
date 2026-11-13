namespace WaterLine.Domain.Comum;

/// <summary>
/// Verificações reutilizadas pelos construtores e métodos das entidades.
/// Lançam <see cref="ArgumentException"/>, que a API converte em HTTP 400.
/// </summary>
internal static class Validacao
{
    public static string TextoObrigatorio(string? valor, string nomeCampo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException($"O campo {nomeCampo} é obrigatório.", nomeCampo);
        }

        var texto = valor.Trim();
        if (texto.Length > tamanhoMaximo)
        {
            throw new ArgumentException($"O campo {nomeCampo} aceita no máximo {tamanhoMaximo} caracteres.", nomeCampo);
        }

        return texto;
    }

    public static double Positivo(double valor, string nomeCampo)
    {
        if (!double.IsFinite(valor) || valor <= 0)
        {
            throw new ArgumentOutOfRangeException(nomeCampo, valor, $"O campo {nomeCampo} deve ser maior que zero.");
        }

        return valor;
    }

    public static double NaoNegativo(double valor, string nomeCampo)
    {
        if (!double.IsFinite(valor) || valor < 0)
        {
            throw new ArgumentOutOfRangeException(nomeCampo, valor, $"O campo {nomeCampo} não pode ser negativo.");
        }

        return valor;
    }

    public static void Coordenadas(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "A latitude deve estar entre -90 e 90.");
        }

        if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "A longitude deve estar entre -180 e 180.");
        }
    }

    /// <summary>
    /// Garante que toda data do domínio esteja em UTC (exigência do PostgreSQL para timestamptz).
    /// Datas sem fuso informado são tratadas como UTC.
    /// </summary>
    public static DateTime EmUtc(DateTime dataHora) => dataHora.Kind switch
    {
        DateTimeKind.Utc => dataHora,
        DateTimeKind.Local => dataHora.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dataHora, DateTimeKind.Utc)
    };
}
