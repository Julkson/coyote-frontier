namespace Content.Server._Coyote;

/// <summary>
/// Hi! This is the RP incentive component.
/// This will track the actions a player does, and adjust some paywards
/// for them once if they do those things, sometimes!
/// </summary>
[RegisterComponent]
public sealed partial class RoleplayIncentiveComponent : Component
{
    /// <summary>
    /// The actions that have taken place.
    /// </summary>
    [DataField]
    public List<RoleplayAction> ActionsTaken = new();

    /// <summary>
    /// The last time the system checked for actions, for paywards.
    /// </summary>
    [DataField]
    public DateTime LastCheck = DateTime.MinValue;

    /// <summary>
    /// The next time the system will check for actions, for paywards.
    /// </summary>
    [DataField]
    public TimeSpan NextPayward = TimeSpan.Zero;

    /// <summary>
    /// Interval between paywards.
    /// </summary>
    [DataField]
    public TimeSpan PaywardInterval = TimeSpan.FromMinutes(20); // TimeSpan.FromMinutes(15);

    /// <summary>
    /// Interval between paywards when offline.
    /// </summary>
    [DataField]
    public TimeSpan PaywardIntervalOffline = TimeSpan.FromMinutes(30); // TimeSpan.FromMinutes(15);

    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracket1Payout = 50;
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracket2Payout = 50;
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracket3Payout = 50;
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracketRestPayout = 50;
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracketPayoutOverride = -1; // -1 means no override, use the default payouts
    [ViewVariables(VVAccess.ReadWrite)]
    public string TaxZZZCurrentActiveBracket = "TaxBracket1";

}
