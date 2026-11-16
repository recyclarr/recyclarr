using System.Globalization;
using Microsoft.Extensions.Configuration;
using Recyclarr.Config.EnvironmentVariables;
using Recyclarr.Config.Secrets;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Recyclarr.Settings.Configuration;

/// <summary>
/// Loads <c>settings.yml</c> as configuration keys under
/// <see cref="SettingsConfiguration.SectionName"/>.
/// </summary>
/// <remarks>
/// This is a parser only: it knows YAML, not the settings model. Each snake_case key segment
/// becomes a PascalCase segment so the configuration binder matches C# property names
/// (<c>resource_providers</c> becomes <c>ResourceProviders</c>); sequence items use their index.
/// Product rules (removed keys, unknown keys, value types) run over the result in
/// <see cref="SettingsBinder"/>. The provider never throws: a file it cannot parse yields no keys
/// and an error in <see cref="Report"/>, so assembling a host never fails on user data.
/// </remarks>
internal sealed class SettingsYamlConfigurationSource : FileConfigurationSource
{
    public required SettingsLoadReport Report { get; init; }
    public required SettingsYamlTags Tags { get; init; }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new Provider(this);
    }

    private sealed class Provider(SettingsYamlConfigurationSource source)
        : FileConfigurationProvider(source)
    {
        private SettingsLoadReport Report => source.Report;

        public override void Load()
        {
            // The base class skips Load(Stream) when the optional file is missing.
            Report.Replace([], []);
            base.Load();
        }

        public override void Load(Stream stream)
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var positions = new Dictionary<string, SettingsPosition>(
                StringComparer.OrdinalIgnoreCase
            );
            var diagnostics = new List<SettingsDiagnostic>();

            try
            {
                var root = Parse(stream);
                if (root is YamlMappingNode mapping)
                {
                    new Flattener(data, positions, diagnostics, source).Mapping(
                        mapping,
                        SettingsConfiguration.SectionName,
                        ""
                    );
                }
                else if (root is not null and not YamlScalarNode { Value: null or "" })
                {
                    diagnostics.Add(Report.Error("the file must contain a mapping of settings"));
                }
            }
            catch (YamlException e)
            {
                data.Clear();
                positions.Clear();
                diagnostics.Add(
                    Report.Error(
                        $"invalid YAML: {Reason(e)}",
                        new SettingsPosition(
                            "",
                            "",
                            SettingsNodeKind.Scalar,
                            (int)e.Start.Line,
                            (int)e.Start.Column
                        )
                    )
                );
            }

            Data = data;
            Report.Replace(positions, diagnostics);
        }

        private static YamlNode? Parse(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var yaml = new YamlStream();
            yaml.Load(reader);
            return yaml.Documents.FirstOrDefault()?.RootNode;
        }

        // YamlDotNet prefixes messages with the positions this diagnostic already carries.
        private static string Reason(YamlException e)
        {
            var message = e.Message;
            var start = message.IndexOf("): ", StringComparison.Ordinal);
            return start < 0 ? message : message[(start + 3)..];
        }
    }

    private sealed class Flattener(
        Dictionary<string, string?> data,
        Dictionary<string, SettingsPosition> positions,
        List<SettingsDiagnostic> diagnostics,
        SettingsYamlConfigurationSource source
    )
    {
        public void Mapping(YamlMappingNode node, string path, string yamlPath)
        {
            foreach (var (keyNode, value) in node.Children)
            {
                var key = (keyNode as YamlScalarNode)?.Value ?? keyNode.ToString();
                var childPath = ConfigurationPath.Combine(path, PascalCase(key));
                var childYamlPath = yamlPath.Length == 0 ? key : $"{yamlPath}.{key}";
                Node(value, childPath, childYamlPath, key, keyNode.Start);
            }
        }

        private void Node(YamlNode node, string path, string yamlPath, string yamlKey, Mark mark)
        {
            var kind = node switch
            {
                YamlMappingNode => SettingsNodeKind.Mapping,
                YamlSequenceNode => SettingsNodeKind.Sequence,
                _ => SettingsNodeKind.Scalar,
            };
            var position = new SettingsPosition(
                yamlPath,
                yamlKey,
                kind,
                (int)mark.Line,
                (int)mark.Column
            );
            positions[path] = position;

            switch (node)
            {
                case YamlMappingNode mapping:
                    Mapping(mapping, path, yamlPath);
                    break;

                case YamlSequenceNode sequence:
                    for (var i = 0; i < sequence.Children.Count; i++)
                    {
                        var item = sequence.Children[i];
                        var index = i.ToString(CultureInfo.InvariantCulture);
                        Node(
                            item,
                            ConfigurationPath.Combine(path, index),
                            $"{yamlPath}[{index}]",
                            index,
                            item.Start
                        );
                    }

                    break;

                case YamlScalarNode scalar:
                    data[path] = Scalar(scalar, position);
                    break;
            }
        }

        private string? Scalar(YamlScalarNode scalar, SettingsPosition position)
        {
            if (scalar.Tag.IsEmpty)
            {
                return IsNull(scalar) ? null : scalar.Value;
            }

            try
            {
                return source.Tags.Resolve(scalar.Tag.Value, scalar.Value ?? "", scalar.Start.Line);
            }
            catch (Exception e)
                when (e
                        is IOException
                            or UnauthorizedAccessException
                            or EnvironmentVariableNotDefinedException
                            or SecretNotFoundException
                            or YamlException
                )
            {
                diagnostics.Add(
                    source.Report.Error($"'{position.YamlPath}': {e.Message}", position)
                );
                return null;
            }
        }

        private static bool IsNull(YamlScalarNode scalar)
        {
            return scalar.Style == ScalarStyle.Plain
                && scalar.Value is null or "" or "~" or "null" or "Null" or "NULL";
        }

        private static string PascalCase(string key)
        {
            return string.Concat(
                key.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
            );
        }
    }
}
