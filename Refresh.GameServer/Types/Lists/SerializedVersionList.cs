using System.Xml.Serialization;
using Refresh.GameServer.Types.Comments;
using Refresh.GameServer.Types.Levels;

namespace Refresh.GameServer.Types.Lists;

[XmlRoot("versions")]
[XmlType("versions")]
public class SerializedVersionList
{
    public SerializedVersionList() {}
    
    public SerializedVersionList(IEnumerable<GameLevelVersion> versions)
    {
        this.Items = versions.ToList();
    }

    [XmlElement("version")]
    public List<GameLevelVersion> Items { get; set; } = new();
}