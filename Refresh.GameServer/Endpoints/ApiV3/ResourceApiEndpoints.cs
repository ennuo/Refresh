using AttribDoc.Attributes;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Core.Storage;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Http;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Database;
using Refresh.GameServer.Endpoints.ApiV3.ApiTypes;
using Refresh.GameServer.Endpoints.ApiV3.ApiTypes.Errors;
using Refresh.GameServer.Endpoints.ApiV3.DataTypes.Response;
using Refresh.GameServer.Importing;
using Refresh.GameServer.Time;
using Refresh.GameServer.Types.Assets;
using Refresh.GameServer.Types.UserData;
using Refresh.GameServer.Verification;

namespace Refresh.GameServer.Endpoints.ApiV3;

public class ResourceApiEndpoints : EndpointGroup
{
    private const string HashMissingErrorWhen = "The hash is missing or null";
    private static readonly ApiValidationError HashMissingError = new(HashMissingErrorWhen);
    private static readonly Response HashMissingErrorResponse = HashMissingError;
    
    private const string HashInvalidErrorWhen = "The hash is invalid (should be SHA1 hash)";
    private static readonly ApiValidationError HashInvalidError = new(HashInvalidErrorWhen);
    private static readonly Response HashInvalidErrorResponse = HashInvalidError;

    private const string CouldNotGetAssetErrorWhen = "An error occurred while retrieving the asset from the data store";
    private static readonly ApiInternalError CouldNotGetAssetError = new(CouldNotGetAssetErrorWhen);
    private static readonly Response CouldNotGetAssetErrorResponse = CouldNotGetAssetError;
    
    private const string CouldNotGetAssetDatabaseErrorWhen = "An error occurred while retrieving the asset from the database";
    private static readonly ApiInternalError CouldNotGetAssetDatabaseError = new(CouldNotGetAssetDatabaseErrorWhen);
    private static readonly Response CouldNotGetAssetDatabaseErrorResponse = CouldNotGetAssetDatabaseError;
    
    [ApiV3Endpoint("assets/{hash}/download"), Authentication(false)]
    [ClientCacheResponse(31556952)] // 1 year, we don't expect the data to change
    [DocSummary("Downloads the raw data for an asset hash. Sent as application/octet-stream")]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    [DocError(typeof(ApiInternalError), CouldNotGetAssetErrorWhen)]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    public Response DownloadGameAsset(RequestContext context, IDataStore dataStore,
        [DocSummary("The SHA1 hash of the asset")] string hash)
    {
        bool isPspAsset = hash.StartsWith("psp/");

        string realHash = isPspAsset ? hash[4..] : hash;

        if (!CommonPatterns.Sha1Regex().IsMatch(realHash)) return HashInvalidError;
        if (string.IsNullOrWhiteSpace(realHash)) return HashMissingErrorResponse;
        if (!dataStore.ExistsInStore(hash)) return ApiNotFoundError.Instance;

        bool gotData = dataStore.TryGetDataFromStore(hash, out byte[]? data);
        if (data == null || !gotData) return CouldNotGetAssetErrorResponse;

        return new Response(data, ContentType.BinaryData);
    }

    [ApiV3Endpoint("assets/psp/{hash}/download"), Authentication(false)]
    [ClientCacheResponse(31556952)] // 1 year, we don't expect the data to change
    [DocSummary("Downloads the raw data for a PSP asset hash. Sent as application/octet-stream")]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    [DocError(typeof(ApiInternalError), CouldNotGetAssetErrorWhen)]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    public Response DownloadPspGameAsset(RequestContext context, IDataStore dataStore,
        [DocSummary("The SHA1 hash of the asset")] string hash) => DownloadGameAsset(context, dataStore, $"psp/{hash}");
    
    [ApiV3Endpoint("assets/{hash}/image", ContentType.Png), Authentication(false)]
    [ClientCacheResponse(9204111)] // 1 week, data may or may not change
    [DocSummary("Downloads any game texture (if it can be converted) as a PNG. Sent as image/png")]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    [DocError(typeof(ApiInternalError), CouldNotGetAssetErrorWhen)]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    public Response DownloadGameAssetAsImage(RequestContext context, IDataStore dataStore, GameDatabaseContext database,
        [DocSummary("The SHA1 hash of the asset")] string hash)
    {
        bool isPspAsset = hash.StartsWith("psp/");

        string realHash = isPspAsset ? hash[4..] : hash;
        
        if (!CommonPatterns.Sha1Regex().IsMatch(realHash)) return HashInvalidError;
        if (string.IsNullOrWhiteSpace(realHash)) return HashMissingErrorResponse;
        if (!dataStore.ExistsInStore(hash)) return ApiNotFoundError.Instance;

        if (!dataStore.ExistsInStore("png/" + realHash))
        {
            GameAsset? asset = database.GetAssetFromHash(realHash);
            if (asset == null) return CouldNotGetAssetDatabaseErrorResponse;
            
            ImageImporter.ImportAsset(asset, dataStore);
        }

        bool gotData = dataStore.TryGetDataFromStore("png/" + realHash, out byte[]? data);
        if (data == null || !gotData) return CouldNotGetAssetErrorResponse;

        return new Response(data, ContentType.Png);
    }

