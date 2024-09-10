using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Http;
using Refresh.GameServer.Configuration;
using Refresh.GameServer.Database;
using Refresh.GameServer.Importing;
using Refresh.GameServer.Time;
using Refresh.GameServer.Types;
using Refresh.GameServer.Types.Challenges;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.Roles;
using Refresh.GameServer.Types.UserData;

namespace Refresh.GameServer.Endpoints.Game;

public class TelemetryEndpoints : EndpointGroup
{
    [GameEndpoint("t", HttpMethods.Post)]
    [MinimumRole(GameUserRole.Restricted)]
    public Response UploadBinaryTelemetry(RequestContext context, byte[] body, GameUser user)
    {
        if (body.Length > 8128) // 4032 in earlier versions, I guess
        {
            context.Logger.LogWarning(BunkumCategory.Game, $"User {user.Username} attempted to upload telemetry buffer above game's maximum size! This likely did not come from an official client.");
            return RequestEntityTooLarge;
        }
        
        // Probably wouldn't be handling all the parsing here normally,
        // but I'm only using this as a scratchpad really.
        MemoryStream ms = new(body);
        CompressedBinaryReaderBE reader = new(ms);
        
        // Common revisions
            // LBP1 01.21 is 0x2 (Start is 0x0 instead of 0x1 in this version?)
            // LBP1 Deploy is 0x3 (Deploy is both before/after LBP1, branches)
            // LBP1 01.30-Final is 0xd
            // LBP2 Pre-Alpha is 0xe
            // LBP2 Move Beta is 0x19
            // LBP2 Final is 0x1f
            // LBP2 Vita Final is 0x1e
            // LBP2 Hub is 0x1e
            // LBP3 Alpha is 0x1b
            
        // LBP1 only has telemetry messages up until E_TELEMETRY_EVENT_DLC_OWNED
            // Deploy only has up to E_TELEMETRY_EVENT_LEAVE_LEVEL, but why is anyone using deploy
        // LBP2 has telemetry messages up until E_TELEMETRY_DCDS_ACTION
        // LBP3 has whatever is after, I'm honestly not bothering to go through them right now.
        // LBP Vita has telemetry messages up until E_TELEMETRY_GAME_PROGRESSION
        
        ushort revision = reader.ReadUInt16();
        uint hashedPlayerId = reader.ReadUInt32();
        
        if (revision >= 0x12)
        {
            Span<byte> levelHash = stackalloc byte[20];
            ms.ReadExactly(levelHash);
        }

        if (revision >= 0x13)
        {
            uint slotType = reader.ReadUInt32();
            uint slotNumber = reader.ReadUInt32();
        }

        // All position messages have a CHash serialized before the
        // frame timestamp specifically between these two revisions and I don't
        // want to handle that case, all updated and beta builds currently in use
        // do not use these revisions, so I don't consider it a priority.
        if (revision is >= 0x10 and < 0x12)
            return BadRequest;
        
        // Between revisions 1 and 5, only the first 4 bytes of hashes were serialized
        // after these revisions, the full SHA1 is serialized.
        bool hasFullHash = revision >= 0x5;
        Span<byte> scratchPadHash = stackalloc byte[20];
        
        // Many messages have frame timestamps prepended after a certain revision.
        bool hasTimestamps = revision >= 0x1d;
        
        // Keep reading telemetry events until we reach the end of the stream
        while (ms.Position < ms.Length)
        {
            // Telemetry events don't include size fields, so just have to
            // parse everything, it's kind of rough.
            TelemetryEvent evt = (TelemetryEvent)reader.ReadUInt32();

            // These two events don't send any data with them, want to avoid any additional
            // conditionals for the frame timestamps added in LBP2
            if (evt is TelemetryEvent.MoveTutorial or TelemetryEvent.MoveCalibration) continue;
            
            uint frame = hasTimestamps ? reader.ReadUInt32() : 0;
            
            // context.Logger.LogDebug(BunkumCategory.Game, $"{evt} from {user.Username}");
            
            switch (evt)
            {
                case TelemetryEvent.Start:
                {
                    // This doesn't send any data in early versions of LBP1,
                    // after it just sends some 6 byte packet.
                    
                    // TODO: Figure out what this field is meant to represent at some point
                    if (revision >= 0xd)
                        ms.Position += 6;
                    
                    break;
                }
                case TelemetryEvent.TestInt:
                {
                    uint num = reader.ReadUInt32();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} send test integer of value '{num}'");
                    
                    break;
                }
                case TelemetryEvent.TestV2:
                {
                    // The v2 struct in LBP is 4 components for alignment reasons,
                    // but is often used with 3 components, wonder how that works.
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} send test vector of value <{x}, {y}, {z}>");
                    
                    break;
                }
                case TelemetryEvent.TestChar:
                {
                    byte c = reader.ReadByte();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} send test character of value <{c}>");
                    
                    break;
                }
                case TelemetryEvent.TestM44:
                {
                    // I don't feel like formatting the output for a testing value honestly.
                    for (int col = 0; col < 4; ++col)
                    for (int row = 0; row < 4; ++row)
                        reader.ReadSingle();

                    break;
                }
                case TelemetryEvent.CostumesWorn:
                {
                    uint count = reader.ReadUInt32();
                    for (int i = 0; i < count; ++i)
                    {
                        if (hasTimestamps)
                        {
                            uint frameWorn = reader.ReadUInt32();   
                        }
                        
                        string costume = reader.ReadTerminatedString(); // max size is 32 bytes
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has worn {costume}");
                    }
                    
                    break;
                }

