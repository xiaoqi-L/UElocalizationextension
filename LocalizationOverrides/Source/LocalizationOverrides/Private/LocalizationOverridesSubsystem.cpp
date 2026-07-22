#include "LocalizationOverridesSubsystem.h"

#include "Containers/Map.h"
#include "Containers/Set.h"
#include "CoreGlobals.h"
#include "HAL/PlatformProcess.h"
#include "Internationalization/Culture.h"
#include "Internationalization/Internationalization.h"
#include "Internationalization/LocalizedTextSourceTypes.h"
#include "Internationalization/PolyglotTextData.h"
#include "Internationalization/TextLocalizationManager.h"
#include "Kismet/KismetInternationalizationLibrary.h"
#include "Misc/CommandLine.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "Misc/SecureHash.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

DEFINE_LOG_CATEGORY_STATIC(LogLocalizationOverrides, Log, All);

namespace
{
const TCHAR* OverridesFolderName = TEXT("LocalizationOverrides");
const TCHAR* LanguagesFileName = TEXT("languages.json");

TMap<FString, FMD5Hash> RegisteredOverrideFileHashes;
TSet<FString> RegisteredOverrideFiles;
TArray<FString> RegisteredCultures;
bool bHasRegisteredOverrides = false;

bool IsGameThreadCall(const TCHAR* FunctionName)
{
	if (!IsInGameThread())
	{
		UE_LOG(LogLocalizationOverrides, Error, TEXT("%s must be called from the Game Thread."), FunctionName);
		return false;
	}

	return true;
}

FString NormalizeDirectory(FString Directory)
{
	FPaths::NormalizeDirectoryName(Directory);
	return Directory;
}

bool LoadJsonObject(const FString& Filename, TSharedPtr<FJsonObject>& OutObject)
{
	FString JsonText;
	if (!FFileHelper::LoadFileToString(JsonText, *Filename))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Could not read '%s'."), *Filename);
		return false;
	}

	const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonText);
	if (!FJsonSerializer::Deserialize(Reader, OutObject) || !OutObject.IsValid())
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Could not parse '%s'."), *Filename);
		return false;
	}

	return true;
}

FString GetStringField(const TSharedPtr<FJsonObject>& Object, const FString& FieldName)
{
	FString Value;
	if (Object.IsValid())
	{
		Object->TryGetStringField(FieldName, Value);
	}
	return Value;
}

}

void FLocalizationOverridesSubsystem::Initialize()
{
	if (!IsGameThreadCall(TEXT("FLocalizationOverridesSubsystem::Initialize")))
	{
		return;
	}

#if WITH_EDITOR
	// The editor process itself should not force the game's startup culture.
	// Standalone Game launched by the editor runs the editor executable with
	// -game; it is a real game session and must apply the project overrides.
	const bool bIsStandaloneEditorGame = FParse::Param(FCommandLine::Get(), TEXT("game"));
	if (GIsEditor && !bIsStandaloneEditorGame)
	{
		UE_LOG(LogLocalizationOverrides, Log, TEXT("Skipping startup culture override in the editor process."));
		return;
	}
#endif

	const FString OverridesDirectory = FindOverridesDirectory();
	if (OverridesDirectory.IsEmpty())
	{
		return;
	}

	TArray<FString> Cultures;
	FString DefaultCulture;
	LoadLanguages(OverridesDirectory, Cultures, &DefaultCulture);

	// Unreal has already applied these explicit command-line settings before the
	// plugin starts. They are the final startup override.
	FString CommandLineCulture;
	const bool bHasCommandLineCulture =
		FParse::Value(FCommandLine::Get(), TEXT("culture="), CommandLineCulture) ||
		FParse::Value(FCommandLine::Get(), TEXT("language="), CommandLineCulture) ||
		FParse::Value(FCommandLine::Get(), TEXT("locale="), CommandLineCulture);
	if (bHasCommandLineCulture)
	{
		UE_LOG(LogLocalizationOverrides, Log, TEXT("Keeping UE command-line culture '%s'; it has priority over localization override settings."), *CommandLineCulture);
		return;
	}

	FString StartupCulture;
	if (Cultures.Contains(DefaultCulture))
	{
		StartupCulture = DefaultCulture;
	}

	if (!StartupCulture.IsEmpty())
	{
		FInternationalization::Get().SetCurrentLanguageAndLocale(StartupCulture);
		UE_LOG(LogLocalizationOverrides, Log, TEXT("Applied startup culture '%s' from defaultCulture."), *StartupCulture);
	}
	else
	{
		UE_LOG(LogLocalizationOverrides, Log, TEXT("No valid defaultCulture was found; keeping UE system fallback culture."));
	}
}

