using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Shared._Coyote.RolePlayIncentiveShared;

/// <summary>
/// This is the event raised when a roleplay incentive action is taken.
/// </summary>
public sealed class GetRoleplayIncentiveModifier(EntityUid src)
    : EntityEventArgs
{
    public EntityUid Source = src;
    public float Multiplier = 1f;
    public float Additive = 0f;

    /// <summary>
    /// This is used to modify the roleplay incentive multiplier and additive values.
    /// </summary>
    public void Modify(float multiplier, float additive)
    {
        Multiplier *= multiplier;
        Additive += additive;
    }
}
