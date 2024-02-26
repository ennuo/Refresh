using System.Xml.Serialization;
using Refresh.GameServer.Types.Levels;

namespace Refresh.GameServer.Types.Challenges;

[XmlRoot("challenge")]
[XmlType("challenges")]
public class SerializedChallengeList
{
    [XmlElement("challenge")]
    public List<SerializedChallenge> Items { get; set; } = new();
}