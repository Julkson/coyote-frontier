using Content.Shared._Coyote;

namespace Content.Server._Coyote

{
    /// <summary>
    /// Structure to hold the action and the time it was taken.
    /// </summary>
    public sealed class ExpeditionIncentive(
        ExpedIncentiveBonus bonus,
        TimeSpan timeDid,
        TimeSpan timeExpires
    )
    {
        /// <summary>
        /// The bonus type that was given for the action.
        /// </summary>
        public ExpedIncentiveBonus Bonus = bonus;

        /// <summary>
        /// The time the action was taken.
        /// </summary>
        public TimeSpan TimeDid = timeDid;

        /// <summary>
        /// The time the action expires.
        /// </summary>
        public TimeSpan TimeExpires = timeExpires;
    }
}
