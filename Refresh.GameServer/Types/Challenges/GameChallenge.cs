using Realms;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Types.Challenges;

[JsonObject(MemberSerialization.OptOut)]
public partial class GameChallenge : IRealmObject, ISequentialId
{
    [PrimaryKey] public int ChallengeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public GameUser? Author { get; set; }
    
    public string LevelType { get; set; }
    public int LevelId { get; set; }
    
    public float Score { get; set; }

    public int StartCheckpoint { get; set; }
    public int EndCheckpoint { get; set; }

    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    
    public IList<GameChallengeCriterion> Criteria { get; }
    
    [JsonIgnore] public int SequentialId
    {
        set => this.ChallengeId = value;
    }
}