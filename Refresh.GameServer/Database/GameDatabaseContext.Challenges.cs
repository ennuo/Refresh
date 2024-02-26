using JetBrains.Annotations;
using Refresh.GameServer.Types.Challenges;
using Refresh.GameServer.Types.Challenges.Serialized;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Database;

public partial class GameDatabaseContext // Challenges
{
    public void SubmitChallengeScore(SerializedChallengeAttempt attempt, GameUser user, GameChallenge challenge)
    {
        GameChallengeScore? score = this._realm.All<GameChallengeScore>()
            .FirstOrDefault(s => s.Player == user);

        if (score != null)
        {
            this._realm.Write(() =>
            {
                score.Ghost = attempt.Ghost;
                score.Score = attempt.Score;
            });

            return;
        }
        
        score = new GameChallengeScore
        {
            Ghost = attempt.Ghost,
            Player = user,
            Score = attempt.Score,
            Challenge = challenge,
        };

        this._realm.Write(() =>
        {
            this._realm.Add(score);
            if (attempt.Score == 0) challenge.Score = attempt.Score;
        });
    }
    
    public GameChallenge UploadChallenge(SerializedChallenge challenge, GameUser publisher)
    {
        GameChallenge newChallenge = new()
        {
            Name = challenge.Name,
            Author = publisher,
            Score = challenge.Score,
            StartCheckpoint = challenge.StartCheckpoint,
            EndCheckpoint = challenge.EndCheckpoint,
            ExpiresAt = this._time.Now.AddDays(challenge.Expiration),
            LevelType = challenge.Level?.Type ?? "",
            LevelId = challenge.Level?.LevelId?? 0,
            PublishedAt = this._time.Now,
        };

        foreach (SerializedCriterion criterion in challenge.Criteria)
        {
            GameChallengeCriterion newCriterion = new()
            {
                Metric = (ChallengeMetric) criterion.Metric,
                Value = criterion.Value,
            };

            newChallenge.Criteria.Add(newCriterion);

        }
        
        this.AddSequentialObject(newChallenge);
        
        return newChallenge;
    }
    
    public void RemoveChallenge(GameChallenge challenge)
    {
        this._realm.Write(() =>
        {
            this._realm.Remove(challenge);
        });
    }
    
    [Pure]
    public GameChallenge? GetChallengeById(int id) =>
        this._realm.All<GameChallenge>().FirstOrDefault(c => c.ChallengeId == id);
    
    [Pure]
    public DatabaseList<GameChallenge> GetChallengesByUser(GameUser user) =>
        new(this._realm.All<GameChallenge>().Where(p => p.Author == user)
            .OrderBy(p => p.PublishedAt));
}