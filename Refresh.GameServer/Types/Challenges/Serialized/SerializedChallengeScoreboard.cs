using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Serialized;

[XmlRoot("challenge-scores")]
[XmlType("challenge-scores")]
public class SerializedChallengeScoreboard
{
    public SerializedChallengeScoreboard()
    {}

    public SerializedChallengeScoreboard(IEnumerable<SerializedChallengeScore> items)
    {
        this.Items = items.ToList();
    }
    
    [XmlElement("challenge-score")]
    public List<SerializedChallengeScore> Items { get; set; } = new();
}