bool FLocalizationOverridesSubsystem::ReloadOverrides()
{
	if (!IsGameThreadCall(TEXT("FLocalizationOverridesSubsystem::ReloadOverrides")))
	{
		return false;
	}

	const FString OverridesDirectory = FindOverridesDirectory();
	if (OverridesDirectory.IsEmpty())
	{
		UE_LOG(LogLocalizationOverrides, Log, TEXT("No LocalizationOverrides directory found."));
		return false;
	}

	TArray<FString> Cultures;
	LoadLanguages(OverridesDirectory, Cultures);
	if (Cultures.IsEmpty())
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("No cultures listed in '%s'."), *(OverridesDirectory / LanguagesFileName));
	}

	TArray<FString> OverrideFiles;
	IFileManager::Get().FindFiles(OverrideFiles, *(OverridesDirectory / TEXT("*.json")), true, false);
	TSet<FString> CurrentOverrideFiles;
	for (const FString& OverrideFile : OverrideFiles)
	{
		if (!OverrideFile.Equals(LanguagesFileName, ESearchCase::IgnoreCase))
		{
			CurrentOverrideFiles.Add(OverridesDirectory / OverrideFile);
		}
	}

	if (bHasRegisteredOverrides)
	{
		if (RegisteredCultures != Cultures || CurrentOverrideFiles.Num() != RegisteredOverrideFiles.Num())
		{
			UE_LOG(LogLocalizationOverrides, Warning, TEXT("Localization override files or cultures changed while the process is running. Restart the process to apply additions or removals."));
			return true;
		}

		for (const FString& RegisteredFile : RegisteredOverrideFiles)
		{
			if (!CurrentOverrideFiles.Contains(RegisteredFile))
			{
				UE_LOG(LogLocalizationOverrides, Warning, TEXT("Localization override file '%s' was removed while the process is running. Restart the process to apply the removal."), *RegisteredFile);
				return true;
			}
		}
	}

	bool bLoadedAnyTarget = false;
	for (const FString& OverrideFile : OverrideFiles)
	{
		if (OverrideFile.Equals(LanguagesFileName, ESearchCase::IgnoreCase))
		{
			continue;
		}

		const FString FullOverrideFilename = OverridesDirectory / OverrideFile;
		const FMD5Hash CurrentHash = FMD5Hash::HashFile(*FullOverrideFilename);
		const FMD5Hash* RegisteredHash = RegisteredOverrideFileHashes.Find(FullOverrideFilename);
		if (RegisteredHash && *RegisteredHash == CurrentHash)
		{
			bLoadedAnyTarget = true;
			continue;
		}

		if (bHasRegisteredOverrides && RegisteredHash)
		{
			UE_LOG(LogLocalizationOverrides, Warning, TEXT("Localization override file '%s' changed while the process is running. Restart the process to avoid stale Polyglot entries."), *FullOverrideFilename);
			bLoadedAnyTarget = true;
			continue;
		}

		const bool bLoadedTarget = LoadTargetOverrides(FullOverrideFilename, Cultures);
		bLoadedAnyTarget |= bLoadedTarget;
		if (bLoadedTarget && CurrentHash.IsValid())
		{
			RegisteredOverrideFileHashes.Add(FullOverrideFilename, CurrentHash);
			RegisteredOverrideFiles.Add(FullOverrideFilename);
		}
	}

	if (!bLoadedAnyTarget)
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("No localization target override JSON files found in '%s'."), *OverridesDirectory);
	}

	if (bLoadedAnyTarget)
	{
		RegisteredCultures = Cultures;
		bHasRegisteredOverrides = true;

		// Polyglot registration does not itself guarantee that display strings
		// which were created before PostEngineInit are refreshed.  Explicitly
		// reload and wait here so Standalone Game starts with the selected
		// culture instead of showing the culture that UE initialized first.
		FTextLocalizationManager::Get().RefreshResources();
		FTextLocalizationManager::Get().WaitForAsyncTasks();
	}

	return bLoadedAnyTarget;
}

bool FLocalizationOverridesSubsystem::SetGameCulture(const FString& Culture, bool bSaveAsDefaultCulture)
{
	if (!IsGameThreadCall(TEXT("FLocalizationOverridesSubsystem::SetGameCulture")))
	{
		return false;
	}

	const bool bChangedCulture = UKismetInternationalizationLibrary::SetCurrentLanguageAndLocale(Culture, false);
	const bool bCultureApplied = bChangedCulture || GetCurrentGameCulture().Equals(Culture, ESearchCase::IgnoreCase);

	ReloadOverrides();
	// SetCurrentLanguageAndLocale queues localization work asynchronously. Wait
	// for it before returning so callers can immediately update their UI.
	FTextLocalizationManager::Get().WaitForAsyncTasks();

	if (!bSaveAsDefaultCulture)
	{
		return bCultureApplied;
	}

	return bCultureApplied && SaveDefaultCulture(Culture);
}

