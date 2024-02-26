using System.Collections;
using System.Xml.Serialization;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Endpoints.Debugging;
using Bunkum.Core.Responses;
using Bunkum.Core.Storage;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Http;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Database;
using Refresh.GameServer.Endpoints.Game.DataTypes.Response;
using Refresh.GameServer.Services;
using Refresh.GameServer.Types.Challenges;
using Refresh.GameServer.Types.Challenges.Serialized;
using Refresh.GameServer.Types.Lists;
using Refresh.GameServer.Types.Photos;
using Refresh.GameServer.Types.Roles;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Endpoints.Game;

public class ChallengeEndpoints : EndpointGroup
{
    [GameEndpoint("user/{username}/challenges", HttpMethods.Get, ContentType.Xml)]
    [MinimumRole(GameUserRole.Restricted)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeList? GetChallenges(RequestContext context, string username, GameDatabaseContext database, IDataStore dataStore)
    {
        GameUser? user = database.GetUserByUsername(username);
        if (user == null) return null;

        IEnumerable<SerializedChallenge> challenges = database.GetChallengesByUser(user).Items
            .Select(SerializedChallenge.FromGameChallenge);
        
        return new SerializedChallengeList(challenges);
    }

    [GameEndpoint("challenge", HttpMethods.Post, ContentType.Xml)]
    public SerializedChallenge UploadChallenge(RequestContext context, SerializedChallenge body,
        GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        return SerializedChallenge.FromGameChallenge(database.UploadChallenge(body, user));
    }
    
    // TODO: Clean up all this nonsense, shouldn't be repeated all the shared leaderboard code
    
    
    // Freaks out when it's not a successful status, or does it legitimately still want the entire leaderboard regardless?
    [GameEndpoint("challenge/{id}/scoreboard/{username}/friends", HttpMethods.Get, ContentType.Xml)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeScoreboard? GetFriendsChallengeScoreboard(RequestContext context, int id,
        string username, GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        GameChallenge? challenge = database.GetChallengeById(id);
        if (challenge == null) return null;
        
        IEnumerable<SerializedChallengeScore> scores = challenge.Scores
            .AsEnumerable()
            .OrderByDescending(c => c.Score)
            .Select((c, i) => SerializedChallengeScore.FromGameChallengeScore(c, i + 1))
            .ToList();
        
        return new SerializedChallengeScoreboard(scores);
    }
    
    [GameEndpoint("challenge/{id}/scoreboard/{username}", HttpMethods.Get, ContentType.Xml)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeScoreboard? GetPersonalChallengeScoreboard(RequestContext context, int id,
        string username, GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        GameChallenge? challenge = database.GetChallengeById(id);
        if (challenge == null) return null;
        
        IEnumerable<SerializedChallengeScore> scores = challenge.Scores
            .AsEnumerable()
            .OrderByDescending(c => c.Score)
            .Select((c, i) => SerializedChallengeScore.FromGameChallengeScore(c, i + 1))
            .ToList();
        
        return new SerializedChallengeScoreboard(scores);
    }
    
    // What even is "contextual"?
    [GameEndpoint("challenge/{id}/scoreboard//contextual", HttpMethods.Get, ContentType.Xml)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeScoreboard? GetContextualChallengeScoreboard(RequestContext context, int id,
        GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        GameChallenge? challenge = database.GetChallengeById(id);
        if (challenge == null) return null;
        
        IEnumerable<SerializedChallengeScore> scores = challenge.Scores
            .AsEnumerable()
            .OrderByDescending(c => c.Score)
            .Select((c, i) => SerializedChallengeScore.FromGameChallengeScore(c, i + 1))
            .ToList();
        
        return new SerializedChallengeScoreboard(scores);
    }

    [GameEndpoint("challenge/{id}/scoreboard", HttpMethods.Get, ContentType.Xml)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeScoreboard? GetChallengeScoreboard(RequestContext context, int id,
        GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        GameChallenge? challenge = database.GetChallengeById(id);
        if (challenge == null) return null;
        
        IEnumerable<SerializedChallengeScore> scores = challenge.Scores
            .AsEnumerable()
            .OrderByDescending(c => c.Score)
            .Select((c, i) => SerializedChallengeScore.FromGameChallengeScore(c, i + 1))
            .ToList();
        
        return new SerializedChallengeScoreboard(scores);
    }

    [GameEndpoint("challenge/{id}/scoreboard", HttpMethods.Post, ContentType.Xml)]
    [NullStatusCode(NotFound)]
    public SerializedChallengeScoreboard? UploadChallengeScore(RequestContext context, int id, SerializedChallengeAttempt body,
        GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        GameChallenge? challenge = database.GetChallengeById(id);
        if (challenge == null) return null;

        if (!dataStore.ExistsInStore(body.Ghost)) return null;
        database.SubmitChallengeScore(body, user, challenge);

        IEnumerable<SerializedChallengeScore> scores = challenge.Scores
            .AsEnumerable()
            .OrderByDescending(c => c.Score)
            .Select((c, i) => SerializedChallengeScore.FromGameChallengeScore(c, i + 1))
            .ToList();
        
        return new SerializedChallengeScoreboard(scores);
    }

    // TODO: Load/store developer challenges and scores, just using this to verify the schema
    [GameEndpoint("developer-challenges/scores")]
    public Response GetDeveloperChallengeScores(RequestContext context, GameDatabaseContext database, GameUser user,
        IDataStore dataStore)
    {
        string xml = "<developer_challenge_scores>";
        for (int i = 1; i < 5; ++i)
        {
            xml += $"<score developer_challenge_id={i}>gold</score>";
        }
        xml += "</developer_challenge_scores>";
        
        return new Response(xml, ContentType.Xml);
    }
    
}