namespace Refresh.GameServer.Types.Telemetry;

public class TelemetryUserExperienceMetrics
{
    public float CurrentMspf;
    public float AverageMspf;
    public float HighMspf;
    public uint PredictApplied;
    public uint PredictDesired;
    public bool IsHost;
    public bool IsCreate;
    public uint NumPlayers;
    public uint NumPs3s;
    public float AverageRttHost;
    public float BandwidthUsage;
    public float WorstPing;
    public float WorstBandwidth;
    public float WorstPacketLoss;
    public uint WorstPlayers;
    public float HttpBandwidthUp;
    public float HttpBandwidthDown;
    public uint Frame;
    public uint LastMgjFrame;
    public List<TelemetryPlayerNetStats> PlayerNetStats = [];
}