TArray<FString> FLocalizationOverridesSubsystem::GetAvailableCultures()
{
	TArray<FString> Cultures;
	const FString OverridesDirectory = FindOverridesDirectory();
	if (!OverridesDirectory.IsEmpty())
	{
		LoadLanguages(OverridesDirectory, Cultures);
	}
	return Cultures;
}

FString FLocalizationOverridesSubsystem::GetOverridesDirectory()
{
	return FindOverridesDirectory();
}

FString FLocalizationOverridesSubsystem::GetCurrentGameCulture()
{
	return FInternationalization::Get().GetCurrentLanguage()->GetName();
}

FString FLocalizationOverridesSubsystem::FindOverridesDirectory()
{
	TArray<FString> Candidates;
	// In an editor build (including Standalone Game launched from the editor),
	// use the project's editable overrides first.  A packaged/shipping build
	// is not compiled with WITH_EDITOR and therefore continues to read the
	// external directory beside the game executable first.
#if WITH_EDITOR
	Candidates.Add(FPaths::ProjectDir() / OverridesFolderName);
	Candidates.Add(FPaths::ProjectContentDir() / OverridesFolderName);
	Candidates.Add(FPaths::ProjectContentDir() / TEXT("..") / OverridesFolderName);
#endif
	// RuntimeDependencies stages the external JSON beside the actual game
	// binary. LaunchDir may instead be the directory of the bootstrap executable
	// or the caller's working directory, so BaseDir must be checked first.
#if !WITH_EDITOR
	Candidates.Add(FPlatformProcess::BaseDir() / FString(OverridesFolderName));
#endif
	Candidates.Add(FPaths::LaunchDir() / OverridesFolderName);
#if !WITH_EDITOR
	// Keep the packaged-game lookup order explicit for clarity.
	Candidates.Add(FPaths::ProjectDir() / OverridesFolderName);
	Candidates.Add(FPaths::ProjectContentDir() / OverridesFolderName);
	Candidates.Add(FPaths::ProjectContentDir() / TEXT("..") / OverridesFolderName);
#endif

	for (FString Candidate : Candidates)
	{
		Candidate = NormalizeDirectory(FPaths::ConvertRelativePathToFull(Candidate));
		if (FPaths::DirectoryExists(Candidate))
		{
			return Candidate;
		}
	}

	return FString();
}

bool FLocalizationOverridesSubsystem::LoadLanguages(const FString& OverridesDirectory, TArray<FString>& OutCultures, FString* OutDefaultCulture)
{
	TSharedPtr<FJsonObject> RootObject;
	if (!LoadJsonObject(OverridesDirectory / LanguagesFileName, RootObject))
	{
		return false;
	}

	if (OutDefaultCulture)
	{
		RootObject->TryGetStringField(TEXT("defaultCulture"), *OutDefaultCulture);
	}

	const TArray<TSharedPtr<FJsonValue>>* CulturesArray = nullptr;
	if (!RootObject->TryGetArrayField(TEXT("cultures"), CulturesArray))
	{
		return false;
	}

	for (const TSharedPtr<FJsonValue>& CultureValue : *CulturesArray)
	{
		FString CultureName;
		if (CultureValue.IsValid() && CultureValue->TryGetString(CultureName) && !CultureName.IsEmpty())
		{
			OutCultures.AddUnique(CultureName);
		}
	}

	return true;
}

