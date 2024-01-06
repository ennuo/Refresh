using System.Xml.Serialization;
using Realms;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types.Comments;
using Refresh.GameServer.Types.UserData;
using Refresh.GameServer.Types.Levels.SkillRewards;
using Refresh.GameServer.Types.Relations;
using Refresh.GameServer.Types.Reviews;
using Refresh.GameServer.Types.UserData.Leaderboard;
using Refresh.GameServer.Workers;

namespace Refresh.GameServer.Types.Levels;

[JsonObject(MemberSerialization.OptIn)]
public partial class GameLevel : IRealmObject, ISequentialId
{
    [PrimaryKey] public int LevelId { get; set; }

    [Indexed(IndexType.FullText)]
    public string Title { get; set; } = "";
    public string IconHash { get; set; } = "0";
    [Indexed(IndexType.FullText)]
    public string Description { get; set; } = "";
    public GameLocation Location { get; set; } = GameLocation.Zero;

    public string RootResource { get; set; } = string.Empty;

    public long PublishDate { get; set; } // unix seconds
    public long UpdateDate { get; set; }
    
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public bool EnforceMinMaxPlayers { get; set; }
    
    public bool SameScreenGame { get; set; }
    public bool TeamPicked { get; set; }
    
    /// <summary>
    /// The GUID of the background, this seems to only be used by LBP PSP
    /// </summary>
    public string? BackgroundGuid { get; set; }
    
    public TokenGame GameVersion 
    {
        get => (TokenGame)this._GameVersion;
        set => this._GameVersion = (int)value;
    }
    
    // ReSharper disable once InconsistentNaming
    internal int _GameVersion { get; set; }
    
    public GameLevelType LevelType
    {
        get => (GameLevelType)this._LevelType;
        set => this._LevelType = (int)value;
    }

    // ReSharper disable once InconsistentNaming
    internal int _LevelType { get; set; }

    [Ignored] public GameLevelSource Source
    {
        get => (GameLevelSource)this._Source;
        set => this._Source = (int)value;
    }

    // ReSharper disable once InconsistentNaming
    internal int _Source { get; set; }
    
    /// <summary>
    /// The associated ID for the developer level, this is only relevant if Source == Story
    /// </summary>
    [Indexed] public int StoryId { get; set; }
    
    public bool IsLocked { get; set; }
    public bool IsSubLevel { get; set; }
    public bool IsCopyable { get; set; }
    
    /// <summary>
    /// The score, used for Cool Levels.
    /// </summary>
    /// <seealso cref="CoolLevelsWorker"/>
    public float Score { get; set; }

#nullable disable
    public IList<GameComment> LevelComments { get; }
    public IList<GameLevelVersion> LevelVersions { get; }
    
    [Backlink(nameof(FavouriteLevelRelation.Level))]
    public IQueryable<FavouriteLevelRelation> FavouriteRelations { get; }
    
    [Backlink(nameof(UniquePlayLevelRelation.Level))]
    public IQueryable<UniquePlayLevelRelation> UniquePlays { get; }
    
    [Backlink(nameof(PlayLevelRelation.Level))]
    public IQueryable<PlayLevelRelation> AllPlays { get; }
    [Backlink(nameof(GameSubmittedScore.Level))]
    public IQueryable<GameSubmittedScore> Scores { get; }
    
    [Backlink(nameof(RateLevelRelation.Level))]
    public IQueryable<RateLevelRelation> Ratings { get; }
    
    // ILists can't be serialized to XML, and Lists/Arrays cannot be stored in realm,
    // hence _SkillRewards and SkillRewards both existing
    // ReSharper disable once InconsistentNaming
    public IList<GameSkillReward> _SkillRewards { get; }
    
#nullable restore
    
    [XmlArray("customRewards")]
    [XmlArrayItem("customReward")]
    public GameSkillReward[] SkillRewards
    {
        get => this._SkillRewards.ToArray();
        set
        {
            this._SkillRewards.Clear();
            value = value.OrderBy(r=>r.Id).ToArray();
            
            // There should never be more than 3 skill rewards
            for (int i = 0; i < Math.Min(value.Length, 3); i++)
            {
                GameSkillReward reward = value[i];
                reward.Id = i;
                this._SkillRewards.Add(reward);
            }
        }
    }
    
    public int SequentialId
    {
        set => this.LevelId = value;
    }

    public GameUser? Publisher { get; set; }

    /// <summary>
    /// Calculates the average rating of a level based on the ratings it has.
    /// </summary>
    /// <returns>A double between 1 and 5, indicating the level's average ratings.</returns>
    public double CalculateAverageStarRating()
    {
        int yayCount = this.Ratings.Count(x => x._RatingType == (int)RatingType.Yay);
        int booCount = this.Ratings.Count(x => x._RatingType == (int)RatingType.Boo);
        int neutralCount = this.Ratings.Count(x => x._RatingType == (int)RatingType.Neutral);

        // Return 0 if all the counts are 0, we dont want a div by 0 error!
        if (yayCount + booCount + neutralCount == 0) return 0;
        
        return (double)((5 * yayCount) + (1 * booCount) + (3 * neutralCount)) / (yayCount + booCount + neutralCount);
    }
}