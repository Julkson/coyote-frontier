using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.RolePlayIncentiveShared;

/// <summary>
/// This is a prototype for defining cool RPI stuff for like, tax brackets and such.
/// </summary>
[Prototype("rpiTaxBracket")]
public sealed partial class RpiTaxBracketPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// This bracket applies to you if you have less than this amount of $$$
    /// </summary>
    [DataField("cashThreshold", required: true)]
    public int CashThreshold = 0;

    /// <summary>
    /// The payout you get, per Judgement Point.
    /// </summary>
    [DataField("judgementPointPayout", required: true)]
    public int JudgementPointPayout = 0;

    /// <summary>
    /// How much you get penalized for dying. This is percent of your total cash!
    /// </summary>
    [DataField("deathPenalty", required: true)]
    public float DeathPenalty = 0f;

    /// <summary>
    /// How much you get penalized for being deep-fried. This is percent of your total cash!
    /// </summary>
    [DataField("deepFriedPenalty", required: true)]
    public float DeepFriedPenalty = 0f;
}
