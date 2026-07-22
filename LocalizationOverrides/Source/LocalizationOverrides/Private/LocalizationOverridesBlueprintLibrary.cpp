#include "LocalizationOverridesBlueprintLibrary.h"

#include "LocalizationOverridesGenerator.h"
#include "LocalizationOverridesSubsystem.h"

bool ULocalizationOverridesBlueprintLibrary::ReloadLocalizationOverrides()
{
	return FLocalizationOverridesSubsystem::ReloadOverrides();
}

bool ULocalizationOverridesBlueprintLibrary::SetGameCulture(const FString& Culture, bool bSaveAsDefaultCulture)
{
	return FLocalizationOverridesSubsystem::SetGameCulture(Culture, bSaveAsDefaultCulture);
}

TArray<FString> ULocalizationOverridesBlueprintLibrary::GetAvailableCultures()
{
	return FLocalizationOverridesSubsystem::GetAvailableCultures();
}

FString ULocalizationOverridesBlueprintLibrary::GetLocalizationOverridesDirectory()
{
	return FLocalizationOverridesSubsystem::GetOverridesDirectory();
}

FString ULocalizationOverridesBlueprintLibrary::GetCurrentGameCulture()
{
	return FLocalizationOverridesSubsystem::GetCurrentGameCulture();
}

bool ULocalizationOverridesBlueprintLibrary::GenerateLocalizationOverrideFiles()
{
#if WITH_EDITOR
	return FLocalizationOverridesGenerator::GenerateFromProjectLocalization();
#else
	return false;
#endif
}
