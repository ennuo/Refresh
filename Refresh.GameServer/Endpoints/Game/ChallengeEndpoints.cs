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
        
        return new SerializedChallengeList { Items = challenges.ToList() };
    }

    [GameEndpoint("challenge", HttpMethods.Post, ContentType.Xml)]
    public SerializedChallenge UploadChallenge(RequestContext context, SerializedChallenge body,
        GameDatabaseContext database, GameUser user, IDataStore dataStore)
    {
        return SerializedChallenge.FromGameChallenge(database.UploadChallenge(body, user));
    }
}