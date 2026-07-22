#pragma once

#include "Kismet/BlueprintFunctionLibrary.h"
#include "LocalizationOverridesBlueprintLibrary.generated.h"

UCLASS()
class LOCALIZATIONOVERRIDES_API ULocalizationOverridesBlueprintLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintCallable, Category = "Localization Overrides")
	static bool ReloadLocalizationOverrides();

	UFUNCTION(BlueprintCallable, Category = "Localization Overrides")
	static bool SetGameCulture(const FString& Culture, bool bSaveAsDefaultCulture = false);

	UFUNCTION(BlueprintPure, Category = "Localization Overrides")
	static TArray<FString> GetAvailableCultures();

	UFUNCTION(BlueprintPure, Category = "Localization Overrides")
	static FString GetLocalizationOverridesDirectory();

	UFUNCTION(BlueprintPure, Category = "Localization Overrides")
	static FString GetCurrentGameCulture();

	UFUNCTION(BlueprintCallable, Category = "Localization Overrides")
	static bool GenerateLocalizationOverrideFiles();
};
