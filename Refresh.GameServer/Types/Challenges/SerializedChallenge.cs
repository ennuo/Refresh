using System.Xml.Serialization;
using Refresh.GameServer.Types.Photos;

namespace Refresh.GameServer.Types.Challenges;

[XmlRoot("challenge")]
[XmlType("challenge")]
public class SerializedChallenge
{
    [XmlElement("id")] public int ChallengeId { get; set; }
    [XmlElement("slot")] public SerializedPhotoLevel Level { get; set; }
    [XmlElement("name")] public string Name { get; set; } = string.Empty;
    [XmlElement("author")] public string Author { get; set; } = string.Empty;
    [XmlElement("score")] public float Score { get; set; }
    [XmlElement("start-checkpoint")] public int StartCheckpoint { get; set; }
    [XmlElement("end-checkpoint")] public int EndCheckpoint { get; set; }
    [XmlElement("published")] public long Published { get; set; }
    [XmlElement("expires")] public long Expiration { get; set; }
    [XmlArray("criteria")] public List<SerializedCriterion> Criteria { get; set; }

    public static SerializedChallenge FromGameChallenge(GameChallenge challenge)
    {
        SerializedChallenge newChallenge = new()
        {
            ChallengeId = challenge.ChallengeId,
            Level = new SerializedPhotoLevel
            {
                LevelId = challenge.LevelId,
                Type = challenge.LevelType,
            },
            Name = challenge.Name,
            Author = challenge.Author?.Username ?? string.Empty,
            Score = challenge.Score,
            StartCheckpoint = challenge.StartCheckpoint,
            EndCheckpoint = challenge.EndCheckpoint,
            Published = challenge.PublishedAt.ToUnixTimeMilliseconds(),
            Expiration = challenge.ExpiresAt.ToUnixTimeMilliseconds(),
            Criteria = new List<SerializedCriterion>(challenge.Criteria.Count)
        };

        foreach (GameChallengeCriterion criterion in challenge.Criteria)
        {
            SerializedCriterion newCriterion = new()
            {
                Name = criterion.Name,
                Metric = (int)criterion.Metric,
            };

            newChallenge.Criteria.Add(newCriterion);
        }

        return newChallenge;
    }
}