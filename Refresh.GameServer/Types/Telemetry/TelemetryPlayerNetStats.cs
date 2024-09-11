namespace Refresh.GameServer.Types.Telemetry;

public class TelemetryPlayerNetStats
{
    public uint Frame;
    public uint Player;
    public bool IsLocal;
    public uint AvailableBandwidth;
    public uint AvailableRnpBandwidth;
    public float AvailableGameBandwidth;
    public uint RecentTotalBandwidthUsed;
    public float TimeBetweenSends;
}