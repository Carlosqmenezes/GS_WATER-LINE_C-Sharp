namespace WaterLine.Domain.Geo;

/// <summary>
/// Cálculos geográficos simples sobre a superfície da Terra.
/// </summary>
public static class Haversine
{
    /// <summary>Raio médio da Terra, em metros.</summary>
    public const double RaioTerraMetros = 6_371_000;

    /// <summary>Metros por grau de latitude (aproximação usada para a caixa de busca).</summary>
    private const double MetrosPorGrauLatitude = 111_320;

    /// <summary>Distância em metros entre dois pontos, pela fórmula de Haversine.</summary>
    public static double DistanciaEmMetros(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var dLat = EmRadianos(latitude2 - latitude1);
        var dLng = EmRadianos(longitude2 - longitude1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(EmRadianos(latitude1)) * Math.Cos(EmRadianos(latitude2))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return RaioTerraMetros * c;
    }

    /// <summary>
    /// Retângulo que contém o círculo de raio informado. Serve como pré-filtro barato no banco;
    /// a distância exata é conferida depois com Haversine.
    /// </summary>
    public static CaixaDeBusca CaixaAoRedor(double latitude, double longitude, double raioMetros)
    {
        var deltaLat = raioMetros / MetrosPorGrauLatitude;
        var cosLat = Math.Max(Math.Cos(EmRadianos(latitude)), 0.000001);
        var deltaLng = raioMetros / (MetrosPorGrauLatitude * cosLat);

        return new CaixaDeBusca(latitude - deltaLat, latitude + deltaLat, longitude - deltaLng, longitude + deltaLng);
    }

    private static double EmRadianos(double graus) => graus * Math.PI / 180.0;
}

/// <summary>Retângulo de latitude e longitude usado para pré-filtrar consultas.</summary>
public readonly record struct CaixaDeBusca(double LatitudeMinima, double LatitudeMaxima, double LongitudeMinima, double LongitudeMaxima);
