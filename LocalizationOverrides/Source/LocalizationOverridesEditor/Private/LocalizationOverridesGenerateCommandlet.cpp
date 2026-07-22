#include "LocalizationOverridesGenerateCommandlet.h"

#include "LocalizationOverridesGenerator.h"

ULocalizationOverridesGenerateCommandlet::ULocalizationOverridesGenerateCommandlet()
{
	IsClient = false;
	IsEditor = true;
	IsServer = false;
	LogToConsole = true;
	ShowErrorCount = true;
}

int32 ULocalizationOverridesGenerateCommandlet::Main(const FString& Params)
{
	UE_LOG(LogTemp, Display, TEXT("Running LocalizationOverrides.Generate in commandlet mode."));
	if (!FLocalizationOverridesGenerator::GenerateFromProjectLocalization())
	{
		UE_LOG(LogTemp, Error, TEXT("LocalizationOverrides.Generate failed."));
		return 1;
	}

	UE_LOG(LogTemp, Display, TEXT("LocalizationOverrides.Generate completed successfully."));
	return 0;
}
