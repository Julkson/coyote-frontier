using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server._Coyote;

/// <summary>
/// This is the event raised when a roleplay incentive action is taken.
/// </summary>
public sealed class ExpeditionIncentiveEvent(
    ExpedIncentiveBonus bonus = ExpedIncentiveBonus.None
    )
    : EntityEventArgs
{
    /// <summary>
    /// The bonus that was given for the action.
    /// </summary>
    public ExpedIncentiveBonus Bonus = bonus;

}
