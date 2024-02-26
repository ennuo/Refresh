using System.Xml.Serialization;
using Realms;

namespace Refresh.GameServer.Types.Challenges;

public partial class GameChallengeCriterion : IEmbeddedObject
{
    public long Value { get; set; }
    [Ignored] public ChallengeMetric Metric
    {
        get => (ChallengeMetric)this._Metric;
        set => this._Metric = (int)value;
    }
    
    // ReSharper disable once InconsistentNaming
    int _Metric { get; set; }
}