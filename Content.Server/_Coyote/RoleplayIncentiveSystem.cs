using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Coyote.CoolIncentives;
using Content.Server._NF.Bank;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Coyote.RolePlayIncentiveShared;
using Content.Shared._NF.Bank.Components;
using Content.Shared.Chat;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.SSDIndicator;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// ReSharper disable InconsistentNaming

namespace Content.Server._Coyote;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoleplayIncentiveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly BankSystem _bank = null!;
    [Dependency] private readonly PopupSystem _popupSystem = null!;
    [Dependency] private readonly ChatSystem _chatsys = null!;
    [Dependency] private readonly IChatManager _chatManager = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly SSDIndicatorSystem _ssdThing = null!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private List<ProtoId<RpiTaxBracketPrototype>> RpiDatumPrototypes = new()
    {
        "rpiTaxBracketBroke",
        "rpiTaxBracketEstablished",
        "rpiTaxBracketWealthy",
    };
    private ProtoId<RpiTaxBracketPrototype> TaxBracketDefault = "rpiTaxBracketDefault";
    private Dictionary<RpiChatActionCategory, string> ChatActionLookup = new()
    {
        { RpiChatActionCategory.Speaking, "rpiChatActionSpeaking" },
        { RpiChatActionCategory.Whispering, "rpiChatActionWhispering" },
        { RpiChatActionCategory.Emoting, "rpiChatActionEmoting" },
        { RpiChatActionCategory.QuickEmoting, "rpiChatActionQuickEmoting" },
        { RpiChatActionCategory.Subtling, "rpiChatActionSubtling" },
        { RpiChatActionCategory.Radio, "rpiChatActionRadio" },
    };

    private TimeSpan DeathPunishmentCooldown = TimeSpan.FromMinutes(30);
    private TimeSpan DeepFryerPunishmentCooldown = TimeSpan.FromMinutes(5); // please stop deep frying tesharis

    /// <inheritdoc/>
    public override void Initialize()
    {
        // get the component this thing is attached to            v- my code my formatting
        SubscribeLocalEvent<RoleplayIncentiveComponent,            ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RoleplayIncentiveComponent,            RoleplayIncentiveEvent>(OnGotRoleplayIncentiveEvent);
        SubscribeLocalEvent<RoleplayIncentiveComponent,            GetRoleplayIncentiveModifier>(OnSelfSucc);
        SubscribeLocalEvent<RoleplayIncentiveComponent,            MobStateChangedEvent>(OnGotMobStateChanged);
        // v- these are awful
        SubscribeLocalEvent<CoolChefComponent,                     GetRoleplayIncentiveModifier>(AdjustRPI);
        SubscribeLocalEvent<CoolPirateComponent,                   GetRoleplayIncentiveModifier>(AdjustRPI);
        SubscribeLocalEvent<CoolStationRepComponent,               GetRoleplayIncentiveModifier>(AdjustRPI);
        SubscribeLocalEvent<CoolStationTrafficControllerComponent, GetRoleplayIncentiveModifier>(AdjustRPI);
        SubscribeLocalEvent<CoolStationDirectorOfCareComponent,    GetRoleplayIncentiveModifier>(AdjustRPI);
        SubscribeLocalEvent<CoolSheriffComponent,                  GetRoleplayIncentiveModifier>(AdjustRPI);
        SortTaxBrackets();
    }

    /// <summary>
    /// Sorts the tax brackets by cash threshold, lowest to highest.
    /// This is done so that we can easily find the correct tax bracket for a player.
    /// </summary>
    private void SortTaxBrackets()
    {
        RpiDatumPrototypes.Sort(
            (a, b) =>
            {
                if (!_prototype.TryIndex(a, out var protoA))
                {
                    Log.Warning($"RpiTaxBracketPrototype {a} not found!");
                    return 0;
                }

                if (!_prototype.TryIndex(b, out var protoB))
                {
                    Log.Warning($"RpiTaxBracketPrototype {b} not found!");
                    return 0;
                }

                return protoA.CashThreshold.CompareTo(protoB.CashThreshold);
            });
    }

    #region Event Handlers
    private void OnComponentInit(EntityUid uid, RoleplayIncentiveComponent component, ComponentInit args)
    {
        // set the next payward time
        component.NextPayward = _timing.CurTime + component.PaywardInterval;
    }

    /// <summary>
    /// This is called when a roleplay incentive event is received.
    /// It checks if it should be done, then it does it when it happensed
    /// </summary>
    /// <param name="uid">The entity that did the thing</param>
    /// <param name="rpic">The roleplay incentive component on the entity</param>
    /// <param name="args">The roleplay incentive event that was received</param>
    /// <remarks>
    /// piss
    /// </remarks>
    private void OnGotRoleplayIncentiveEvent(
        EntityUid uid,
        RoleplayIncentiveComponent rpic,
        RoleplayIncentiveEvent args)
    {
        ProcessRoleplayIncentiveEvent(uid, args);
    }

    /*
     * None
     * Local -> RpiChatActionCategory.Speaking
     * Whisper -> RpiChatActionCategory.Whispering
     * Server
     * Damage
     * Radio -> RpiChatActionCategory.Radio
     * LOOC
     * OOC
     * Visual
     * Notifications
     * Emotes -> RpiChatActionCategory.Emoting OR RpiChatActionCategory.QuickEmoting
     * Dead
     * Admin
     * AdminAlert
     * AdminChat
     * Unspecified
     * Telepathic
     * Subtle -> RpiChatActionCategory.Subtling
     * rest are just null
     */

    /// <summary>
    /// Applies the self success multiplier to the payward
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnSelfSucc(
        EntityUid uid,
        RoleplayIncentiveComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        if (TryComp<SSDIndicatorComponent>(uid, out var ssd)
            && _ssdThing.IsInNashStation(uid))
        {
            args.Modify(1.5f, 0f); // 'double' pay if youre in nash!
        }
    }

    /// <summary>
    /// If the mob dies, punish them for being awful
    /// </summary>
    private void OnGotMobStateChanged(
        EntityUid uid,
        RoleplayIncentiveComponent rpic,
        MobStateChangedEvent args)
    {
        if (!rpic.PunishDeath)
            return;
        if (args.NewMobState != MobState.Dead)
            return;
        var curTime = _timing.CurTime;
        // if they died recently, dont punish them again
        if (curTime < rpic.LastDeathPunishment + DeathPunishmentCooldown)
            return;
        PunishPlayerForDeath(uid, rpic);
    }
    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RoleplayIncentiveComponent>();
        while (query.MoveNext(out var uid, out var rpic))
        {
            if (_timing.CurTime < rpic.NextPayward)
                continue;
            rpic.NextPayward = _timing.CurTime + rpic.PaywardInterval;
            // check if they have a bank account
            if (!TryComp<BankAccountComponent>(uid, out _))
            {
                continue; // no bank account, no pramgle
            }

            // pay the player
            PayoutPaywardToPlayer(uid, rpic);
        }
    }

    #region Payward Action
    /// <summary>
    /// Goes through all the relevant actions taken and stored, judges them,
    /// And gives the player a payward if they did something good.
    /// It also checks for things like duplicate actions, if theres people around, etc.
    /// Basically if you do stuff, you get some pay for it!
    /// </summary>
    private void PayoutPaywardToPlayer(EntityUid uid, RoleplayIncentiveComponent rpic)
    {
        if (!_bank.TryGetBalance(uid, out var hasThisMuchMoney))
            return; // no bank account, no pramgle
        //first check if this rpic is actually on the uid
        if (!TryComp<RoleplayIncentiveComponent>(uid, out var incentive))
        {
            Log.Warning($"RoleplayIncentiveComponent not found on entity {uid}!");
            return;
        }

        var chatJudgement = GetChatActionJudgement(rpic.ChatActionsTaken);
        var taxBracket = GetTaxBracketData(rpic, hasThisMuchMoney);

        var chatPay = chatJudgement * taxBracket.PayPerJudgement;

        var modifyEvent = new GetRoleplayIncentiveModifier(uid);
        RaiseLocalEvent(
            uid,
            modifyEvent,
            true);
        if (Math.Abs(rpic.DebugMultiplier - 1f) > 0.001f)
        {
            Log.Info($"RPI Debug Multiplier applied: {rpic.DebugMultiplier}");
            modifyEvent.Multiplier = rpic.DebugMultiplier;
        }
        ProcessPaymentDetails(
            chatPay,
            modifyEvent,
            out var payDetails);

        // pay the player
        if (!_bank.TryBankDeposit(uid, payDetails.FinalPay))
        {
            Log.Warning($"Failed to deposit {payDetails.FinalPay} into bank account of entity {uid}!");
            return;
        }
        ShowPopup(uid, payDetails);
        ShowChatMessage(uid, payDetails);
        PruneOldActions(incentive);
    }
    #endregion

    #region Punishment Actions

    /// <summary>
    /// Punishes the player for dying, based on their tax bracket.
    /// This will take money from their bank account, based on their tax bracket.
    /// </summary>
    private void PunishPlayerForDeath(EntityUid uid, RoleplayIncentiveComponent rpic)
    {
        if (!_bank.TryGetBalance(uid, out var hasThisMuchMoney))
            return; // no bank account, no pramgle
        var taxBracket = GetTaxBracketData(rpic, hasThisMuchMoney);
        var penalty = taxBracket.DeathPenalty;
        if (penalty > hasThisMuchMoney)
            penalty = hasThisMuchMoney; // cant take more than they have
        if (penalty <= 0)
            return; // no penalty, no punishment
        if (!_bank.TryBankWithdraw(uid, (int)penalty))
        {
            Log.Warning($"Failed to withdraw {penalty} from bank account of entity {uid}!");
            return;
        }
        rpic.LastDeathPunishment = _timing.CurTime;
        // tell them they got punished
        var message = Loc.GetString(
            "coyote-rp-incentive-death-penalty-message",
            ("amount", (int)penalty));
        if (_playerManager.TryGetSessionByEntity(uid, out var session))
        {
            _chatManager.ChatMessageToOne(
                ChatChannel.Notifications,
                message,
                message,
                default,
                false,
                session.Channel);
        }
        // also show a popup
        _popupSystem.PopupEntity(
            message,
            uid,
            uid,
            PopupType.LargeCaution);
    }

    #endregion

    #region Helpers
    private int GetChatActionJudgement(List<RpiChatRecord> actions)
    {
        var total = 0;
        // go through all the actions, and compile the BEST ONES EVER
        Dictionary<RpiChatActionCategory, float> bestActions = new();
        foreach (var action in actions.Where(action => !(action.Judgement > 0)))
        {
            var judgement = JudgeChatAction(action);
            // slot it into the best action for that type
            if (bestActions.TryGetValue(action.Action, out var existing))
            {
                if (judgement > existing)
                {
                    bestActions[action.Action] = judgement;
                }
            }
            else
            {
                bestActions[action.Action] = judgement;
            }
        }
        // now, sum up the best actions
        foreach (var kvp in bestActions)
        {
            total += (int)MathF.Ceiling(kvp.Value);
            // also, mark the actions as judged
            foreach (var action in actions.Where(a => a.Action == kvp.Key && a.Judgement == 0))
            {
                action.Judgement = kvp.Value;
            }
        }
        return total;
    }

    private void PruneOldActions(RoleplayIncentiveComponent rpic)
    {
        rpic.ChatActionsTaken.Clear();
    }

    private TaxBracketResult GetTaxBracketData(
        RoleplayIncentiveComponent rpic,
        int hasThisMuchMoney)
    {
        var taxBracket = new TaxBracketResult(0, 0, 0); // default values
        // go through the prototypes, and find the one that fits the player's money
        // if none fit, use the default
        if (!_prototype.TryIndex(TaxBracketDefault, out var defaultProto))
        {
            Log.Warning($"RpiTaxBracketPrototype {TaxBracketDefault} not found! ITS THE DEFAULT AOOOAOAOAOOA");
            return taxBracket;
        }
        RpiTaxBracketPrototype? proto = null;
        // go through the sorted list, and find the Lowest bracket that is higher than the player's money
        List<RpiTaxBracketPrototype> protosHigherThanPlayer = new();
        foreach (var protoId in RpiDatumPrototypes)
        {
            if (!_prototype.TryIndex(protoId, out var myProto))
            {
                Log.Warning($"RpiTaxBracketPrototype {protoId} not found!");
                continue;
            }
            if (hasThisMuchMoney < myProto.CashThreshold)
            {
                protosHigherThanPlayer.Add(myProto);
            }
        }
        // if we found any, use the lowest one
        if (protosHigherThanPlayer.Count > 0)
        {
            proto = protosHigherThanPlayer.OrderBy(p => p.CashThreshold).First();
        }
        // if we didnt find any, use the default
        proto ??= defaultProto;
        taxBracket = new TaxBracketResult(
            proto.JudgementPointPayout,
            (int)(proto.DeathPenalty * hasThisMuchMoney),
            (int)(proto.DeepFriedPenalty * hasThisMuchMoney));

        // and now the overrides
        if (rpic.TaxBracketPayoutOverride != -1)
        {
            taxBracket.PayPerJudgement = rpic.TaxBracketPayoutOverride;
        }

        if (rpic.TaxBracketDeathPenaltyOverride != -1)
        {
            taxBracket.DeathPenalty = rpic.TaxBracketDeathPenaltyOverride;
        }

        if (rpic.TaxBracketDeepFryerPenaltyOverride != -1)
        {
            taxBracket.DeepFryPenalty = rpic.TaxBracketDeepFryerPenaltyOverride;
        }
        return taxBracket;
    }

    private void ProcessPaymentDetails(
        int basePay,
        GetRoleplayIncentiveModifier modifyEvent,
        out PayoutDetails details)
    {
        var finalPay = basePay;
        // apply the add first
        finalPay += (int)modifyEvent.Additive;
        // then apply the multiplier
        finalPay = (int)(finalPay * modifyEvent.Multiplier);
        // clamp the pay amount to a minimum of 20 and a maximum of int.MaxValue
        finalPay = Math.Clamp(
            (int)(Math.Ceiling(finalPay / 10.0) * 10),
            20,
            int.MaxValue);

        var addedPay = (int) modifyEvent.Additive;
        // round the multiplier to 2 decimal places
        var multiplier = modifyEvent.Multiplier;
        var hasMultiplier = Math.Abs(multiplier - 1f) > 0.01f;
        var hasAdditive = addedPay != 0;
        var hasModifier = hasMultiplier || hasAdditive;
        details = new PayoutDetails(
            basePay,
            finalPay,
            addedPay,
            multiplier,
            modifyEvent.Multiplier,
            hasModifier,
            hasAdditive,
            hasMultiplier);
    }

    private void ShowPopup(EntityUid uid, PayoutDetails payDetails)
    {
        if (payDetails.FinalPay <= 0)
            return; // no pay, no popup
        var messageOverhead = Loc.GetString(
            "coyote-rp-incentive-payward-message",
            ("amount", payDetails.FinalPay));
        _popupSystem.PopupEntity(
            messageOverhead,
            uid,
            uid);
    }

    private void ShowChatMessage(EntityUid uid, PayoutDetails payDetails)
    {
        if (payDetails.FinalPay <= 0)
            return; // no pay, no popup
        var message = "Hi mom~";
        // convert the multiplier to a string with 2 decimal places, if present
        if (payDetails.HasModifier)
        {
            if (payDetails.HasMultiplier && payDetails.HasAdditive)
            {
                message = Loc.GetString(
                    "coyote-rp-incentive-payward-message-multiplier-and-additive",
                    ("amount", payDetails.FinalPay),
                    ("basePay", payDetails.BasePay),
                    ("multiplier", payDetails.Multiplier),
                    ("additive", payDetails.AddedPay));
            }
            else if (payDetails.HasMultiplier)
            {
                message = Loc.GetString(
                    "coyote-rp-incentive-payward-message-multiplier",
                    ("amount", payDetails.FinalPay),
                    ("basePay", payDetails.BasePay),
                    ("multiplier", payDetails.Multiplier));
            }
            else if (payDetails.HasAdditive)
            {
                message = Loc.GetString(
                    "coyote-rp-incentive-payward-message-additive",
                    ("amount", payDetails.FinalPay),
                    ("basePay", payDetails.BasePay),
                    ("additive", payDetails.AddedPay));
            }
        }
        else
        {
            message = Loc.GetString(
                "coyote-rp-incentive-payward-message",
                ("amount", payDetails.FinalPay));
        }

        // cum it to chat
        if (_playerManager.TryGetSessionByEntity(uid, out var session))
        {
            _chatManager.ChatMessageToOne(
                ChatChannel.Notifications,
                message,
                message,
                default,
                false,
                session.Channel);
        }
    }

    private void ProcessRoleplayIncentiveEvent(EntityUid uid, RoleplayIncentiveEvent args)
    {
        // first, check if the uid has the component
        if (!TryComp<RoleplayIncentiveComponent>(uid, out var incentive))
        {
            Log.Warning($"RoleplayIncentiveComponent not found on entity {uid}!");
            return;
        } // i guess?

        // then, check if the channel in the args can be translated to a RoleplayAct
        var actOut = ChatChannel2RpiChatAction(args.Channel);
        if (actOut == RpiChatActionCategory.None)
        {
            return; // lot of stuff happens and it dont
        }

        // if its EmotingOrQuickEmoting, we need to doffgerentiate thewween the tween the two
        if (actOut == RpiChatActionCategory.EmotingOrQuickEmoting)
        {
            actOut = DoffgerentiateEmotingAndQuickEmoting(
                args.Source,
                args.Message);
        }

        // make the thing
        var action = new RpiChatRecord(
            actOut,
            _timing.CurTime,
            args.Message,
            args.PeoplePresent);
        // add it to the actions taken
        incentive.ChatActionsTaken.Add(action);
        // and we're good
    }

    private bool GetChatActionLookup(
        RpiChatActionCategory action,
        [NotNullWhen(true)] out RpiChatActionPrototype? proot)
    {
        if (!ChatActionLookup.TryGetValue(action, out var myPrototype))
        {
            proot = null;
            return false;
        }
        if (!_prototype.TryIndex<RpiChatActionPrototype>(myPrototype, out var proto))
        {
            Log.Warning($"RpiChatActionPrototype {myPrototype} not found!");
            proot = null;
            return false;
        }
        proot = proto;
        return true;
    }

    private static RpiChatActionCategory ChatChannel2RpiChatAction(ChatChannel channel)
    {
        // this is a bit of a hack, but it works
        return channel switch
        {
            ChatChannel.Local => RpiChatActionCategory.Speaking,
            ChatChannel.Whisper => RpiChatActionCategory.Whispering,
            ChatChannel.Emotes => RpiChatActionCategory.EmotingOrQuickEmoting, // we dont know yet
            ChatChannel.Radio => RpiChatActionCategory.Radio,
            ChatChannel.Subtle => RpiChatActionCategory.Subtling,
            // the rest are not roleplay actions
            _ => RpiChatActionCategory.None,
        };
    }

    private RpiChatActionCategory DoffgerentiateEmotingAndQuickEmoting(
        EntityUid source,
        string message
    )
    {
        return _chatsys.TryEmoteChatInput(
            source,
            message,
            false)
            ? RpiChatActionCategory.QuickEmoting // if the message is a valid emote, then its a quick emote
            : RpiChatActionCategory.Emoting;

        // well i cant figure out how the system does it, so im just gonnasay if theres
        // no spaces, its a quick emote
        // return !message.Contains(' ')
        //     ? RpiChatActionCategory.QuickEmoting
        //     // otherwise, its a normal emote
        //     : RpiChatActionCategory.Emoting;
    }

        /// <summary>
    /// Passes judgement on the action
    /// Based on a set of criteria, it will return a judgement value
    /// It will be judged based on:
    /// - How long the text was
    /// - How many people were present
    /// - and thats it for now lol
    /// </summary>
    private int JudgeChatAction(RpiChatRecord chatRecord)
    {
        var lengthMult = GetMessageLengthMultiplier(chatRecord.Action, chatRecord.Message?.Length ?? 1);
        var listenerMult = GetListenerMultiplier(chatRecord.Action, chatRecord.PeoplePresent);
        // if the action is a quick emote, it gets no judgement
        var judgement = lengthMult + listenerMult + 1f;
        return (int)Math.Floor(judgement);
    }

    /// <summary>
    /// Gets the multiplier for the number of listeners present
    /// </summary>
    /// <param name="action">The action being performed</param>
    /// <param name="listeners">The number of listeners present</param>
    private int GetListenerMultiplier(RpiChatActionCategory action, int listeners)
    {
        // if there are no listeners, return 0
        if (listeners <= 0)
            return 1;
        var numListeners = listeners;
        if (!GetChatActionLookup(action, out var proto))
            return 1;
        if (!proto.MultiplyByPeoplePresent)
            return 1;
        // clamp the number of listeners to the max defined in the prototype
        numListeners = Math.Clamp(
            numListeners,
            0,
            proto.MaxPeoplePresent);
        return numListeners;
    }

    /// <summary>
    /// Gets the message length multiplier for the action
    /// </summary>
    /// <param name="action">The action being performed</param>
    /// <param name="messageLength">The length of the message</param>
    private int GetMessageLengthMultiplier(RpiChatActionCategory action, int messageLength)
    {
        // if the message length is 0, return 1
        if (messageLength <= 0)
            return 1;

        // get the prototype for the action
        if (!GetChatActionLookup(action, out var proto))
            return 1;

        if (proto.LengthPerPoint <= 0)
        {
            return 1; // thingy isnt using length based judgement, also dont divide by 0
        }

        var rawLengthMult = messageLength / (float)proto.LengthPerPoint;
        // floor it to the nearest whole number, with a minimum of 1 and max of who cares
        return Math.Clamp(
            (int)Math.Floor(rawLengthMult),
            1,
            100);
    }
    #endregion

    #region Data Holbies
    public sealed class TaxBracketResult(
        int payPerJudgement,
        int deathPenalty,
        int deepFryPenalty)
    {
        public int PayPerJudgement = payPerJudgement;
        public int DeathPenalty = deathPenalty;
        public int DeepFryPenalty = deepFryPenalty;
    }

    private struct PayoutDetails(
        int basePay,
        int finalPay,
        int addedPay,
        FixedPoint2 multiplier,
        FixedPoint2 rawMultiplier,
        bool hasModifier,
        bool hasAdditive,
        bool hasMultiplier)
    {
        public int BasePay = basePay;
        public int FinalPay = finalPay;
        public int AddedPay = addedPay;
        public FixedPoint2 Multiplier = multiplier;
        public FixedPoint2 RawMultiplier = rawMultiplier;
        public bool HasModifier = hasModifier;
        public bool HasAdditive = hasAdditive;
        public bool HasMultiplier = hasMultiplier;
    }
    #endregion

    #region Awgful Job RPI modifiers
    private void AdjustRPI(
        float mult,
        ref GetRoleplayIncentiveModifier args)
    {
        args.Modify(mult, 0f);
    }

    private void AdjustRPI(
        EntityUid uid,
        CoolChefComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }

    private void AdjustRPI(
        EntityUid uid,
        CoolPirateComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }

    private void AdjustRPI(
        EntityUid uid,
        CoolStationRepComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }

    private void AdjustRPI(EntityUid uid,
        CoolStationTrafficControllerComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }

    private void AdjustRPI(EntityUid uid,
        CoolStationDirectorOfCareComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }

    private void AdjustRPI(
        EntityUid uid,
        CoolSheriffComponent component,
        ref GetRoleplayIncentiveModifier args)
    {
        AdjustRPI(component.Multiplier, ref args);
    }
    #endregion
}
