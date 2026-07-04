using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaperComponent : Component
{
    [DataField]
    public Color TextColor = new(25, 25, 25);
    [DataField]
    public Color Color = Color.White;

    [DataField]
    public int Thickness = 20;

    public PaperAction Mode;
    [DataField("content")]
    public string Content { get; set; } = "";

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 10000;

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState")]
    public string? StampState { get; set; }

    [DataField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    // RMC14
    [DataField]
    public ProtoId<LanguagePrototype>? Language;
    // RMC14

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;

        public PaperBoundUserInterfaceState(List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            StampedBy = stampedBy;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperSignatureRequestMessage : BoundUserInterfaceMessage
    {
        public readonly int SignatureIndex;

        public PaperSignatureRequestMessage(int signatureIndex)
        {
            SignatureIndex = signatureIndex;
        }
    }

    // RMC14
    [Serializable, NetSerializable]
    public sealed class PaperComponentState : IComponentState
    {
        public Color TextColor;
        public Color Color;
        public int Thickness;
        public string Content = string.Empty;
        public List<StampDisplayInfo> StampedBy = new();
        public string? StampState;
        public bool EditingDisabled;
        public ProtoId<LanguagePrototype>? Language;
    }
    // RMC14

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}
