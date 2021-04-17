using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Serilog;
using Trash.Extensions;

namespace Trash.Sonarr.ReleaseProfile
{
    public class ReleaseProfileGuideParser : IReleaseProfileGuideParser
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

        private readonly Regex _regexHeader = new(@"^(#+)\s([\w\s\d]+)\s*$", RegexOptions.Compiled);
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
            var results = new Dictionary<string, ProfileData>();
            var state = new ParserState();

            var reader = new StringReader(markdown);
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                state.LineNumber++;
                if (IsSkippableLine(line))
                {
                    continue;
                }

                // Always check if we're starting a fenced code block. Whether we are inside one or not greatly affects
                // the logic we use.
                if (line.StartsWith("```"))
                {
                    state.BracketDepth = 1 - state.BracketDepth;
                    continue;
                }

                // Not inside brackets
                if (state.BracketDepth == 0)
                {
                    ParseMarkdownOutsideFence(line, state, results);
                }
                // Inside brackets
                else if (state.BracketDepth == 1)
                {
                    if (!state.IsValid)
                    {
                        Log.Debug("  - !! Inside bracket with invalid state; skipping! " +
                                  "[Profile Name: {ProfileName}] " +
                                  "[Category: {Category}] " + "[Score: {Score}] " + "[Line: {Line}] ",
                            state.ProfileName,
                            state.CurrentCategory, state.Score, line);
                    }
                    else
                    {
                        ParseMarkdownInsideFence(config, line, state, results);
                    }
                }
            }

            Log.Debug("\n");
            return results;
        }

        private bool IsSkippableLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return true;
            }

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
            return new(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private Url BuildUrl(ReleaseProfileType profileName)
        {
            return "https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/V3".AppendPathSegment(
                $"{_markdownDocNames[profileName]}.md");
        }

        private void ParseMarkdownInsideFence(ReleaseProfileConfig config, string line, ParserState state,
            IDictionary<string, ProfileData> results)
        {
            // ProfileName is verified for validity prior to this method being invoked.
            // The actual check occurs in the call to ParserState.IsValid.
            var profile = results.GetOrCreate(state.ProfileName!);

            // Sometimes a comma is present at the end of these lines, because when it's
            // pasted into Sonarr it acts as a delimiter. However, when using them with the
            // API we do not need them.
            line = line.TrimEnd(',');

            switch (state.CurrentCategory)
            {
                case TermCategory.Preferred:
                {
                    Log.Debug("    + Capture Term " + "[Category: {CurrentCategory}] " + "[Score: {Score}] " +
                              "[Strict: {StrictNegativeScores}] " + "[Term: {Line}]", state.CurrentCategory,
                        state.Score,
                        config.StrictNegativeScores, line);

                    if (config.StrictNegativeScores && state.Score < 0)
                    {
                        profile.Ignored.Add(line);
                    }
                    else
                    {
                        // Score is already checked for null prior to the method being invoked.
                        var prefList = profile.Preferred.GetOrCreate(state.Score!.Value);
                        prefList.Add(line);
                    }

                    break;
                }

                case TermCategory.Ignored:
                {
                    profile.Ignored.Add(line);
                    Log.Debug("    + Capture Term [Category: {Category}] [Term: {Line}]", state.CurrentCategory, line);
                    break;
                }

                case TermCategory.Required:
                {
                    profile.Required.Add(line);
                    Log.Debug("    + Capture Term [Category: {Category}] [Term: {Line}]", state.CurrentCategory, line);
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException($"Unknown term category: {state.CurrentCategory}");
                }
            }
        }

        private void ParseMarkdownOutsideFence(string line, ParserState state, IDictionary<string, ProfileData> results)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            Match match;

            // Header Processing
            if (_regexHeader.Match(line, out match))
            {
                var headerDepth = match.Groups[1].Length;
                var headerText = match.Groups[2].Value;
                Log.Debug("> Parsing Header [Text: {HeaderText}] [Depth: {HeaderDepth}]", headerText, headerDepth);

                // Profile name (always reset previous state here)
                if (_regexHeaderReleaseProfile.Match(headerText).Success)
                {
                    state.Reset();
                    state.ProfileName = headerText;
                    state.CurrentHeaderDepth = headerDepth;
                    Log.Debug("  - New Profile [Text: {HeaderText}]", headerText);
                    return;
                }

                if (headerDepth <= state.CurrentHeaderDepth)
                {
                    Log.Debug("  - !! Non-nested, non-profile header found; resetting all state");
                    state.Reset();
                    return;
                }
            }

            // Until we find a header that defines a profile, we don't care about anything under it.
            if (string.IsNullOrEmpty(state.ProfileName))
            {
                return;
            }

            var profile = results.GetOrCreate(state.ProfileName);
            if (line.ContainsIgnoreCase("include preferred"))
            {
                profile.IncludePreferredWhenRenaming = !line.ContainsIgnoreCase("not");
                Log.Debug("  - 'Include Preferred' found [Value: {IncludePreferredWhenRenaming}] [Line: {Line}]",
                    profile.IncludePreferredWhenRenaming, line);
                return;
            }

            // Either we have a nested header or normal line at this point.
            // We need to check if we're defining a new category.
            var category = ParseCategory(line);
            if (category != null)
            {
                state.CurrentCategory = category.Value;
                Log.Debug("  - Category Set [Name: {Category}] [Line: {Line}]", category, line);
                // DO NOT RETURN HERE!
                // The category and score are sometimes in the same sentence (line); continue processing the line!
                // return;
            }

            if (_regexScore.Match(line, out match))
            {
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

        private enum TermCategory
        {
            Required,
            Ignored,
            Preferred
        }

        private class ParserState
        {
            public ParserState()
            {
                Reset();
            }

            public string? ProfileName { get; set; }
            public int? Score { get; set; }
            public TermCategory CurrentCategory { get; set; }
            public int BracketDepth { get; set; }
            public int CurrentHeaderDepth { get; set; }
            public int LineNumber { get; set; }

            public bool IsValid => ProfileName != null && (CurrentCategory != TermCategory.Preferred || Score != null);

            public void Reset()
            {
                ProfileName = null;
                Score = null;
                CurrentCategory = TermCategory.Preferred;
                BracketDepth = 0;
                CurrentHeaderDepth = -1;
            }
        }
    }
}
