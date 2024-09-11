using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Protocols.Http;
using Refresh.GameServer.Importing;
using Refresh.GameServer.Types.Roles;
using Refresh.GameServer.Types.Telemetry;
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
        MemoryBitStream reader = new(body);
        
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

        TelemetryHeader header = new();
        ushort revision = reader.ReadUInt16();

        header.Revision = revision;
        header.HashedPlayerId = reader.ReadUInt32();
        
        if (revision >= 0x12)
            reader.ReadExactly(header.LevelHash);
        
        if (revision >= 0x13)
        {
            header.SlotType = reader.ReadUInt32();
            header.SlotNumber = reader.ReadUInt32();
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
        
        // Many messages have frame timestamps prepended after a certain revision.
        bool hasTimestamps = revision >= 0x1d;
        
        // Keep reading telemetry events until we reach the end of the stream
        // When the data is no longer bit aligned, we might read too much data,
        // so just check that we have at least 8 bits left.
        while (reader.BitsRemaining >= 8)
        {
            // Telemetry events don't include size fields, so just have to
            // parse everything, it's kind of rough.
            TelemetryEvent evt = (TelemetryEvent)reader.ReadUInt32();

            // These two events don't send any data with them, want to avoid any additional
            // conditionals for the frame timestamps added in LBP2
            if (evt is TelemetryEvent.MoveTutorial or TelemetryEvent.MoveCalibration) continue;
            
            uint frame = hasTimestamps ? reader.ReadUInt32() : 0;
            
            context.Logger.LogDebug(BunkumCategory.Game, $"{evt} from {user.Username}");
            
            switch (evt)
            {
                case TelemetryEvent.Start:
                {
                    // This doesn't send any data in early versions of LBP1
                    if (revision >= 0xd)
                    {
                        InlinePhysicalAddress addr = new();
                        reader.ReadExactly(addr);
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has started sending telemetry data. MAC: {Convert.ToHexString(addr)}");
                    }
                    
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
                        
                        string costume = reader.ReadString(); // max size is 32 bytes
                        
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
                    TelemetryPosition pos = new()
                    {
                        X = reader.ReadSingle(),
                        Y = reader.ReadSingle(),
                        Layer = reader.ReadUInt32(),
                    };
                    
                    // They already added the frame to most telemetry messages,
                    // couldn't they have removed these duplicates?
                    // Seems to always be the same as the prior frame value.
                    if (revision >= 0x19)
                        pos.Frame = reader.ReadUInt32();
                    
                    context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} - {evt} - <{pos.X}, {pos.Y}, {pos.Layer}> @ {pos.Frame}");
                    
                    break;
                }

                case TelemetryEvent.GameMessage:
                {
                    if (revision >= 0x14)
                    {
                        TelemetryGameMessage msg = new()
                        {
                            // Probably important to note that the types get moved around depending on the version of the game,
                            // for example EGMT_ALERT in LBP2 is 19, while in LBP3, it's 20
                            Type = reader.ReadUInt32(),
                        };
                        
                        // Some removed value, no builds seem to have this revision,
                        // so it's probably not important to consider.
                        if (revision < 0x15) reader.ReadUInt32();
                        else msg.Key = reader.ReadUInt32();

                        // This message has a max size of 40 bytes including the null terminator.
                        msg.Message = reader.ReadString();
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has game message [type]={msg.Type}, [key]={msg.Key}, [text]={msg.Message}");
                    }
                    
                    break;
                }
                
                case TelemetryEvent.PoppetState:
                {
                    if (revision >= 0x14)
                    {
                        TelemetryPoppetState poppet = new()
                        {
                            Mode = reader.ReadUInt32(),
                            SubMode = reader.ReadUInt32(),
                        };

                        // Max size is 256 characters for whatever reason,
                        // might contain other data in certain sub modes?
                        if (revision >= 0x1d)
                            poppet.Player = reader.ReadString();
                        
                        context.Logger.LogDebug(BunkumCategory.Game, $"{user.Username} has poppet state [mode]={(PoppetMode)poppet.Mode}, [submode]={(PoppetSubMode)poppet.SubMode}, [player]={poppet.Player}");
                    }
                    
                    break;
                }
                
                case TelemetryEvent.PodComputerState:
                {
                    if (revision >= 0x14)
                    {
                        // This is just the name of the Pod Computer state returned from PodComputerState::GetName 
                        string state = reader.ReadString(); // Max string length is 512
                    
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

                case TelemetryEvent.UserExperienceMetrics:
                {
                    if (revision < 0x17) break;
                    
                    // These values are probably not accurate in terms of names,
                    // well they could be close, since it seems they're probably(?)
                    // the same as the LBP3 JSON versions, but who knows, it at least
                    // is the correct data size.
                    TelemetryUserExperienceMetrics metrics = new()
                    {
                        CurrentMspf = reader.ReadSingle(),
                        AverageMspf = reader.ReadSingle(),
                        HighMspf = reader.ReadSingle(),
                        PredictApplied = reader.ReadUInt32(),
                        PredictDesired = reader.ReadUInt32(),
                        IsHost = reader.ReadBit(),
                        IsCreate = reader.ReadBit(),
                        NumPlayers = reader.ReadUInt32(),
                        NumPs3s = reader.ReadUInt32(),
                        AverageRttHost = reader.ReadSingle(),
                        BandwidthUsage = reader.ReadSingle(),
                        WorstPing = reader.ReadSingle(),
                        WorstBandwidth = reader.ReadSingle(),
                        WorstPacketLoss = reader.ReadSingle(),
                        WorstPlayers = reader.ReadUInt32(),
                        HttpBandwidthUp = reader.ReadSingle(),
                        HttpBandwidthDown = reader.ReadSingle(),
                        Frame = reader.ReadUInt32(),
                        LastMgjFrame = reader.ReadUInt32(),
                    };
                    
                    for (int i = 0; i < metrics.NumPlayers; ++i)
                    {
                        TelemetryPlayerNetStats stats = new()
                        {
                            Frame = reader.ReadUInt32(),
                            Player = reader.ReadUInt32(),
                            IsLocal = reader.ReadBit(),
                            AvailableBandwidth = reader.ReadUInt32(),
                            AvailableRnpBandwidth = reader.ReadUInt32(),
                            AvailableGameBandwidth = reader.ReadSingle(),
                            RecentTotalBandwidthUsed = reader.ReadUInt32(),
                            TimeBetweenSends = reader.ReadSingle(),
                        };
                        
                        metrics.PlayerNetStats.Add(stats);
                    }
                    
                    break;
                }

                case TelemetryEvent.InventoryItemClick:
                {
                    if (revision < 0x19) break;

                    TelemetryInventoryItem item = new()
                    {
                        Action = reader.ReadUInt32(),
                        Type = reader.ReadUInt32(),
                    };

                    uint numGuids = reader.ReadUInt32();
                    for (int i = 0; i < numGuids; ++i)
                        item.Guids.Add(reader.ReadUInt32());
                    uint numHashes = reader.ReadUInt32();
                    for (int i = 0; i < numHashes; ++i)
                    {
                        InlineHash hash = new();
                        reader.ReadExactly(hash);
                        item.Hashes.Add(hash);
                    }
                    
                    break;
                }

                case TelemetryEvent.OpenPsid:
                {
                    if (revision >= 0x19)
                    {
                        OpenPsid _ = new()
                        {
                            Low = reader.ReadUInt64(),
                            High = reader.ReadUInt64(),
                        };
                    }
                    
                    break;
                }

                case TelemetryEvent.Is50HzTv:
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
                    string state = reader.ReadString();
                    
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