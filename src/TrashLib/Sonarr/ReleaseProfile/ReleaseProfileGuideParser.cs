using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Extensions;
using Flurl;
using Flurl.Http;
using Serilog;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile
{
    internal class ReleaseProfileGuideParser : IReleaseProfileGuideParser
    {
        private readonly Dictionary<ReleaseProfileType, string> _markdownDocNames = new()
        {
            {ReleaseProfileType.Anime, "Sonarr-Release-Profile-RegEx-Anime"},
            {ReleaseProfileType.Series, "Sonarr-Release-Profile-RegEx"}
        };

        private readonly (TermCategory, Regex)[] _regexCategories =
        {
            (TermCategory.Required, BuildRegex(@"must contain")),
            (TermCategory.Ignored, BuildRegex(@"must not contain")),
            (TermCategory.Preferred, BuildRegex(@"preferred"))
        };

        private readonly Regex _regexHeader = new(@"^(#+)\s(.+?)\s*$", RegexOptions.Compiled);
        private readonly Regex _regexHeaderReleaseProfile = BuildRegex(@"release profile");
        private readonly Regex _regexPotentialScore = BuildRegex(@"\[(-?[\d]+)\]");
        private readonly Regex _regexScore = BuildRegex(@"score.*?\[(-?[\d]+)\]");

        public ReleaseProfileGuideParser(ILogger logger)
        {
            Log = logger;
        }

        private ILogger Log { get; }

        public async Task<string> GetMarkdownData(ReleaseProfileType profileName)
        {
            return await BuildUrl(profileName).GetStringAsync();
        }

        public IDictionary<string, ProfileData> ParseMarkdown(ReleaseProfileConfig config, string markdown)
        {
            var state = new ParserState(Log);

            var reader = new StringReader(markdown);
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                state.LineNumber++;
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Always check if we're starting a fenced code block. Whether we are inside one or not greatly affects
                // the logic we use.
                if (line.StartsWith("```"))
                {
                    state.InsideCodeBlock = !state.InsideCodeBlock;
                    continue;
                }

                // Not inside brackets
                if (!state.InsideCodeBlock)
                {
                    OutsideFence_ParseMarkdown(line, state);
                }
                // Inside brackets
                else
                {
                    if (!state.IsValid)
                    {
                        Log.Debug("  - !! Inside bracket with invalid state; skipping! " +
                                  "[Profile Name: {ProfileName}] " +
                                  "[Category: {Category}] " + "[Score: {Score}] " + "[Line: {Line}] ",
                            state.ProfileName,
                            state.CurrentCategory.Value, state.Score, line);
                    }
                    else
                    {
                        InsideFence_ParseMarkdown(config, line, state);
                    }
                }
            }

            Log.Debug("\n");
            return state.Results;
        }

        private bool IsSkippableLine(string line)
        {
            // Skip lines with leading whitespace (i.e. indentation).
            // These lines will almost always be `!!! attention` blocks of some kind and won't contain useful data.
            if (char.IsWhiteSpace(line, 0))
            {
                Log.Debug("  - Skip Indented Line: {Line}", line);
                return true;
            }

            // Lines that begin with `???` or `!!!` are admonition syntax (extended markdown supported by Python)
            if (line.StartsWith("!!!") || line.StartsWith("???"))
            {
                Log.Debug("  - Skip Admonition: {Line}", line);
                return true;
            }

            return false;
        }

        private static Regex BuildRegex(string regex)
        {
            return new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private Url BuildUrl(ReleaseProfileType profileName)
        {
            return "https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr".AppendPathSegment(
                $"{_markdownDocNames[profileName]}.md");
        }

        private void InsideFence_ParseMarkdown(ReleaseProfileConfig config, string line, ParserState state)
        {
            // Sometimes a comma is present at the end of these lines, because when it's
            // pasted into Sonarr it acts as a delimiter. However, when using them with the
            // API we do not need them.
            line = line.TrimEnd(',');

            var category = state.CurrentCategory.Value;
            switch (category!.Value)
            {
                case TermCategory.Preferred:
                {
                    Log.Debug("    + Capture Term " +
                              "[Category: {CurrentCategory}] " +
                              "[Optional: {Optional}] " +
                              "[Score: {Score}] " +
                              "[Strict: {StrictNegativeScores}] " +
                              "[Term: {Line}]",
                        category.Value, state.TermsAreOptional.Value, state.Score, config.StrictNegativeScores, line);

                    if (config.StrictNegativeScores && state.Score < 0)
                    {
                        state.IgnoredTerms.Add(line);
                    }
                    else
                    {
                        // Score is already checked for null prior to the method being invoked.
                        var prefList = state.PreferredTerms.GetOrCreate(state.Score!.Value);
                        prefList.Add(line);
                    }

                    break;
                }

                case TermCategory.Ignored:
                {
                    state.IgnoredTerms.Add(line);
                    Log.Debug("    + Capture Term " +
                              "[Category: {Category}] " +
                              "[Optional: {Optional}] " +
                              "[Term: {Line}]",
                        category.Value, state.TermsAreOptional.Value, line);
                    break;
                }

                case TermCategory.Required:
                {
                    state.RequiredTerms.Add(line);
                    Log.Debug("    + Capture Term " +
                              "[Category: {Category}] " +
                              "[Optional: {Optional}] " +
                              "[Term: {Line}]",
                        category.Value, state.TermsAreOptional.Value, line);
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException($"Unknown term category: {category.Value}");
                }
            }
        }

        private void OutsideFence_ParseMarkdown(string line, ParserState state)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            Match match;

            // Header Processing. Never do any additional processing to headers, so return after processing it
            if (_regexHeader.Match(line, out match))
            {
                OutsideFence_ParseHeader(state, match);
                return;
            }

            // Until we find a header that defines a profile, we don't care about anything under it.
            if (string.IsNullOrEmpty(state.ProfileName))
            {
                return;
            }

            // These are often found in admonition (indented) blocks, so we check for it before we
            // run the IsSkippableLine() check.
            if (line.ContainsIgnoreCase("include preferred"))
            {
                state.GetProfile().IncludePreferredWhenRenaming = !line.ContainsIgnoreCase("not");
                Log.Debug("  - 'Include Preferred' found [Value: {IncludePreferredWhenRenaming}] [Line: {Line}]",
                    state.GetProfile().IncludePreferredWhenRenaming, line);
                return;
            }

            if (IsSkippableLine(line))
            {
                return;
            }

            OutsideFence_ParseInformationOnSameLine(line, state);
        }

        private void OutsideFence_ParseHeader(ParserState state, Match match)
        {
            var headerDepth = match.Groups[1].Length;
            var headerText = match.Groups[2].Value;
            state.CurrentHeaderDepth = headerDepth;

            // Always reset the scope-based state any time we see a header, regardless of depth or phrasing.
            // Each header "resets" scope-based state, even if it's entering into a nested header, which usually will
            // not reset as much state.
            state.ResetScopeState(headerDepth);

            Log.Debug("> Parsing Header [Nested: {Nested}] [Depth: {HeaderDepth}] [Text: {HeaderText}]",
                headerDepth > state.ProfileHeaderDepth, headerDepth, headerText);

            // Profile name (always reset previous state here)
            if (_regexHeaderReleaseProfile.Match(headerText).Success)
            {
                state.ResetParserState();
                state.ProfileName = headerText;
                state.ProfileHeaderDepth = headerDepth;
                Log.Debug("  - New Profile [Text: {HeaderText}]", headerText);
            }
            else if (headerDepth <= state.ProfileHeaderDepth)
            {
                Log.Debug("  - !! Non-nested, non-profile header found; resetting all state");
                state.ResetParserState();
            }

            // If a single header can be parsed with multiple phrases, add more if conditions below this comment.
            // In order to make sure all checks happen as needed, do not return from the condition (to allow conditions
            // below it to be executed)

            // Another note: Any "state" set by headers has longer lasting effects. That state will remain in effect
            // until the next header. That means multiple fenced code blocks will be impacted.

            ParseAndSetOptional(headerText, state);
            ParseAndSetCategory(headerText, state);
        }

        private void OutsideFence_ParseInformationOnSameLine(string line, ParserState state)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            Match match;

            ParseAndSetOptional(line, state);
            ParseAndSetCategory(line, state);

            if (_regexScore.Match(line, out match))
            {
                // As a convenience, if we find a score, we obviously should set the category to Preferred even if
                // the guide didn't explicitly mention that.
                state.CurrentCategory.PushValue(TermCategory.Preferred, state.CurrentHeaderDepth);

                state.Score = int.Parse(match.Groups[1].Value);
                Log.Debug("  - Score [Value: {Score}]", state.Score);
            }
            else if (_regexPotentialScore.Match(line, out match))
            {
                Log.Warning("Found a potential score on line #{Line} that will be ignored because the " +
                            "word 'score' is missing (This is probably a bug in the guide itself): {ScoreMatch}",
                    state.LineNumber, match.Groups[0].Value);
            }
        }

        private void ParseAndSetCategory(string line, ParserState state)
        {
            var category = ParseCategory(line);
            if (category == null)
            {
                return;
            }

            state.CurrentCategory.PushValue(category.Value, state.CurrentHeaderDepth);

            Log.Debug("  - Category Set " +
                      "[Scope: {Scope}] " +
                      "[Name: {Category}] " +
                      "[Stack Size: {StackSize}] " +
                      "[Line: {Line}]",
                category.Value, state.CurrentHeaderDepth, state.CurrentCategory.StackSize, line);
        }

        private void ParseAndSetOptional(string line, ParserState state)
        {
            if (line.ContainsIgnoreCase("optional"))
            {
                state.TermsAreOptional.PushValue(true, state.CurrentHeaderDepth);

                Log.Debug("  - Optional Set " +
                          "[Scope: {Scope}] " +
                          "[Stack Size: {StackSize}] " +
                          "[Line: {Line}]",
                    state.CurrentHeaderDepth, state.CurrentCategory.StackSize, line);
            }
        }

        private TermCategory? ParseCategory(string line)
        {
            foreach (var (category, regex) in _regexCategories)
            {
                if (regex.Match(line).Success)
                {
                    return category;
                }
            }

            return null;
        }
    }
}
