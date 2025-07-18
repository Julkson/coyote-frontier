using Robust.Shared.Serialization;

namespace Content.Server._Coyote;

/// <summary>
/// Enum of possible roleplay actions.
/// </summary>
[Serializable]
public enum ExpedIncentiveBonus : byte
{
    Success,
    Failure,
    None,
}
