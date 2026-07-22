using UnrealBuildTool;

public class LocalizationOverridesEditor : ModuleRules
{
	public LocalizationOverridesEditor(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PrecompileForTargets = PrecompileTargetsType.Editor;

		PrivateDependencyModuleNames.AddRange(new[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"LocalizationOverrides",
			"UnrealEd"
		});
	}
}