    [ApiV3Endpoint("assets/psp/{hash}/image", ContentType.Png), Authentication(false)]
    [ClientCacheResponse(9204111)] // 1 week, data may or may not change
    [DocSummary("Downloads any PSP game texture (if it can be converted) as a PNG. Sent as image/png")]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    [DocError(typeof(ApiInternalError), CouldNotGetAssetErrorWhen)]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    public Response DownloadPspGameAssetAsImage(RequestContext context, IDataStore dataStore, GameDatabaseContext database,
        [DocSummary("The SHA1 hash of the asset")] string hash) => this.DownloadGameAssetAsImage(context, dataStore, database, $"psp/{hash}");

    [ApiV3Endpoint("assets/{hash}"), Authentication(false)]
    [DocSummary("Gets information from the database about a particular hash. Includes user who uploaded, dependencies, timestamps, etc.")]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    public ApiResponse<ApiGameAssetResponse> GetAssetInfo(RequestContext context, GameDatabaseContext database,
        [DocSummary("The SHA1 hash of the asset")] string hash)
    {
        bool isPspAsset = hash.StartsWith("psp/");

        string realHash = isPspAsset ? hash[4..] : hash;

        if (!CommonPatterns.Sha1Regex().IsMatch(realHash)) return HashInvalidError;
        if (string.IsNullOrWhiteSpace(realHash)) return HashMissingError;

        GameAsset? asset = database.GetAssetFromHash(realHash);
        if (asset == null) return ApiNotFoundError.Instance;

        return ApiGameAssetResponse.FromOld(asset);
    }

    [ApiV3Endpoint("assets/psp/{hash}"), Authentication(false)]
    [DocSummary("Gets information from the database about a particular PSP hash. Includes user who uploaded, dependencies, timestamps, etc.")]
    [DocError(typeof(ApiValidationError), HashMissingErrorWhen)]
    [DocError(typeof(ApiNotFoundError), "The asset could not be found")]
    public ApiResponse<ApiGameAssetResponse> GetPspAssetInfo(RequestContext context, GameDatabaseContext database,
        [DocSummary("The SHA1 hash of the asset")] string hash) => GetAssetInfo(context, database, $"psp/{hash}");

    [ApiV3Endpoint("assets/{hash}", HttpMethods.Post)]
    [DocSummary("Uploads an image (PNG/JPEG) asset")]
    [DocError(typeof(ApiValidationError), HashInvalidErrorWhen)]
    public ApiResponse<ApiGameAssetResponse> UploadImageAsset(RequestContext context, GameDatabaseContext database, IDataStore dataStore, AssetImporter importer,
        [DocSummary("The SHA1 hash of the asset")] string hash,
        byte[] body, GameUser user)
    {
        if (!CommonPatterns.Sha1Regex().IsMatch(hash)) return HashInvalidError;

        if (dataStore.ExistsInStore(hash))
        {
            GameAsset? existingAsset = database.GetAssetFromHash(hash);
            if (existingAsset == null)
                return new ApiInternalError("The hash was present on the server, but could not be found in the database.");

            return ApiGameAssetResponse.FromOld(existingAsset);
        }

        if (body.Length > 1_048_576 * 2)
        {
            return new ApiValidationError($"The asset must be under 2MB. Your file was {body.Length:N0} bytes.");
        }

        GameAsset? gameAsset = importer.ReadAndVerifyAsset(hash, body, TokenPlatform.Website);
        if (gameAsset == null)
            return new ApiValidationError("The asset could not be read.");

        if (gameAsset.AssetType is not GameAssetType.Jpeg and not GameAssetType.Png)
        {
            return new ApiValidationError("You must provide a PNG or a JPEG file.");
        }
        
        if (!dataStore.WriteToStore(hash, body))
            return new ApiInternalError("The asset could not be written to the server's data store.");
        
        gameAsset.OriginalUploader = user;
        database.AddAssetToDatabase(gameAsset);

        return ApiGameAssetResponse.FromOld(gameAsset);
    }
}