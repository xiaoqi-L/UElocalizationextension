using System;
using System.IO;
using System.Linq;
using System.Text;
using UnrealBuildTool;

public class LocalizationOverrides : ModuleRules
{
	public LocalizationOverrides(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PrecompileForTargets = PrecompileTargetsType.Any;

		PublicDependencyModuleNames.AddRange(new[]
		{
			"Core",
			"CoreUObject",
			"Engine"
		});

		PrivateDependencyModuleNames.AddRange(new[] { "Json" });

		AddProjectLocalizationOverrides(Target);
	}

	private void AddProjectLocalizationOverrides(ReadOnlyTargetRules Target)
	{
		// BuildPlugin precompiles this module without a consuming project. Only a
		// real game project can provide project-specific localization JSON files.
		if (Target.ProjectFile == null ||
			(Target.Type != TargetType.Game && Target.Type != TargetType.Client && Target.Type != TargetType.Server))
		{
			return;
		}

		string projectDirectory = Target.ProjectFile.Directory.FullName;
		// BuildPlugin creates a minimal temporary HostProject with no Content or
		// Config directory. It is only a compilation harness and must not be
		// treated as the project which owns localization data.
		bool isBuildPluginHost =
			Path.GetFileNameWithoutExtension(Target.ProjectFile.FullName).Equals("HostProject", StringComparison.OrdinalIgnoreCase) &&
			!Directory.Exists(Path.Combine(projectDirectory, "Content")) &&
			!Directory.Exists(Path.Combine(projectDirectory, "Config"));
		if (isBuildPluginHost)
		{
			return;
		}

		string overridesDirectory = Path.Combine(projectDirectory, "LocalizationOverrides");
		if (!Directory.Exists(overridesDirectory))
		{
			throw new BuildException("LocalizationOverrides directory was not found: {0}", overridesDirectory);
		}

		string[] jsonFiles = Directory.GetFiles(overridesDirectory, "*", SearchOption.TopDirectoryOnly)
			.Where(file => Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
			.Where(file => !file.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) &&
				!file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
			.OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		string languagesFile = jsonFiles.FirstOrDefault(file =>
			Path.GetFileName(file).Equals("languages.json", StringComparison.OrdinalIgnoreCase));
		if (languagesFile == null)
		{
			throw new BuildException("LocalizationOverrides/languages.json was not found in {0}", overridesDirectory);
		}

		if (!jsonFiles.Any(file => !Path.GetFileName(file).Equals("languages.json", StringComparison.OrdinalIgnoreCase)))
		{
			throw new BuildException("No localization target JSON files were found in {0}", overridesDirectory);
		}

		foreach (string jsonFile in jsonFiles)
		{
			ValidateJsonFile(jsonFile);
			string stagedPath = Path.Combine("$(TargetOutputDir)", "LocalizationOverrides", Path.GetFileName(jsonFile));
			RuntimeDependencies.Add(stagedPath, jsonFile, StagedFileType.NonUFS);
		}
	}

	private static void ValidateJsonFile(string filename)
	{
		byte[] bytes = File.ReadAllBytes(filename);
		if (bytes.Length < 2 || bytes[0] != 0xFF || bytes[1] != 0xFE)
		{
			throw new BuildException("Localization JSON must be UTF-16 LE with BOM: {0}", filename);
		}

		string json = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
		JsonSyntaxValidator validator = new JsonSyntaxValidator(json);
		if (!validator.ValidateObject(out string error))
		{
			throw new BuildException("Invalid localization JSON '{0}': {1}", filename, error);
		}
	}

	private sealed class JsonSyntaxValidator
	{
		private readonly string Text;
		private int Position;

		public JsonSyntaxValidator(string text)
		{
			Text = text ?? String.Empty;
		}

		public bool ValidateObject(out string error)
		{
			SkipWhitespace();
			if (!ParseObject())
			{
				error = "expected a valid JSON object at character " + Position;
				return false;
			}

			SkipWhitespace();
			if (Position != Text.Length)
			{
				error = "unexpected content at character " + Position;
				return false;
			}

			error = String.Empty;
			return true;
		}

		private bool ParseValue()
		{
			SkipWhitespace();
			if (Position >= Text.Length)
			{
				return false;
			}

			switch (Text[Position])
			{
				case '{': return ParseObject();
				case '[': return ParseArray();
				case '"': return ParseString();
				case 't': return Consume("true");
				case 'f': return Consume("false");
				case 'n': return Consume("null");
				default: return ParseNumber();
			}
		}

		private bool ParseObject()
		{
			if (!ConsumeCharacter('{'))
			{
				return false;
			}

			SkipWhitespace();
			if (ConsumeCharacter('}'))
			{
				return true;
			}

			while (true)
			{
				SkipWhitespace();
				if (!ParseString())
				{
					return false;
				}
				SkipWhitespace();
				if (!ConsumeCharacter(':') || !ParseValue())
				{
					return false;
				}
				SkipWhitespace();
				if (ConsumeCharacter('}'))
				{
					return true;
				}
				if (!ConsumeCharacter(','))
				{
					return false;
				}
			}
		}

		private bool ParseArray()
		{
			if (!ConsumeCharacter('['))
			{
				return false;
			}

			SkipWhitespace();
			if (ConsumeCharacter(']'))
			{
				return true;
			}

			while (true)
			{
				if (!ParseValue())
				{
					return false;
				}
				SkipWhitespace();
				if (ConsumeCharacter(']'))
				{
					return true;
				}
				if (!ConsumeCharacter(','))
				{
					return false;
				}
			}
		}

		private bool ParseString()
		{
			if (!ConsumeCharacter('"'))
			{
				return false;
			}

			while (Position < Text.Length)
			{
				char character = Text[Position++];
				if (character == '"')
				{
					return true;
				}
				if (character < 0x20)
				{
					return false;
				}
				if (character != '\\')
				{
					continue;
				}

				if (Position >= Text.Length)
				{
					return false;
				}
				char escaped = Text[Position++];
				if (escaped == 'u')
				{
					for (int index = 0; index < 4; ++index)
					{
						if (Position >= Text.Length || !IsHexDigit(Text[Position++]))
						{
							return false;
						}
					}
				}
				else if ("\"\\/bfnrt".IndexOf(escaped) < 0)
				{
					return false;
				}
			}

			return false;
		}

		private bool ParseNumber()
		{
			int start = Position;
			ConsumeCharacter('-');
			if (ConsumeCharacter('0'))
			{
				if (Position < Text.Length && Char.IsDigit(Text[Position]))
				{
					return false;
				}
			}
			else if (!ConsumeDigits())
			{
				return false;
			}

			if (ConsumeCharacter('.') && !ConsumeDigits())
			{
				return false;
			}
			if (Position < Text.Length && (Text[Position] == 'e' || Text[Position] == 'E'))
			{
				++Position;
				if (Position < Text.Length && (Text[Position] == '+' || Text[Position] == '-'))
				{
					++Position;
				}
				if (!ConsumeDigits())
				{
					return false;
				}
			}

			return Position > start;
		}

		private bool ConsumeDigits()
		{
			int start = Position;
			while (Position < Text.Length && Char.IsDigit(Text[Position]))
			{
				++Position;
			}
			return Position > start;
		}

		private bool Consume(string value)
		{
			if (Position + value.Length > Text.Length ||
				!Text.Substring(Position, value.Length).Equals(value, StringComparison.Ordinal))
			{
				return false;
			}
			Position += value.Length;
			return true;
		}

		private bool ConsumeCharacter(char expected)
		{
			if (Position >= Text.Length || Text[Position] != expected)
			{
				return false;
			}
			++Position;
			return true;
		}

		private void SkipWhitespace()
		{
			while (Position < Text.Length && Char.IsWhiteSpace(Text[Position]))
			{
				++Position;
			}
		}

		private static bool IsHexDigit(char value)
		{
			return (value >= '0' && value <= '9') ||
				(value >= 'a' && value <= 'f') ||
				(value >= 'A' && value <= 'F');
		}
	}
}

