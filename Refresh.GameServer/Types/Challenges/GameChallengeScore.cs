using MongoDB.Bson;
using Realms;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Types.Challenges;

#nullable disable

public partial class GameChallengeScore : IRealmObject
{
    [PrimaryKey] public ObjectId ScoreId { get; set; } = ObjectId.GenerateNewId();

    public string Ghost { get; set; }
    public GameChallenge Challenge { get; set; }

    public GameUser Player { get; set; }
    // 32 bit value, 32 bit time
    public long Score { get; set; }
}