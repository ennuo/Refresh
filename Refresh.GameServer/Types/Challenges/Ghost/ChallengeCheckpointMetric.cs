using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Ghost;

[XmlType("metric")]
[XmlRoot("metric")]
public class ChallengeCheckpointMetric
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlText] public long Value { get; set; }
}