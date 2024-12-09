using System.Numerics;

namespace Circles.Common;

public static class Demurrage
{
    private const decimal Gamma = 0.9998013320085989574306481700129226782902039065082930593676448873m;
    private const long DemurrageWindow = 86400;
    public const long InflationDayZero = 1675209600;

    public static BigInteger ApplyDemurrage(long inflationDayZero, long timestamp, BigInteger value)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long dayLastInteraction = CrcDay(inflationDayZero, timestamp);
        long dayNow = CrcDay(inflationDayZero, now);

        var demurragedCircles = value * BigRational.Pow(Gamma, dayNow - dayLastInteraction);
        return (BigInteger)demurragedCircles;
    }

    private static long CrcDay(long inflationDayZero, long timestamp)
    {
        return (timestamp - inflationDayZero) / DemurrageWindow;
    }
}