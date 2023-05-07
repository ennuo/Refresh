using System.Net;
using Bunkum.CustomHttpListener.Parsing;
using Bunkum.HttpServer;
using Bunkum.HttpServer.Endpoints;
using Bunkum.HttpServer.Responses;
using Refresh.GameServer.Services;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Endpoints.Game;

public class MatchingEndpoints : EndpointGroup
{
    // [FindBestRoom,["Players":["VitaGamer128"],"Reservations":["0"],"NAT":[2],"Slots":[[5,0]],"Location":[0x17257bc9,0x17257bf2],"Language":1,"BuildVersion":289,"Search":"","RoomState":3]]
    [GameEndpoint("match", Method.Post, ContentType.Json)]
    public Response Match(RequestContext context, GameUser user, MatchService service, string body)
    {
        Console.WriteLine(body);
        (string method, string jsonBody) = MatchService.ExtractMethodAndBodyFromJson(body);
        Console.WriteLine(jsonBody);
        Console.WriteLine(method);

        return HttpStatusCode.Gone;
    }
}