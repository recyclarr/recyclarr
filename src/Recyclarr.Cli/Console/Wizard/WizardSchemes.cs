using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Recyclarr.Cli.Console.Wizard;

internal static class WizardSchemes
{
    // -- Color palette --
    // Accent: teal for focused/active elements, selection highlights
    private static readonly Color Accent = new(0x5F, 0xAF, 0xAF);

    // Secondary: blue for headers, informational highlights
    private static readonly Color Secondary = new(0x5F, 0x87, 0xAF);

    // Text hierarchy
    private static readonly Color Foreground = new(0xCD, 0xD6, 0xF4); // bright text
    private static readonly Color Muted = new(0x7F, 0x84, 0x9C); // secondary text
    private static readonly Color Faint = new(0x58, 0x5B, 0x70); // hints, disabled

    // Surfaces: elevated backgrounds for dialog overlays
    private static readonly Color Surface = new(0x31, 0x32, 0x44);

    // Borders: dim by default, accent on focus
    private static readonly Color BorderDim = new(0x45, 0x47, 0x5A);
    private static readonly Color BorderActive = Accent;

    // Semantic (used sparingly)
    private static readonly Color Success = new(0xA6, 0xE3, 0xA1);
    private static readonly Color Error = new(0xF3, 0x8B, 0xA8);

    // Scheme names
    public const string Panel = "Panel";
    public const string PanelInactive = "PanelInactive";
    public const string HintText = "HintText";
    public const string Question = "Question";
    public const string NavHint = "NavHint";
    public const string NavKey = "NavKey";
    public const string ProgressCurrent = "ProgressCurrent";
    public const string ProgressDone = "ProgressDone";
    public const string ProgressFuture = "ProgressFuture";
    public const string ConfirmDialog = "ConfirmDialog";
    public const string ConfirmDialogButton = "ConfirmDialogButton";

    public static void Register()
    {
        // Active/focused panel: accent-colored borders, bright text
        SchemeManager.AddScheme(
            Panel,
            new Scheme
            {
                Normal = new Attribute(Foreground, Color.None),
                Focus = new Attribute(Accent, Color.None),
                HotNormal = new Attribute(BorderActive, Color.None),
                HotFocus = new Attribute(BorderActive, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Inactive panel: dim borders, muted text
        SchemeManager.AddScheme(
            PanelInactive,
            new Scheme
            {
                Normal = new Attribute(Muted, Color.None),
                Focus = new Attribute(Muted, Color.None),
                HotNormal = new Attribute(BorderDim, Color.None),
                HotFocus = new Attribute(BorderDim, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Question text: secondary (blue) for visual hierarchy
        SchemeManager.AddScheme(
            Question,
            new Scheme
            {
                Normal = new Attribute(Secondary, Color.None),
                Focus = new Attribute(Secondary, Color.None),
                HotNormal = new Attribute(Secondary, Color.None),
                HotFocus = new Attribute(Secondary, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Hint text below inputs
        SchemeManager.AddScheme(
            HintText,
            new Scheme
            {
                Normal = new Attribute(Muted, Color.None),
                Focus = new Attribute(Muted, Color.None),
                HotNormal = new Attribute(Muted, Color.None),
                HotFocus = new Attribute(Muted, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Navigation hint bar: key names (brighter) vs descriptions (dim)
        SchemeManager.AddScheme(
            NavKey,
            new Scheme
            {
                Normal = new Attribute(Muted, Color.None),
                Focus = new Attribute(Muted, Color.None),
                HotNormal = new Attribute(Muted, Color.None),
                HotFocus = new Attribute(Muted, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        SchemeManager.AddScheme(
            NavHint,
            new Scheme
            {
                Normal = new Attribute(Faint, Color.None),
                Focus = new Attribute(Faint, Color.None),
                HotNormal = new Attribute(Faint, Color.None),
                HotFocus = new Attribute(Faint, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Progress sidebar: current step (accent, bold)
        SchemeManager.AddScheme(
            ProgressCurrent,
            new Scheme
            {
                Normal = new Attribute(Accent, Color.None),
                Focus = new Attribute(Accent, Color.None),
                HotNormal = new Attribute(Accent, Color.None),
                HotFocus = new Attribute(Accent, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Progress sidebar: completed step (green checkmark)
        SchemeManager.AddScheme(
            ProgressDone,
            new Scheme
            {
                Normal = new Attribute(Success, Color.None),
                Focus = new Attribute(Success, Color.None),
                HotNormal = new Attribute(Success, Color.None),
                HotFocus = new Attribute(Success, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Progress sidebar: future step (very dim)
        SchemeManager.AddScheme(
            ProgressFuture,
            new Scheme
            {
                Normal = new Attribute(Faint, Color.None),
                Focus = new Attribute(Faint, Color.None),
                HotNormal = new Attribute(Faint, Color.None),
                HotFocus = new Attribute(Faint, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );

        // Confirmation dialog: elevated surface background, accent border
        SchemeManager.AddScheme(
            ConfirmDialog,
            new Scheme
            {
                Normal = new Attribute(Foreground, Surface),
                Focus = new Attribute(Accent, Surface),
                HotNormal = new Attribute(Accent, Surface),
                HotFocus = new Attribute(Accent, Surface),
                Disabled = new Attribute(Faint, Surface),
            }
        );

        // Dialog buttons: inverted accent highlight on focus for clear contrast
        SchemeManager.AddScheme(
            ConfirmDialogButton,
            new Scheme
            {
                Normal = new Attribute(Foreground, Surface),
                Focus = new Attribute(Surface, Accent),
                HotNormal = new Attribute(Foreground, Surface),
                HotFocus = new Attribute(Surface, Accent),
                Disabled = new Attribute(Faint, Surface),
            }
        );

        // Override built-in Error scheme for validation messages
        SchemeManager.AddScheme(
            "Error",
            new Scheme
            {
                Normal = new Attribute(Error, Color.None),
                Focus = new Attribute(Error, Color.None),
                HotNormal = new Attribute(Error, Color.None),
                HotFocus = new Attribute(Error, Color.None),
                Disabled = new Attribute(Faint, Color.None),
            }
        );
    }
}
