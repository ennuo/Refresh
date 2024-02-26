using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Serialized;

[XmlRoot("criterion")]
[XmlType("criterion")]
public class SerializedCriterion
{
    [XmlAttribute("name")] public int Metric { get; set; }
    [XmlText] public long Value { get; set; }
}