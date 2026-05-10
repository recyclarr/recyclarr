using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Disposables.Fluent;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class ReviewStepView : WizardStepViewBase<ReviewViewModel>
{
    public ReviewStepView(ReviewViewModel viewModel)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Review your configuration");

        var scrollArea = new View
        {
            Y = Pos.Bottom(question) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 2,
            CanFocus = false,
            ViewportSettings = ViewportSettingsFlags.AllowYGreaterThanContentHeight,
        };

        var hint = CreateHint(
            "YAML generation coming soon. Press Enter to finish.",
            Pos.Bottom(scrollArea) + 1
        );
        Add(question, scrollArea, hint);

        viewModel
            .WhenAnyValue(x => x.Sections)
            .Subscribe(sections => RebuildSummary(scrollArea, sections))
            .DisposeWith(Disposables);
    }

    private static void RebuildSummary(View container, IReadOnlyList<ReviewSection> sections)
    {
        container.RemoveAll();

        // Track Y position as absolute int for SetContentSize
        var currentY = 0;

        foreach (var section in sections)
        {
            var header = new Label
            {
                Text = section.Header,
                Y = currentY,
                SchemeName = WizardSchemes.Question,
            };
            container.Add(header);
            currentY++;

            foreach (var item in section.Items)
            {
                if (item.IsSubHeader)
                {
                    var subHeader = new Label
                    {
                        Text = $"  {item.Value}",
                        Y = currentY,
                        SchemeName = WizardSchemes.HintText,
                    };
                    container.Add(subHeader);
                }
                else if (item.Label is { } label)
                {
                    // Key-value pair: label in default color, value in accent
                    var keyLabel = new Label { Text = $"  {label} ", Y = currentY };
                    var valueLabel = new Label
                    {
                        Text = item.Value,
                        X = Pos.Right(keyLabel),
                        Y = currentY,
                        SchemeName = WizardSchemes.ReviewValue,
                    };
                    container.Add(keyLabel, valueLabel);
                }
                else
                {
                    // Value-only row: white (no label to contrast against)
                    var valueLabel = new Label { Text = $"  {item.Value}", Y = currentY };
                    container.Add(valueLabel);
                }

                currentY++;
            }

            // Blank line between sections
            currentY++;
        }

        container.SetContentSize(new Size(container.Viewport.Width, currentY));
    }
}
