#pragma once

#include "Containers/Array.h"
#include "Containers/UnrealString.h"

class FLocalizationOverridesSubsystem
{
public:
	/** Applies the default culture before game content is loaded. Must run on the Game Thread. */
	static void Initialize();
	/** Registers JSON overrides once per file version. Must run on the Game Thread. */
	static bool ReloadOverrides();
	/** Changes the current culture and optionally writes defaultCulture. Must run on the Game Thread. */
	static bool SetGameCulture(const FString& Culture, bool bSaveAsDefaultCulture);
	static TArray<FString> GetAvailableCultures();
	static FString GetOverridesDirectory();
	static FString FindOverridesDirectory();
	static FString GetCurrentGameCulture();

private:
	static bool LoadLanguages(const FString& OverridesDirectory, TArray<FString>& OutCultures, FString* OutDefaultCulture = nullptr);
	static bool SaveDefaultCulture(const FString& Culture);
	static bool LoadTargetOverrides(const FString& Filename, const TArray<FString>& Cultures);
};
