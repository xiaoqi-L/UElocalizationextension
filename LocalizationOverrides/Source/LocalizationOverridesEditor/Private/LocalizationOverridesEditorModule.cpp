#include "LocalizationOverridesGenerator.h"
#include "HAL/IConsoleManager.h"
#include "Modules/ModuleManager.h"

namespace
{
void GenerateLocalizationOverrideFiles()
{
	if (FLocalizationOverridesGenerator::GenerateFromProjectLocalization())
	{
		UE_LOG(LogTemp, Display, TEXT("LocalizationOverrides.Generate completed successfully."));
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("LocalizationOverrides.Generate failed; see LogLocalizationOverridesGenerator for details."));
	}
}
}

class FLocalizationOverridesEditorModule : public IModuleInterface
{
public:
	virtual void StartupModule() override
	{
		GenerateCommand = IConsoleManager::Get().RegisterConsoleCommand(
			TEXT("LocalizationOverrides.Generate"),
			TEXT("Generates LocalizationOverrides JSON files from project localization manifest and archive files. This is an explicit operation and does not run automatically at editor startup."),
			FConsoleCommandDelegate::CreateStatic(&GenerateLocalizationOverrideFiles),
			ECVF_Default);
	}

	virtual void ShutdownModule() override
	{
		if (GenerateCommand)
		{
			IConsoleManager::Get().UnregisterConsoleObject(GenerateCommand);
			GenerateCommand = nullptr;
		}
	}

private:
	IConsoleObject* GenerateCommand = nullptr;
};

IMPLEMENT_MODULE(FLocalizationOverridesEditorModule, LocalizationOverridesEditor)