bool FLocalizationOverridesSubsystem::SaveDefaultCulture(const FString& Culture)
{
	const FString OverridesDirectory = FindOverridesDirectory();
	if (OverridesDirectory.IsEmpty())
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot save default culture: no LocalizationOverrides directory was found."));
		return false;
	}

	const FString LanguagesFilename = OverridesDirectory / LanguagesFileName;
	TSharedPtr<FJsonObject> RootObject;
	if (!LoadJsonObject(LanguagesFilename, RootObject))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot save default culture: '%s' is unreadable or invalid."), *LanguagesFilename);
		return false;
	}

	const TArray<TSharedPtr<FJsonValue>>* CulturesArray = nullptr;
	if (!RootObject->TryGetArrayField(TEXT("cultures"), CulturesArray))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot save default culture: '%s' has no cultures array."), *LanguagesFilename);
		return false;
	}

	bool bCultureIsDeclared = false;
	for (const TSharedPtr<FJsonValue>& CultureValue : *CulturesArray)
	{
		FString DeclaredCulture;
		if (CultureValue.IsValid() && CultureValue->TryGetString(DeclaredCulture) && DeclaredCulture.Equals(Culture, ESearchCase::IgnoreCase))
		{
			bCultureIsDeclared = true;
			break;
		}
	}

	if (!bCultureIsDeclared)
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot save default culture '%s': it is not listed in '%s'."), *Culture, *LanguagesFilename);
		return false;
	}

	RootObject->SetStringField(TEXT("defaultCulture"), Culture);
	FString JsonText;
	const TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&JsonText);
	if (!FJsonSerializer::Serialize(RootObject.ToSharedRef(), Writer))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot serialize '%s' while saving the default culture."), *LanguagesFilename);
		return false;
	}

	const FString TemporaryFilename = LanguagesFilename + TEXT(".tmp");
	if (!FFileHelper::SaveStringToFile(JsonText, *TemporaryFilename, FFileHelper::EEncodingOptions::ForceUnicode))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot write temporary default culture file '%s'."), *TemporaryFilename);
		return false;
	}

	if (!IFileManager::Get().Move(*LanguagesFilename, *TemporaryFilename, true, true))
	{
		IFileManager::Get().Delete(*TemporaryFilename, false, true);
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("Cannot replace '%s' while saving the default culture."), *LanguagesFilename);
		return false;
	}

	UE_LOG(LogLocalizationOverrides, Log, TEXT("Saved default culture '%s' to '%s'."), *Culture, *LanguagesFilename);
	return true;
}

bool FLocalizationOverridesSubsystem::LoadTargetOverrides(const FString& Filename, const TArray<FString>& Cultures)
{
	TSharedPtr<FJsonObject> RootObject;
	if (!LoadJsonObject(Filename, RootObject))
	{
		return false;
	}

	const FString NativeCulture = GetStringField(RootObject, TEXT("nativeCulture"));
	if (NativeCulture.IsEmpty())
	{
		UE_LOG(LogLocalizationOverrides, Error, TEXT("Localization target '%s' has no valid nativeCulture; skipping it."), *Filename);
		return false;
	}

	const TArray<TSharedPtr<FJsonValue>>* Entries = nullptr;
	if (!RootObject->TryGetArrayField(TEXT("entries"), Entries))
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("No entries array found in '%s'."), *Filename);
		return false;
	}

	TArray<FPolyglotTextData> PolyglotData;
	for (const TSharedPtr<FJsonValue>& EntryValue : *Entries)
	{
		const TSharedPtr<FJsonObject>* EntryObjectPtr = nullptr;
		if (!EntryValue.IsValid() || !EntryValue->TryGetObject(EntryObjectPtr) || !EntryObjectPtr || !EntryObjectPtr->IsValid())
		{
			continue;
		}

		const TSharedPtr<FJsonObject>& EntryObject = *EntryObjectPtr;
		const FString Key = GetStringField(EntryObject, TEXT("key"));
		const FString Source = GetStringField(EntryObject, TEXT("source"));
		if (Key.IsEmpty() || Source.IsEmpty())
		{
			continue;
		}

		FPolyglotTextData EntryData(
			ELocalizedTextSourceCategory::Game,
			GetStringField(EntryObject, TEXT("namespace")),
			Key,
			Source,
			NativeCulture);
		EntryData.IsMinimalPatch(true);

		const TSharedPtr<FJsonObject>* TranslationsObjectPtr = nullptr;
		if (EntryObject->TryGetObjectField(TEXT("translations"), TranslationsObjectPtr) && TranslationsObjectPtr && TranslationsObjectPtr->IsValid())
		{
			for (const FString& EntryCulture : Cultures)
			{
				FString Translation;
				if ((*TranslationsObjectPtr)->TryGetStringField(EntryCulture, Translation) && !Translation.IsEmpty())
				{
					EntryData.AddLocalizedString(EntryCulture, Translation);
				}
			}
		}

		PolyglotData.Add(MoveTemp(EntryData));
	}

	if (PolyglotData.IsEmpty())
	{
		UE_LOG(LogLocalizationOverrides, Warning, TEXT("No valid localization override entries found in '%s'."), *Filename);
		return false;
	}

	FTextLocalizationManager::Get().RegisterPolyglotTextData(PolyglotData);
	UE_LOG(LogLocalizationOverrides, Log, TEXT("Registered %d localization override entries from '%s'."), PolyglotData.Num(), *Filename);
	return true;
}
