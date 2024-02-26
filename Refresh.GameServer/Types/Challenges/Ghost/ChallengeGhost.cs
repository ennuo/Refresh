using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Ghost;

[XmlType("ghost")]
[XmlRoot("ghost")]
public class ChallengeGhost
{
    [XmlElement("checkpoint")] public List<ChallengeCheckpoint> Checkpoints { get; set; } = new();
    [XmlElement("ghost_frame")] public List<GhostFrame> Frames { get; set; } = new();
    
}