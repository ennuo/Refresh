using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Serialized;

[XmlRoot("challenge-attempt")]
[XmlType("challenge-attempt")]
public class SerializedChallengeAttempt
{
    [XmlElement("score")] public long Score { get; set; }
    [XmlElement("ghost")] public string Ghost { get; set; } = string.Empty;
}