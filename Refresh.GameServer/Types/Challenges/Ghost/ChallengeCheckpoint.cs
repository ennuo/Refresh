using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Ghost;

[XmlRoot("checkpoint")]
[XmlType("checkpoint")]
public class ChallengeCheckpoint
{
    [XmlAttribute("uid")] public int Uid { get; set; }
    [XmlAttribute("time")] public long Time { get; set; }
    [XmlAttribute("metric")] public List<ChallengeCheckpointMetric> Metrics { get; set; } = new();
}