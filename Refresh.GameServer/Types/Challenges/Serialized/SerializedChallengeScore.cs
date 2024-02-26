using System.Xml.Serialization;

namespace Refresh.GameServer.Types.Challenges.Serialized;

[XmlRoot("challenge-score")]
[XmlType("challenge-score")]
public class SerializedChallengeScore
{
    [XmlElement("ghost")] public string Ghost { get; set; } = string.Empty;
    [XmlElement("player")] public string Player { get; set; } = string.Empty;
    [XmlElement("rank")] public int Rank { get; set; }
    [XmlElement("score")] public long Score { get; set; }

    public static SerializedChallengeScore FromGameChallengeScore(GameChallengeScore score, int rank)
    {
        SerializedChallengeScore newScore = new()
        {
            Ghost = score.Ghost,
            Player = score.Player.Username,
            Rank = rank,
            Score = score.Score,
        };

        return newScore;
    }
}