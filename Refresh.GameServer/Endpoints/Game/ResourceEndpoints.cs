using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Bunkum.CustomHttpListener.Parsing;
using Bunkum.HttpServer;
using Bunkum.HttpServer.Endpoints;
using Bunkum.HttpServer.Responses;
using Bunkum.HttpServer.Storage;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Configuration;
using Refresh.GameServer.Database;
using Refresh.GameServer.Importing;
using Refresh.GameServer.Types.Assets;
using Refresh.GameServer.Types.Lists;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Endpoints.Game;

public class ResourceEndpoints : EndpointGroup
{
    //NOTE: type does nothing here, but it's sent by LBP so we have to accept it
    [GameEndpoint("upload/{hash}/{type}", Method.Post)]
    [GameEndpoint("upload/{hash}", Method.Post)]
    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    public Response UploadAsset(RequestContext context, string hash, string type, byte[] body, IDataStore dataStore,
        GameDatabaseContext database, GameUser user, AssetImporter importer, GameServerConfig config)
    {
        if (dataStore.ExistsInStore(hash))
            return Conflict;

        GameAsset? gameAsset = importer.ReadAndVerifyAsset(hash, body);
        if (gameAsset == null)
            return BadRequest;

        // for example, if asset safety level is Dangerous (2) and maximum is configured as Safe (0), return 401
        // if asset safety is Safe (0), and maximum is configured as Safe (0), proceed 
        if (gameAsset.SafetyLevel > config.MaximumAssetSafetyLevel)
        {
            context.Logger.LogWarning(BunkumContext.UserContent, $"{gameAsset.AssetType} {hash} is above configured safety limit " +
                                                                 $"({gameAsset.SafetyLevel} > {config.MaximumAssetSafetyLevel})");
            return Unauthorized;
        }

        if (!dataStore.WriteToStore(hash, body))
            return InternalServerError;

        gameAsset.OriginalUploader = user;
        database.AddAssetToDatabase(gameAsset);

        return OK;
    }

    [GameEndpoint("r/{hash}")]
    public Response GetResource(RequestContext context, string hash, IDataStore dataStore, GameDatabaseContext database, Token token)
    {
        if (!dataStore.ExistsInStore(hash))
            return NotFound;
        
        // Vita only accepts PNG
        if (token.TokenGame == TokenGame.LittleBigPlanetVita)
        {
            GameAsset? asset = database.GetAssetFromHash(hash);
            if (asset == null)
            {
                context.Logger.LogWarning(BunkumContext.UserContent, "Failed to convert to PNG because asset was missing in database");
                return InternalServerError;
            }

            if (asset.AssetType is GameAssetType.Jpeg or GameAssetType.Texture)
            {
                if (!dataStore.ExistsInStore("png/" + hash))
                {
                    ImageImporter.ImportAsset(asset, dataStore);
                }
            
                bool gotData = dataStore.TryGetDataFromStore("png/" + hash, out byte[]? pngData);
                
                // ReSharper disable once InvertIf
                if (!gotData || pngData == null)
                {
                    context.Logger.LogWarning(BunkumContext.UserContent, $"Couldn't get data: gotData: {gotData}, pngData: {pngData}");
                    return InternalServerError;
                }

                return new Response(pngData, ContentType.Png);
            }
        }

        if (!dataStore.TryGetDataFromStore(hash, out byte[]? data))
            return InternalServerError;

        Debug.Assert(data != null);
        return new Response(data, ContentType.BinaryData);
    }

    [GameEndpoint("showNotUploaded", Method.Post, ContentType.Xml)]
    [GameEndpoint("filterResources", Method.Post, ContentType.Xml)]
    public SerializedResourceList GetAssetsMissingFromStore(RequestContext context, SerializedResourceList body, IDataStore dataStore) 
        => new(body.Items.Where(r => !dataStore.ExistsInStore(r)));
}