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
    public List<RpiChatRecord> ChatActionsTaken = new();

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
    public TimeSpan PaywardInterval = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Interval between paywards when offline.
    /// hey guess what doesnt work? this thing!
    /// </summary>
    [DataField]
    public TimeSpan PaywardIntervalOffline = TimeSpan.FromMinutes(30); // TimeSpan.FromMinutes(15);

    /// <summary>
    /// The last time they were PUNISHED for DYING like a noob.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastDeathPunishment = TimeSpan.Zero;

    /// <summary>
    /// The last time they were PUNISHED for hopping in the fukcing deep fryer, you LRP frick.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastDeepFryerPunishment = TimeSpan.Zero;

    /// <summary>
    /// Punish dying?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PunishDeath = false;

    /// <summary>
    /// Punish deep frying?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PunishDeepFryer = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracketPayoutOverride = -1; // -1 means no override, use the default payouts
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracketDeathPenaltyOverride = -1; // -1 means no override, use the default payouts
    [ViewVariables(VVAccess.ReadWrite)]
    public int TaxBracketDeepFryerPenaltyOverride = -1; // -1 means no override, use the default payouts

    [ViewVariables(VVAccess.ReadWrite)]
    public float DebugMultiplier = 1.0f;

}
