using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Serialized;

[XmlRoot("challenge")]
[XmlType("challenges")]
public class SerializedChallengeList
{
    public SerializedChallengeList()
    {}

    public SerializedChallengeList(IEnumerable<SerializedChallenge> items)
    {
        this.Items = items.ToList();
    }
    
    [XmlElement("challenge")]
    public List<SerializedChallenge> Items { get; set; } = new();
}