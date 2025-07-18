coyote-rp-incentive-payward-message =
    Incentive payward deposited: { $hasExpedMult ->
    *[false] { $amount } credits.
     [true] { $preExpedAmount} x {$multiplier} credits = { $amount } credits.
    }

coyote-rp-incentive-expedition-incentive-message =
    {$success ->
    *[false] For showing up and trying to help with the expedition,
    [true] For doing such a great job on the expedition,
    } your incentive payward's multiplier is now set to {$multiplier}x for the next {$time} minutes!