                case TelemetryEvent.UpdatePosition:
                case TelemetryEvent.DeathPosition:
                case TelemetryEvent.SuicidePosition:
                case TelemetryEvent.RestartPosition:
                case TelemetryEvent.QuitPosition:
                case TelemetryEvent.SwitchToEasyPosition:
                case TelemetryEvent.OffScreenPosition:
                case TelemetryEvent.PhotoPosition:
                case TelemetryEvent.LostAllLivesPosition:
                case TelemetryEvent.StickerPosition:
                case TelemetryEvent.PadDisconnectPosition:
                case TelemetryEvent.AiDeathPosition:
                case TelemetryEvent.OffscreenDeathPosition:
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uint a = reader.ReadUInt32();
                    uint b = revision >= 0x19 ? reader.ReadUInt32() : 0x0;
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} - {evt} - <{x}, {y}> ({a}, {b})");
                    
                    break;
                }

                case TelemetryEvent.GameMessage:
                {
                    if (revision >= 0x14)
                    {
                        uint a = reader.ReadUInt32(); // type?

                        uint b = 0; // lams?
                        if (revision < 0x15) reader.ReadUInt32(); // Some removed field
                        else b = reader.ReadUInt32(); // Field that replaced it? Both are unsigned integers, but whatev

                        string message = reader.ReadTerminatedString(); // Max size is 40
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has game message [a]={a}, [b]={b}, [msg]={message}");
                    }
                    
                    break;
                }
                
                case TelemetryEvent.PoppetState:
                {
                    if (revision >= 0x14)
                    {
                        uint mode = reader.ReadUInt32();
                        uint subMode = reader.ReadUInt32();
                        string state = revision >= 0x1d ? reader.ReadTerminatedString() : string.Empty; // Max size is 256 characters
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has poppet state [mode]={mode}, [submode]={subMode}, [state]={state}");
                    }
                    
                    break;
                }
                
                case TelemetryEvent.PodComputerState:
                {
                    if (revision >= 0x14)
                    {
                        // This is just the name of the Pod Computer state returned from PodComputerState::GetName 
                        string state = reader.ReadTerminatedString(); // Max string length is 512
                    
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} visited pod computer state '{state}' at {frame}");   
                    }
                    
                    break;
                }
                case TelemetryEvent.ExpressionState:
                {
                    if (revision < 0x14) break;
                    
                    // 0 = HAPPY, 1 = SAD, 2 = ANGRY, 3 = SCARED, 4 = NEUTRAL
                    // The neutral message doesn't always seem to get sent?
                    // Although it might just be transitional issues.
                    uint expressionIndex = reader.ReadUInt32();
                    // Intensity of each expression, 0 means NEUTRAL,
                    uint expressionLevel = reader.ReadUInt32(); // happy
                    
                    // Not sure what this value is meant to be,
                    // it always seems to be 0.
                    int _ = reader.ReadInt32();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has expression data [index]={expressionIndex}, [level]={expressionLevel}");
                    
                    break;
                }

                case TelemetryEvent.InventoryItemClick:
                {
                    if (revision < 0x19) break;
                    
                    uint action = reader.ReadUInt32();
                    uint type = reader.ReadUInt32();
                    uint numGuids = reader.ReadUInt32();
                    for (int i = 0; i < numGuids; ++i)
                        reader.ReadUInt32();
                    uint numHashes = reader.ReadUInt32();
                    for (int i = 0; i < numHashes; ++i)
                        ms.ReadExactly(scratchPadHash);
                    
                    break;
                }

                case TelemetryEvent.OpenPsid:
                {
                    if (revision >= 0x19)
                    {
                        // High and low bits of the user's OpenPSID
                        reader.ReadUInt64();
                        reader.ReadUInt64();
                    }
                    
                    break;
                }

                case TelemetryEvent.Is50hzTv:
                case TelemetryEvent.IsStandardDefTv:
                case TelemetryEvent.UsingImportedLbp1Profile:
                {
                    bool _ = reader.ReadUInt32() != 0;
                    
                    break;
                }
                
                case TelemetryEvent.ImportProfile:
                {
                    if (revision >= 0x19)
                    {
                        reader.ReadUInt32();
                        reader.ReadUInt32();
                    }
                    
                    break;
                }
                
                case TelemetryEvent.ModalOverlayState:
                {
                    // This is just the name of the modal overlay state returned from ModalOverlayState::GetName
                    string state = reader.ReadTerminatedString();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} visited modal overlay state '{state}' at {frame}");
                    
                    break;
                }

                case TelemetryEvent.GameProgression:
                {
                    if (revision >= 0x1a)
                    {
                        uint a = reader.ReadUInt32();
                        uint b = reader.ReadUInt32();
                    }
                    
                    break;
                }

                case TelemetryEvent.MainPlayerCostume:
                {
                    uint numCostumePieces = reader.ReadUInt32();
                    for (int i = 0; i < numCostumePieces; ++i)
                        reader.ReadUInt32(); // Costume piece GUID

                    break;
                }
                    
                default:
                {
                    context.Logger.LogDebug(BunkumCategory.Game, $"Unsupported telemetry message type: {evt}");
                    
                    // Early return a 200 because why not
                    return OK;
                }
            }
        }
        
        return OK;
    }
}