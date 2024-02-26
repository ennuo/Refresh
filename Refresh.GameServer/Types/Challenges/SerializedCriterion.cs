using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges;

[XmlRoot("criterion")]
[XmlType("criterion")]
public class SerializedCriterion
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlText] public int Metric { get; set; }
}