using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Effects.Special;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Intel;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Reagent = Content.Shared._RMC14.Chemistry.Reagent.Reagent;

namespace Content.Server._RMC14.Chemistry.Generation;
// INSANITY BELOW
public sealed record ContractStats(
    string Name,
    string Color,
    string PhysicalDesc,
    List<(ChemGeneratorPropertyPrototype Property, int Level)> Properties,
    int Overdose,
    int CriticalOverdose,
    string PropertyHintId,
    string IngredientHintId);

public sealed class RMCChemicalGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCReagentPrototypeSyncSystem _protoSync = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;

    private static readonly EntProtoId ReportProto = "RMCChemResearchReport";

    private int _generatedCount;
    private readonly Dictionary<string, int> _generatedTiers = new();
    private readonly Dictionary<string, string> _originalIds = new();
    private readonly Dictionary<string, ReactionIndicator> _reactionIndicators = new();

    private readonly Dictionary<string, ProtoId<ReagentPrototype>> _simulationResults = new();

    // legendary/ciphering recipes are rolled fresh once per round
    private static readonly string[] LegendaryPropertyIds = ["Hypergenetic", "Boosting", "Regulating", "Optimized"];
    private const string CipheringId = "Ciphering";
    private const string EncryptedId = "Encrypted";
    private readonly Dictionary<string, List<string>> _legendaryCombines = new();

    public override void Initialize()
    {
        base.Initialize();
        RollLegendaryCombines();
    }

    // LEGENDARY_COMBINE_PROPERTIES = 3 random properties per legendary, drawn from the same
    // pool PickProperties uses. Ciphering is a separate case sharing the mechanism: 2 random
    // properties plus a hardcoded 3rd slot of encrypted
    private void RollLegendaryCombines()
    {
        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>()
            .Where(p => !p.Flags.HasFlag(ChemPropertyType.Disabled))
            .Select(p => p.ID)
            .ToList();

        if (pool.Count == 0)
            return;

        foreach (var legendary in LegendaryPropertyIds)
        {
            var available = new List<string>(pool);
            var pieces = new List<string>();
            for (var i = 0; i < 3 && available.Count > 0; i++)
                pieces.Add(_random.PickAndTake(available));

            _legendaryCombines[legendary] = pieces;
        }

        var cipherAvailable = new List<string>(pool);
        var cipherPieces = new List<string>();
        for (var i = 0; i < 2 && cipherAvailable.Count > 0; i++)
            cipherPieces.Add(_random.PickAndTake(cipherAvailable));
        cipherPieces.Add(EncryptedId);

        _legendaryCombines[CipheringId] = cipherPieces;
    }

    public bool TryGetLegendaryRecipe(string propertyId, [NotNullWhen(true)] out List<string>? pieces) =>
        _legendaryCombines.TryGetValue(propertyId, out pieces);

    // tracked for the life of the round so low tier chems don't keep
    // rolling the same property, until every property in that category has appeared at least once.
    private readonly Dictionary<ChemPropertyCategory, HashSet<string>> _generatedProperties = new()
    {
        [ChemPropertyCategory.Positive] = new(),
        [ChemPropertyCategory.Negative] = new(),
        [ChemPropertyCategory.Neutral] = new(),
    };

    public bool IsGenerated(ProtoId<ReagentPrototype> id) => _generatedTiers.ContainsKey(id.Id);

    public bool TryGetGeneratedTier(ProtoId<ReagentPrototype> id, out int tier) => _generatedTiers.TryGetValue(id.Id, out tier);

    public bool TryGetOriginalId(string id, [NotNullWhen(true)] out string? originalId) => _originalIds.TryGetValue(id, out originalId);

    public IEnumerable<string> GetVariantsOf(string baseReagentId) => _originalIds.Where(kv => kv.Value == baseReagentId).Select(kv => kv.Key);

    private static readonly string[] Prefix =
    [
        "Alph", "At", "Aw", "Az", "Bar", "Bet", "Bren", "Bor", "Cak", "Cal", "Cath", "Cet", "Con", "Dal", "De", "Dean",
        "Delt", "Dox", "Em", "Ep", "Ethr", "Far", "Feth", "Fohs", "Het", "Hydr", "Hyper", "Ian", "In", "Iot", "Jam",
        "Kan", "Kap", "Kil", "Kl", "Kol", "Lar", "Lin", "Loz", "Meg", "Meth", "Mn", "Mnem", "Mit", "Mor", "Mut", "Nath",
        "Neth", "Nov", "Nux", "Pas", "Pax", "Per", "Ph", "Phr", "Quaz", "Rath", "Ret", "Rol", "Sal", "Sig", "Stim",
        "Super", "Tau", "Te", "Th", "Thet", "Throx", "Ul", "Up", "Ven", "Vic", "Xen", "Xeon", "Xit", "Xir", "Zar",
        "Zet", "Zol",
    ];

    private static readonly string[] WordRoot =
    [
        "a", "adia", "agha", "ama", "amma", "anthra", "ara", "ata", "ade", "alde", "ale", "ange", "arpe", "anthe",
        "aze", "azide", "adefi", "ami", "ari", "assi", "athi", "avi", "azi", "ado", "alope", "arpo", "alto", "aquo",
        "agu", "alnu", "aphnu", "axu", "adhy", "any", "aphy", "awy", "e", "ea", "ega", "enna", "epina", "era", "erma",
        "etha", "eva", "exa", "edre", "etre", "epe", "ephe", "exe", "efi", "emni", "esli", "eno", "elto", "eto", "evo",
        "elnu", "epu", "ethu", "equ", "edry", "ely", "ety", "evy", "i", "ia", "ie", "iqua", "ipha", "ista", "ira",
        "itra", "iphe", "imne", "ine", "isse", "ihi", "ini", "itri", "izi", "ico", "iglo", "iho", "ino", "isto",
        "immu", "inu", "ixu", "izu", "idy", "immy", "ithy", "ity", "isty", "o", "oga", "olna", "omna", "ora", "oxa",
        "oghe", "one", "ore", "oste", "ove", "oi", "ogi", "oni", "orphi", "owni", "oxi", "ocho", "ommo", "osto",
        "odru", "oru", "owu", "oxu", "odry", "omny", "oxy", "u", "ua", "uga", "unga", "uppa", "ura", "ugre", "une",
        "use", "uve", "udri", "uli", "unni", "utri", "uthri", "uo", "uclo", "uno", "uto", "uu", "uphu", "umnu", "uru",
        "udry", "uly", "upy", "urvy", "y", "ya", "ydra", "yla", "ytha", "yna", "yxa", "ye", "ylfe", "ymne", "ylre",
        "yse", "ytre", "yani", "yphi", "ypi", "yvi", "yo", "ylo", "ypho", "yro", "ydro", "ynu", "ysto", "ytro", "yxu",
        "yru", "ydy",
    ];

    private static readonly string[] Suffix =
    [
        "cin", "cene", "cin", "cine", "cone", "din", "dine", "dol", "done", "drate", "drine", "fen", "fene", "fic",
        "gar", "gen", "l", "lem", "lin", "line", "lis", "mel", "mide", "mite", "mol", "n", "ne", "nt", "phrine", "r",
        "rene", "rine", "ry", "ride", "rium", "rus", "sene", "sine", "sone", "sp", "stol", "tane", "tant", "tene",
        "tine", "thol", "tic", "um", "vene", "vone", "x", "xene", "xin", "xine", "zene", "zine", "zone",
    ];

    private void BroadcastGenerated(
        string id,
        string name,
        string color,
        string physicalDesc,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int overdose,
        int criticalOverdose,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        ChemClass chemClass,
        ReactionIndicator indicator)
    {
        var data = new RMCGeneratedReagentData(
            id,
            name,
            color,
            physicalDesc,
            properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList(),
            overdose,
            criticalOverdose,
            ingredients.Select(i => new RecipeCandidateIngredient { Id = i.Id, Amount = i.Amount, Catalyst = i.Catalyst }).ToList(),
            chemClass,
            indicator);

        _protoSync.Broadcast(data);

        if (chemClass >= ChemClass.Special)
            _intel.AddPendingChemical(id, name);
    }

    private static bool IsWhiteHot(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties)
    {
        var intensity = 0f;
        foreach (var (property, level) in properties)
        {
            intensity += property.Effect switch
            {
                Intensity => level,
                Fueling => -3f * level,
                Oxidizing => 7f * level,
                _ => 0f,
            };
        }

        return intensity >= 50f;
    }

    public ProtoId<ReagentPrototype> GenerateReagent(int tier)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var id = $"RMCGenerated{++_generatedCount}{name.Replace(" ", "")}";
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";

        var properties = PickProperties(tier);
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (overdose, criticalOverdose) = RollOverdose(tier);

        var ingredients = PickRecipeIngredients(tier, id);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, properties, overdose, criticalOverdose, ingredients, ChemClass.Special, indicator);
        AppendToClassPoolCache(ChemClass.Special, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return id;
    }

    public ProtoId<ReagentPrototype> GenerateReagentWithProperties(
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int tier = 1,
        int? overdose = null,
        int? criticalOverdose = null)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var id = $"RMCGenerated{++_generatedCount}{name.Replace(" ", "")}";
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (rolledOverdose, rolledCriticalOverdose) = RollOverdose(tier);
        var finalOverdose = overdose ?? rolledOverdose;
        var finalCriticalOverdose = criticalOverdose ?? rolledCriticalOverdose;

        var ingredients = PickRecipeIngredients(tier, id);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, properties, finalOverdose, finalCriticalOverdose, ingredients, ChemClass.Special, indicator);
        AppendToClassPoolCache(ChemClass.Special, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return id;
    }

    public ContractStats RollStats(int tier)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";
        var properties = PickProperties(tier);
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (overdose, criticalOverdose) = RollOverdose(tier);
        var physicalDesc = _random.Pick(PhysicalDescriptions);

        var propertyHintId = _random.Pick(properties).Property.ID;

        var pools = BuildClassPools();
        var ingredientPool = RollContractHintPool(pools, tier);
        if (ingredientPool.Count == 0)
            ingredientPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();
        var ingredientHintId = ingredientPool.Count > 0 ? _random.Pick(ingredientPool) : string.Empty;

        return new ContractStats(name, color, physicalDesc, properties, overdose, criticalOverdose, propertyHintId, ingredientHintId);
    }

    public (ProtoId<ReagentPrototype> Id, List<(string Id, int Amount, bool Catalyst)> Ingredients) FinalizeContract(ContractStats stats, int tier)
    {
        tier = Math.Clamp(tier, 1, 3);
        var id = $"RMCGenerated{++_generatedCount}{stats.Name.Replace(" ", "")}";

        var forcedFirst = string.IsNullOrEmpty(stats.IngredientHintId) ? null : stats.IngredientHintId;
        var ingredients = PickRecipeIngredients(tier, id, forcedFirst);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, stats.Name, stats.Color, stats.PhysicalDesc, stats.Properties, stats.Overdose, stats.CriticalOverdose, ingredients, ChemClass.Special, indicator);
        AppendToClassPoolCache(ChemClass.Special, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return (id, ingredients);
    }

    private ReactionIndicator RollReactionIndicator()
    {
        // Weighted like cms: Bubbling 1, Fire 2, Endothermic 1, Glowing 1, Smoking 2, Calm 1.
        return _random.Next(0, 8) switch
        {
            0 => ReactionIndicator.Bubbling,
            1 or 2 => ReactionIndicator.Fire,
            3 => ReactionIndicator.Endothermic,
            4 => ReactionIndicator.Glowing,
            5 or 6 => ReactionIndicator.Smoking,
            _ => ReactionIndicator.Calm,
        };
    }

    private HashSet<string>? _generatedNameCache;

    private string GenerateName()
    {
        _generatedNameCache ??= _prototype.EnumeratePrototypes<ReagentPrototype>().Select(r => r.LocalizedName).ToHashSet();

        string name;
        do
        {
            name = _random.Pick(Prefix) + _random.Pick(WordRoot) + _random.Pick(Suffix);
        } while (_generatedNameCache.Contains(name));

        _generatedNameCache.Add(name);
        return name;
    }

    private (int Overdose, int CriticalOverdose) RollOverdose(int tier)
    {
        var overdoseMultiplier = tier switch
        {
            1 => _random.Next(1, 3),
            2 => _random.Next(4, 7),
            _ => _random.Next(6, 10),
        };

        var overdose = 5 + 5 * overdoseMultiplier;
        var criticalOverdose = overdose + 5;

        var attempts = _random.Next(1, 4);
        for (var i = 0; i < attempts; i++)
        {
            if (_random.Prob(0.20f + 0.02f * tier))
                criticalOverdose += 5;
        }

        return (overdose, criticalOverdose);
    }

    private List<(ChemGeneratorPropertyPrototype Property, int Level)> PickProperties(int tier)
    {
        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>()
            .Where(p => !p.Flags.HasFlag(ChemPropertyType.Disabled))
            .ToList();

        // mutually exclusive if/else chain keyed on rarity -
        // a rare  property only ever lands in the rare pool, never in the ordinary
        // positive/negative/neutral pools, so it's only reachable via the forced first property
        // mechanism below rather than leaking into normal weighted category rolls too
        var rarePool = pool.Where(p => p.Rarity == ChemPropertyRarity.Rare).ToList();
        var positive = pool.Where(p => p.Category == ChemPropertyCategory.Positive && p.Rarity != ChemPropertyRarity.Rare).ToList();
        var negative = pool.Where(p => p.Category == ChemPropertyCategory.Negative && p.Rarity != ChemPropertyRarity.Rare).ToList();
        var neutral = pool.Where(p => p.Category == ChemPropertyCategory.Neutral && p.Rarity != ChemPropertyRarity.Rare).ToList();

        var propertiesBuff = _random.Next(3, 5);
        if (tier == 2)
            propertiesBuff -= 2;
        var count = tier + propertiesBuff;

        var properties = new List<(ChemGeneratorPropertyPrototype, int)>();
        var genValue = 0;
        var forceNegative = false;

        for (var i = 1; i <= count; i++)
        {
            if (i == 1)
            {
                List<ChemGeneratorPropertyPrototype>? forcePool = null;
                var trackFirst = true;
                if (tier > 2 && rarePool.Count > 0)
                {
                    forcePool = rarePool;
                    trackFirst = false;
                }
                else if (tier > 1 && rarePool.Count > 0 && _random.Prob(0.20f))
                {
                    forcePool = rarePool;
                    forceNegative = true;
                }

                genValue = AddRandomProperty(properties, pool, forcePool, negative, neutral, positive, tier, 0, false, trackFirst);
                continue;
            }

            if (genValue == tier * 2 + 2)
                break;

            var valueOffset = tier - genValue - 1;
            genValue += AddRandomProperty(properties, pool, null, negative, neutral, positive, tier, valueOffset, forceNegative, tier < 3);
        }

        while (properties.Count < tier + 1)
            AddRandomProperty(properties, pool, null, negative, neutral, positive, tier, 0, false);

        return properties;
    }

    private int AddRandomProperty(
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        List<ChemGeneratorPropertyPrototype> pool,
        List<ChemGeneratorPropertyPrototype>? forcePool,
        List<ChemGeneratorPropertyPrototype> negative,
        List<ChemGeneratorPropertyPrototype> neutral,
        List<ChemGeneratorPropertyPrototype> positive,
        int tier,
        int valueOffset,
        bool forceNegative,
        bool trackAdded = false)
    {
        List<ChemGeneratorPropertyPrototype> from;
        if (forcePool is { Count: > 0 })
        {
            from = forcePool;
        }
        else if (forceNegative && negative.Count > 0)
        {
            from = negative;
        }
        else if (valueOffset > 0 && positive.Count > 0)
        {
            from = positive;
        }
        else if (valueOffset < 0)
        {
            from = _random.Next(0, 100) < tier * 10 && negative.Count > 0 ? negative : neutral;
        }
        else
        {
            var (negativeChance, neutralChance) = tier switch
            {
                1 => (0.40f, 0.10f),
                2 => (0.35f, 0.10f),
                _ => (0.15f, 0.10f),
            };
            var roll = _random.NextFloat();
            from = roll <= negativeChance ? negative : roll <= negativeChance + neutralChance ? neutral : positive;
        }

        if (from.Count == 0)
            from = pool;
        if (from.Count == 0)
            return 0;

        var property = _random.Pick(from);

        if (trackAdded)
        {
            var category = property.Category switch
            {
                ChemPropertyCategory.Negative => negative,
                ChemPropertyCategory.Neutral => neutral,
                _ => positive,
            };

            var checks = 0;
            while (!CheckGeneratedProperty(property, category) && checks < 4 && category.Count > 0)
            {
                checks++;
                property = _random.Pick(category);
            }
        }

        var level = Math.Clamp(RollLevel(), 1, Math.Min(tier + 3, property.MaxLevel));

        var newValue = property.Category switch
        {
            ChemPropertyCategory.Negative => -level,
            ChemPropertyCategory.Neutral => -(int) Math.Ceiling(level / 2.0),
            _ => level,
        };

        InsertProperty(properties, property, level);
        return newValue;
    }

    // cm rejects a repeat within its category until every property in
    // that category has been generated at least once this round, then repeats are allowed freely.
    private bool CheckGeneratedProperty(ChemGeneratorPropertyPrototype property, List<ChemGeneratorPropertyPrototype> categoryPool)
    {
        var generated = _generatedProperties[property.Category];
        if (generated.Contains(property.ID) && generated.Count < categoryPool.Count)
            return false;

        generated.Add(property.ID);
        return true;
    }

    private int RollLevel()
    {
        var roll = _random.Next(0, 101);
        return roll switch
        {
            <= 20 => 1,
            <= 40 => 2,
            <= 60 => 3,
            <= 75 => 4,
            <= 85 => 5,
            <= 90 => 6,
            <= 95 => 7,
            _ => 8,
        };
    }

    // Jank asf but idk what else to do
    private Dictionary<ChemClass, List<string>>? _classPoolCache;

    private Dictionary<ChemClass, List<string>> GetClassPools()
    {
        if (_classPoolCache != null)
            return _classPoolCache;

        var pools = new Dictionary<ChemClass, List<string>>();
        foreach (ChemClass c in Enum.GetValues<ChemClass>())
            pools[c] = new List<string>();

        foreach (var r in _prototype.EnumeratePrototypes<ReagentPrototype>())
        {
            if (r.Abstract || r.Unknown || r.NoGeneration)
                continue;

            pools[r.ChemClass].Add(r.ID);
        }

        _classPoolCache = pools;
        return pools;
    }

    private void AppendToClassPoolCache(ChemClass chemClass, string id)
    {
        _classPoolCache?[chemClass].Add(id);
    }

    private Dictionary<ChemClass, List<string>> BuildClassPools(params string[] exclude)
    {
        var cached = GetClassPools();
        if (exclude.Length == 0)
            return cached;

        var excludeSet = exclude.ToHashSet();
        var pools = new Dictionary<ChemClass, List<string>>();
        foreach (var (chemClass, ids) in cached)
            pools[chemClass] = ids.Where(id => !excludeSet.Contains(id)).ToList();

        return pools;
    }

    private List<string> RollIngredientPool(Dictionary<ChemClass, List<string>> pools, int tier)
    {
        var roll = _random.Next(0, 101);
        return tier switch
        {
            1 => roll <= 60 ? pools[ChemClass.Basic] : roll <= 80 ? pools[ChemClass.Common] : pools[ChemClass.Uncommon],
            2 => roll <= 50 ? pools[ChemClass.Common] : roll <= 75 ? pools[ChemClass.Uncommon] : pools[ChemClass.Rare],
            _ => roll <= 80 ? pools[_random.Prob(0.5f) ? ChemClass.Basic : ChemClass.Common] : pools[ChemClass.Hydro],
        };
    }

    // cm vial/random: rolls a real reagent id from the same tiered class pools used for
    // generator ingredients, with a small flat chance of a Special tier (xeno) reagent instead -
    // Special is never included in RollIngredientPool's own tier table, same as cms C1-C6
    // tiers excluding its separate xenogenic bonus roll.
    // 4% to get special RMCXenogeneticCatalyst (not implemented fully yet)
    private const float VialSpecialChance = 0.04f;
    private static readonly ProtoId<ReagentPrototype> XenogeneticCatalyst = "RMCXenogeneticCatalyst";

    public string RollRandomKnownReagent()
    {
        if (_random.Prob(VialSpecialChance))
            return XenogeneticCatalyst.Id;

        var special = GetClassPools()[ChemClass.Special].Where(id => id != XenogeneticCatalyst.Id).ToList();

        return special.Count > 0 ? _random.Pick(special) : string.Empty;
    }

    // cms reroll_chemicals() uses its own, simpler tier table for the contract ingredient hint -
    // distinct from add_component's recipe-ingredient table above. Tier 3 always hints a hydro exclusive.
    private List<string> RollContractHintPool(Dictionary<ChemClass, List<string>> pools, int tier)
    {
        var roll = _random.Next(0, 101);
        return tier switch
        {
            1 => roll <= 60 ? pools[ChemClass.Basic] : pools[ChemClass.Common],
            2 => roll <= 40 ? pools[ChemClass.Common] : pools[ChemClass.Uncommon],
            _ => pools[ChemClass.Hydro],
        };
    }

    private bool TryPickIngredient(Dictionary<ChemClass, List<string>> pools, List<string> allPool, int tier, HashSet<string> used, out string pick)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var pool = RollIngredientPool(pools, tier);
            if (pool.Count == 0)
                pool = allPool;
            if (pool.Count == 0)
                break;

            var candidate = _random.Pick(pool);
            if (!used.Add(candidate))
                continue;

            pick = candidate;
            return true;
        }

        pick = string.Empty;
        return false;
    }

    private List<(string Id, int Amount, bool Catalyst)> PickRecipeIngredients(int tier, string generatedId, string? forcedFirst = null)
    {
        List<(string Id, int Amount, bool Catalyst)> recipe = new();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            recipe = RollRecipeIngredientsOnce(tier, generatedId, forcedFirst);
            if (recipe.Count == 0)
                continue;

            if (!IsAllMedical(recipe) && !DuplicatesExistingReaction(recipe))
                return recipe;
        }

        return recipe;
    }

    private bool IsAllMedical(List<(string Id, int Amount, bool Catalyst)> recipe)
    {
        var any = false;
        foreach (var (id, _, catalyst) in recipe)
        {
            if (catalyst)
                continue;

            any = true;
            if (!_rmcReagent.TryIndex(id, out var reagent) || !reagent.Medical)
                return false;
        }

        return any;
    }

    private bool DuplicatesExistingReaction(List<(string Id, int Amount, bool Catalyst)> recipe)
    {
        var reagents = recipe.Where(i => !i.Catalyst).Select(i => i.Id).ToHashSet();
        if (reagents.Count == 0)
            return false;

        foreach (var reaction in _prototype.EnumeratePrototypes<ReactionPrototype>())
        {
            if (reagents.All(r => reaction.Reactants.ContainsKey(r)))
                return true;
        }

        foreach (var (_, cached) in _recipeReactantsCache)
        {
            var existing = cached.Where(i => !i.Catalyst).Select(i => i.Id).ToHashSet();
            if (reagents.All(r => existing.Contains(r)))
                return true;
        }

        return false;
    }

    private List<(string Id, int Amount, bool Catalyst)> RollRecipeIngredientsOnce(int tier, string generatedId, string? forcedFirst = null)
    {
        var pools = BuildClassPools(generatedId);
        var allPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();
        var used = new HashSet<string>();

        var desired = tier == 1 ? 3 : _random.Next(3, 5);

        var result = new List<(string, int, bool)>();
        for (var i = 0; i < desired; i++)
        {
            string pick;
            if (i == 0 && forcedFirst != null)
            {
                pick = forcedFirst;
                used.Add(pick);
            }
            else if (!TryPickIngredient(pools, allPool, tier, used, out pick))
            {
                break;
            }

            var amount = 1;
            if (i == 0)
            {
                amount = _random.Next(0, 101) switch
                {
                    <= 60 => 1,
                    <= 75 => 2,
                    <= 85 => 3,
                    <= 92 => 4,
                    <= 97 => 5,
                    _ => 6,
                };
            }

            result.Add((pick, amount, false));
        }

        // prob(20) && gen_tier >= 2 adds one extra catalyst component, always modifier 5.
        if (tier >= 2 && _random.Prob(0.20f) && TryPickIngredient(pools, allPool, tier, used, out var catalystPick))
            result.Add((catalystPick, 5, true));

        return result;
    }

    private static readonly string[] PhysicalDescriptions =
    [
        "nondescript", "oily", "cloudy", "syrupy", "crystalline", "metallic", "pungent", "viscous", "gaseous",
        "shiny", "powdery",
    ];


    public List<(ChemGeneratorPropertyPrototype Property, int Level)> GetProperties(Reagent reagent)
    {
        var result = new List<(ChemGeneratorPropertyPrototype, int)>();
        if (reagent.Metabolisms is not { } metabolisms || !metabolisms.TryGetValue("Poison", out var entry))
            return result;

        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>().ToList();
        foreach (var effect in entry.Effects)
        {
            var property = pool.FirstOrDefault(p => p.Effect.GetType() == effect.GetType());
            if (property == null)
                continue;

            var level = effect is RMCChemicalEffect rmcEffect ? (int) rmcEffect.Potency : 1;
            result.Add((property, level));
        }

        return result;
    }

    public EntityUid PrintReport(EntityCoordinates coords, ProtoId<ReagentPrototype> reagentId, Reagent reagent, int sampleNumber = 0, string? category = null, bool simulatorReport = false)
    {
        var isGenerated = IsGenerated(reagentId);

        var hasTier = TryGetGeneratedTier(reagentId, out var tier);
        var tierClassified = isGenerated && hasTier && !_research.HasSufficientClearance(tier);

        var xClassified = !isGenerated && reagent.ChemClass >= ChemClass.Special && !_research.HasXAccess();

        var classified = tierClassified || xClassified;

        var properties = classified ? new List<(ChemGeneratorPropertyPrototype Property, int Level)>() : GetProperties(reagent);
        var report = Spawn(ReportProto, coords);

        var comp = EnsureComp<RMCChemResearchReportComponent>(report);
        comp.ReagentId = reagentId;
        comp.Properties = properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList();
        comp.Overdose = (int) (reagent.Overdose ?? FixedPoint2.Zero);
        comp.CriticalOverdose = (int) (reagent.CriticalOverdose ?? FixedPoint2.Zero);
        comp.Completed = !classified;
        comp.IsGenerated = isGenerated;
        Dirty(report, comp);

        _metaData.SetEntityName(report, $"research report ({reagent.LocalizedName})");

        var sb = new StringBuilder();
        if (simulatorReport)
            RMCChemPaperFormat.AppendHeader(sb, "Official Company Document", "Simulated Synthesis Report");
        else
            RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document");

        sb.AppendLine();
        sb.AppendLine($"[bold][head=3]Result for {reagent.LocalizedName}[/head][/bold]");

        if (classified)
        {
            sb.AppendLine();
            if (tierClassified)
            {
                sb.AppendLine($"[bold]CLASSIFIED: Clearance Level {tier} Required[/bold]");
                sb.AppendLine();
                sb.AppendLine("Formula and composition data withheld pending clearance elevation.");
            }
            else
            {
                sb.AppendLine("[bold]CLASSIFIED: 5X Clearance Required[/bold]");
                sb.AppendLine();
                sb.AppendLine("This sample's molecular structure exceeds standard clearance parameters. Elevated access is required to view its properties.");
            }
            sb.AppendLine();
            RMCChemPaperFormat.AppendFooter(sb, simulatorReport
                ? "This report was automatically printed by the Synthesis Simulator."
                : "This report was automatically printed by the A-XRF Scanner.");
            _paper.SetContent(report, RMCChemPaperFormat.Wrap(sb));

            if (category != null)
            {
                var classifiedTitle = sampleNumber > 0 ? $"{sampleNumber} - {reagent.LocalizedName}" : reagent.LocalizedName;
                _research.SaveDocument(category, report, classifiedTitle);
            }

            return report;
        }

        sb.AppendLine();
        sb.AppendLine($"ID: {reagent.LocalizedName}");
        if (properties.Count > 0)
        {
            var key = string.Join(" ", properties.Select(p => $"{(string.IsNullOrEmpty(p.Property.Code) ? p.Property.ID : p.Property.Code)}{p.Level}"));
            sb.AppendLine($"[color=#4A90E2]{key}[/color]");
        }

        sb.AppendLine();
        if (properties.Count == 0)
        {
            sb.AppendLine($"[font size={RMCChemPaperFormat.PropertiesFontSize}][italic]None detected[/italic][/font]");
        }
        else
        {
            var entries = properties.Select(p => $"[bold]{p.Property.ID} Level {p.Level}[/bold] - {p.Property.Description}");
            sb.AppendLine($"[font size={RMCChemPaperFormat.PropertiesFontSize}]{string.Join("\n\n", entries)}[/font]");
        }

        sb.AppendLine();
        sb.AppendLine("Chemical has the following reaction indicators:");
        var indicator = _reactionIndicators.TryGetValue(reagentId.Id, out var rollAndIndicator) ? rollAndIndicator : ReactionIndicator.Calm;
        sb.AppendLine(GetIndicatorText(indicator));

        sb.AppendLine();
        sb.AppendLine($"Overdose threshold: {comp.Overdose} units");
        sb.AppendLine($"Critical overdose: {comp.CriticalOverdose} units");
        sb.AppendLine($"Standard duration multiplier: {GetDurationMultiplier(reagent)}x");

        sb.AppendLine();
        AppendComposition(sb, reagentId, reagent);

        if (RMCChemPaperFormat.IsVolatile(properties))
        {
            sb.AppendLine();
            sb.AppendLine("[bold]WARNING: UNSTABLE REAGENT. MIX CAREFULLY.[/bold]");
        }

        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, simulatorReport
            ? "This report was automatically printed by the Synthesis Simulator."
            : "This report was automatically printed by the A-XRF Scanner.");

        _paper.SetContent(report, RMCChemPaperFormat.Wrap(sb));

        if (category != null)
        {
            var title = sampleNumber > 0 ? $"{sampleNumber} - {reagent.LocalizedName}" : reagent.LocalizedName;
            _research.SaveDocument(category, report, title);
        }

        return report;
    }

    private static string GetIndicatorText(ReactionIndicator indicator)
    {
        return indicator switch
        {
            ReactionIndicator.Bubbling => "[bold]Aggressive foaming[/bold]\n- The reaction causes bubbling and foam to build up rapidly and shoot out of the beaker. Biological Suit gives complete protection.",
            ReactionIndicator.Glowing => "[bold]Luminescence[/bold]\n- The reaction produces light, power of the light is dictated by the amount mixed.",
            ReactionIndicator.Fire => "[bold]Exothermic[/bold]\n- The reaction produces heat and will cause a small scale combustion. This will not compromise the contents of the beaker.",
            ReactionIndicator.Smoking => "[bold]Fuming[/bold]\n- The reaction produces heavy fumes from contents of the beaker. Work under a fume hood, wear a gas mask, or simply put an airtight seal over the beaker.",
            ReactionIndicator.Endothermic => "[bold]Endothermic[/bold]\n- The reaction is endothermic. This slows down the mixing process significantly and stops all other reactions from happening.",
            _ => "[bold]Inert[/bold]\n- The reaction has no indicators.",
        };
    }

    private static string GetDurationMultiplier(ReagentPrototype reagent)
    {
        const float standardRate = 0.1f;

        if (reagent.Metabolisms is not { } metabolisms ||
            !metabolisms.TryGetValue("Poison", out var entry) ||
            entry.MetabolismRate <= FixedPoint2.Zero)
        {
            return "1";
        }

        var multiplier = standardRate / (float) entry.MetabolismRate;
        return multiplier.ToString("0.##");
    }

    private void AppendComposition(StringBuilder sb, ProtoId<ReagentPrototype> reagentId, ReagentPrototype reagent)
    {
        sb.AppendLine("Composition Details:");

        var reaction = _prototype.EnumeratePrototypes<ReactionPrototype>().FirstOrDefault(r => r.Products.ContainsKey(reagentId.Id));
        if (reaction != null)
        {
            AppendReactants(sb, reaction.Reactants.Where(kv => !kv.Value.Catalyst));

            var catalysts = reaction.Reactants.Where(kv => kv.Value.Catalyst).ToList();
            if (catalysts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Reaction would require the following catalysts:");
                AppendReactants(sb, catalysts);
            }
        }
        else if (reagent.ChemClass == ChemClass.Basic)
        {
            sb.AppendLine($"- {reagent.LocalizedName}");
        }
        else
        {
            sb.AppendLine("[italic]ERROR: Unable to analyze emission spectrum of sample.[/italic]");
        }
    }

    private void AppendReactants(StringBuilder sb, IEnumerable<KeyValuePair<string, ReactantPrototype>> reactants)
    {
        foreach (var (ingredientId, reactant) in reactants)
        {
            if (TryGetChemClass(ingredientId, out var ingredientClass) &&
                ingredientClass >= ChemClass.Special &&
                !_research.IsIdentified(ingredientId))
            {
                sb.AppendLine("[italic]- Unknown emission spectrum[/italic]");
                continue;
            }

            var name = _rmcReagent.TryIndex(ingredientId, out var ingredientReagent) ? ingredientReagent.LocalizedName : ingredientId;
            sb.AppendLine($"- {reactant.Amount} {name}");
        }
    }

    public EntityUid PrintErrorReport(EntityCoordinates coords, string reason, int sampleNumber = 0)
    {
        var report = Spawn(ReportProto, coords);
        var comp = EnsureComp<RMCChemResearchReportComponent>(report);
        comp.Completed = false;
        Dirty(report, comp);

        _metaData.SetEntityName(report, "research report (ERROR)");

        var sb = new StringBuilder();
        RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Reagent Analysis Print");
        sb.AppendLine();
        sb.AppendLine("[bold][head=3]Analysis ERROR[/head][/bold]");
        sb.AppendLine();
        sb.AppendLine(sampleNumber > 0
            ? $"Result: Analysis failed for sample #{sampleNumber}."
            : "Result: Analysis failed.");
        sb.AppendLine();
        sb.AppendLine("Reason for error:");
        sb.AppendLine($"[italic]{reason}[/italic]");
        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, "This report was automatically printed by the A-XRF Scanner.");

        _paper.SetContent(report, RMCChemPaperFormat.Wrap(sb));

        return report;
    }

    public void InsertProperty(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties, ChemGeneratorPropertyPrototype property, int level)
    {
        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>().ToDictionary(p => p.ID);
        var propertyId = property.ID;
        string? initialPropertyId = null;

        foreach (var (existing, existingLevel) in properties.ToArray())
        {
            ChemGeneratorPropertyPrototype? match = null;

            if (existing.ID == propertyId)
            {
                match = existing;
            }
            else
            {
                foreach (var (comboId, pieces) in ChemPropertyRelations.Combines.Concat(_legendaryCombines))
                {
                    if (!pieces.Contains(propertyId) || !pieces.Contains(existing.ID))
                        continue;

                    var satisfied = pieces.Count(p => p == propertyId || properties.Any(x => x.Property.ID == p));
                    if (satisfied < pieces.Count)
                        continue;

                    initialPropertyId = propertyId;
                    propertyId = comboId;
                    level = Math.Max(Math.Max(level - existingLevel, existingLevel - level), 1);

                    foreach (var (r, rLevel) in properties.ToArray())
                    {
                        if (!pieces.Contains(r.ID) || r.Flags.HasFlag(ChemPropertyType.Catalyst))
                            continue;

                        var newLevel = rLevel - level;
                        var idx = properties.FindIndex(p => p.Property.ID == r.ID);
                        if (newLevel <= 0)
                            properties.RemoveAt(idx);
                        else
                            properties[idx] = (r, newLevel);
                    }

                    break;
                }

                if (match == null)
                {
                    foreach (var (a, b) in ChemPropertyRelations.Conflicts)
                    {
                        if ((propertyId == a && existing.ID == b) || (propertyId == b && existing.ID == a))
                        {
                            match = existing;
                            break;
                        }
                    }
                }
            }

            if (match == null)
                continue;

            var matchIdx = properties.FindIndex(p => p.Property.ID == match.ID);
            if (matchIdx == -1)
                continue;

            var matchLevel = properties[matchIdx].Level;
            if (matchLevel > level)
            {
                properties[matchIdx] = (match, matchLevel - level);
                return;
            }

            if (matchLevel < level)
            {
                level -= matchLevel;
                properties.RemoveAt(matchIdx);
            }
            else
            {
                properties.RemoveAt(matchIdx);
                return;
            }

            break;
        }

        if (!pool.TryGetValue(propertyId, out var finalProperty))
            return;

        level = Math.Min(level, finalProperty.MaxLevel);
        properties.Add((finalProperty, level));

        if (initialPropertyId != null &&
            initialPropertyId != propertyId &&
            pool.TryGetValue(initialPropertyId, out var initialProperty) &&
            initialProperty.Flags.HasFlag(ChemPropertyType.Catalyst))
        {
            properties.Add((initialProperty, level));
        }
    }

    private readonly Dictionary<string, List<(string Id, int Amount, bool Catalyst)>> _recipeReactantsCache = new();

    private void CacheRecipeReactants(string reagentId, List<(string Id, int Amount, bool Catalyst)> ingredients)
    {
        if (ingredients.Count > 0)
            _recipeReactantsCache[reagentId] = ingredients;
    }

    public bool HasExistingRecipe(string reagentId) => TryGetRecipeReactants(reagentId, out _);

    public bool TryGetRecipeReactants(string reagentId, out List<(string Id, int Amount, bool Catalyst)> reactants)
    {
        if (_recipeReactantsCache.TryGetValue(reagentId, out var cached))
        {
            reactants = cached;
            return true;
        }

        var existing = _prototype.EnumeratePrototypes<ReactionPrototype>()
            .FirstOrDefault(r => r.Products.ContainsKey(reagentId));

        if (existing == null || existing.Reactants.Count == 0)
        {
            reactants = new();
            return false;
        }

        reactants = existing.Reactants.Select(kv => (Id: kv.Key, Amount: (int) kv.Value.Amount, kv.Value.Catalyst)).ToList();
        return true;
    }

    public bool IsDuplicateRecipe(List<(string Id, int Amount, bool Catalyst)> a, List<(string Id, int Amount, bool Catalyst)> b)
    {
        if (a.Count != b.Count)
            return false;

        var aKeys = a.Select(x => $"{x.Id}:{x.Amount}:{x.Catalyst}").OrderBy(x => x, StringComparer.Ordinal);
        var bKeys = b.Select(x => $"{x.Id}:{x.Amount}:{x.Catalyst}").OrderBy(x => x, StringComparer.Ordinal);
        return aKeys.SequenceEqual(bKeys);
    }

    public string ComputeSimulationSignature(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties, int overdose)
    {
        var sorted = properties
            .OrderBy(p => p.Property.ID, StringComparer.Ordinal)
            .Select(p => $"{p.Property.ID}:{p.Level}");
        return string.Join("|", sorted) + $"#{overdose}";
    }

    public bool TryGetSimulationResult(string signature, out ProtoId<ReagentPrototype> reagentId) =>
        _simulationResults.TryGetValue(signature, out reagentId);

    public void RegisterSimulationResult(string signature, ProtoId<ReagentPrototype> reagentId) =>
        _simulationResults[signature] = reagentId;

    public (List<(string Id, int Amount, bool Catalyst)> Ingredients, string? NewIngredientId) RollRecipeCandidate(string baseReagentId, int tier, string newId)
    {
        if (!_recipeReactantsCache.TryGetValue(baseReagentId, out var cachedReactants))
        {
            var existing = _prototype.EnumeratePrototypes<ReactionPrototype>()
                .FirstOrDefault(r => r.Products.ContainsKey(baseReagentId));

            if (existing == null || existing.Reactants.Count == 0)
                return (PickRecipeIngredients(tier, newId), null);

            cachedReactants = existing.Reactants.Select(kv => (Id: kv.Key, Amount: (int) kv.Value.Amount, kv.Value.Catalyst)).ToList();
        }

        var reactants = new List<(string Id, int Amount, bool Catalyst)>(cachedReactants);

        var swapTier = Math.Max(tier - 1, 1);
        var pools = BuildClassPools([newId, ..reactants.Select(x => x.Id)]);
        var allPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();

        string? newIngredient = null;
        if (TryPickIngredient(pools, allPool, swapTier, new HashSet<string>(), out var picked))
        {
            newIngredient = picked;

            var amount = _random.Next(0, 65) switch
            {
                < 30 => 1,
                < 45 => 2,
                < 60 => 3,
                _ => 4,
            };

            var swapIndex = _random.Next(reactants.Count);
            reactants[swapIndex] = (newIngredient, amount, reactants[swapIndex].Catalyst);
        }

        return (reactants, newIngredient);
    }

    public bool TryGetChemClass(string reagentId, out ChemClass chemClass)
    {
        if (_rmcReagent.TryIndex(reagentId, out var reagent))
        {
            chemClass = reagent.ChemClass;
            return true;
        }

        chemClass = ChemClass.None;
        return false;
    }

    public ProtoId<ReagentPrototype> GenerateVariant(
        Reagent baseReagent,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> newProperties,
        int tier,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        int newOverdose)
    {
        var suffix = string.Concat(newProperties.Select(p => $" {(string.IsNullOrEmpty(p.Property.Code) ? p.Property.ID : p.Property.Code)}{p.Level}"));
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(suffix)))[..2];

        var name = $"{baseReagent.LocalizedName} {hash}";
        var id = $"{baseReagent.ID}Variant{++_generatedCount}{hash}";
        var color = baseReagent.SubstanceColor.ToHexNoAlpha();

        var criticalOverdose = Math.Max(Math.Min(newOverdose * 2, newOverdose + 30), 10);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, newProperties, newOverdose, criticalOverdose, ingredients, ChemClass.Rare, indicator);
        AppendToClassPoolCache(ChemClass.Rare, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _originalIds[id] = baseReagent.ID;
        _reactionIndicators[id] = indicator;

        return id;
    }
}
