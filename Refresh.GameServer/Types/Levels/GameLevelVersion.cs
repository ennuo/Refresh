using System.Xml.Serialization;
using Realms;
using Refresh.GameServer.Database;

namespace Refresh.GameServer.Types.Levels;

#nullable disable

[XmlRoot("version")]
[XmlType("version")]
public partial class GameLevelVersion : IRealmObject, ISequentialId
{
    [PrimaryKey] [XmlElement("id")] public int SequentialId { get; set; }
    [XmlIgnore] public GameLevel Level { get; set; } = null!;
    [XmlElement("timestamp")] public long Timestamp { get; set; }
    public string LevelAsset { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
}