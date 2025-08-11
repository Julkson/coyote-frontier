using System.Collections.Frozen;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.GenitalsShared;

/// <summary>
/// Hi! This is the genital manager! It handles all the stuff related to genitals.
/// </summary>
public sealed class GenitalManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly List<GenitalShapePrototype> _index = new();
    public FrozenDictionary<GenitalSlot, FrozenDictionary<string, GenitalShapePrototype>> CategorizedGenitals = default!;
    public FrozenDictionary<string, GenitalShapePrototype> Genitals = default!;

    public void Initialize()
    {
        _prototypeManager.PrototypesReloaded += OnPrototypeReload;
        CachePrototypes();
    }

    private void CachePrototypes()
    {
        _index.Clear();
        var markingDict = new Dictionary<GenitalSlot, Dictionary<string, GenitalShapePrototype>>();

        foreach (var category in Enum.GetValues<GenitalSlot>())
        {
            markingDict.Add(category, new());
        }

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<GenitalShapePrototype>())
        {
            _index.Add(prototype);
            markingDict[prototype.Kind].Add(prototype.ID, prototype);
        }

        Genitals = _prototypeManager.EnumeratePrototypes<GenitalShapePrototype>().ToFrozenDictionary(x => x.ID);
        CategorizedGenitals = markingDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
    }
    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingPrototype>())
            CachePrototypes();
    }

    public void GetGenital(ProtoId<GenitalShapePrototype> id, out GenitalShapePrototype? prototype)
    {
        if (Genitals.TryGetValue(id, out prototype))
            return;
        prototype = null;
    }

    public void GetGenitalSize(ProtoId<GenitalShapePrototype> id,
        int saniSize,
        out GenitalSizeCollection genSizeProt)
    {
        if (!Genitals.TryGetValue(id, out var prototype))
        {
            saniSize = 0;
            genSizeProt = null!;
            return;
        }
        saniSize = Math.Clamp(
            saniSize,
            0,
            prototype.Sizes.Count - 1);
        genSizeProt = prototype.Sizes[saniSize];
    }

    public void GetGenitalSlot(string id, out GenitalSlot slot)
    {
        slot = GenitalSlot.NoZZZne;
        GetGenital(id, out var prototype);
        if (prototype != null)
            slot = prototype.Kind;
    }
}
