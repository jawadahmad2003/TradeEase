using System.Security.Cryptography;
using System.Text;

namespace TradeEase.Services;

public static class TradeProductHash
{
    public static string Compute(string hsCode, string description, string country = "PK") =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{hsCode}|{description}|{country}")))[..16];

    public static string ComputeForRoute(string hsCode, string description, TradeRouteContext route) =>
        Compute(hsCode, description, $"{route.ShipmentType.ToUpperInvariant()}|{route.OriginCountry}|{route.DestinationCountry}");
}
