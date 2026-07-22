#include "LocalizationOverridesGenerator.h"

#if WITH_EDITOR

#include "Dom/JsonObject.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Guid.h"
#include "Misc/Paths.h"
#include "Policies/CondensedJsonPrintPolicy.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"

DEFINE_LOG_CATEGORY_STATIC(LogLocalizationOverridesGenerator, Log, All);

namespace
{
const TCHAR* GeneratedOverridesFolderName = TEXT("LocalizationOverrides");
const TCHAR* GeneratedLanguagesFileName = TEXT("languages.json");

struct FTextIdentity
{
	FString Namespace;
	FString Key;

	bool operator==(const FTextIdentity& Other) const
	{
		return Namespace == Other.Namespace && Key == Other.Key;
	}
};

uint32 GetTypeHash(const FTextIdentity& Identity)
{
	return HashCombine(GetTypeHash(Identity.Namespace), GetTypeHash(Identity.Key));
}

FString DescribeIdentity(const FTextIdentity& Identity)
{
	return FString::Printf(TEXT("Namespace='%s', Key='%s'"), *Identity.Namespace, *Identity.Key);
}

struct FLocalizationTargetConfig
{
	FString TargetName;
	FString SourcePath;
	FString NativeCulture;
	FString ManifestName;
	FString ArchiveName;
	/** Cultures declared by the UE export config. Their archives are required. */
	TArray<FString> ExportCultures;
	/** Export cultures plus cultures maintained manually in languages.json. */
	TArray<FString> Cultures;
};

struct FGeneratedOutput
{
	FString Name;
	FString FinalFilename;
	TSharedPtr<FJsonObject> JsonObject;
	int32 EntryCount = 0;
};

struct FTransactionFile
{
	FString Name;
	FString FinalFilename;
	FString TemporaryFilename;
	FString BackupFilename;
	TSharedPtr<FJsonObject> JsonObject;
	bool bHadOriginal = false;
	bool bWasCommitted = false;
};

bool ContainsCulture(const TArray<FString>& Cultures, const FString& Culture)
{
	return Cultures.ContainsByPredicate([&Culture](const FString& ExistingCulture)
	{
		return ExistingCulture.Equals(Culture, ESearchCase::IgnoreCase);
	});
}

void AddUniqueCulture(TArray<FString>& Cultures, const FString& Culture)
{
	if (!Culture.IsEmpty() && !ContainsCulture(Cultures, Culture))
	{
		Cultures.Add(Culture);
	}
}

FString StripInlineComment(const FString& Line)
{
	int32 CommentIndex = INDEX_NONE;
	if (Line.FindChar(TEXT(';'), CommentIndex))
	{
		return Line.Left(CommentIndex).TrimStartAndEnd();
	}
	return Line.TrimStartAndEnd();
}

bool ParseLocalizationExportConfig(const FString& Filename, FLocalizationTargetConfig& OutConfig)
{
	FString Text;
	if (!FFileHelper::LoadFileToString(Text, *Filename))
	{
		return false;
	}

	OutConfig.TargetName = FPaths::GetBaseFilename(Filename);
	OutConfig.TargetName.RemoveFromEnd(TEXT("_Export"));
	OutConfig.ManifestName = OutConfig.TargetName + TEXT(".manifest");
	OutConfig.ArchiveName = OutConfig.TargetName + TEXT(".archive");

	bool bInCommonSettings = false;
	TArray<FString> Lines;
	Text.ParseIntoArrayLines(Lines);
	for (const FString& RawLine : Lines)
	{
		const FString Line = StripInlineComment(RawLine);
		if (Line.IsEmpty())
		{
			continue;
		}

		if (Line.StartsWith(TEXT("[")) && Line.EndsWith(TEXT("]")))
		{
			bInCommonSettings = Line.Equals(TEXT("[CommonSettings]"), ESearchCase::IgnoreCase);
			continue;
		}

		if (!bInCommonSettings)
		{
			continue;
		}

		FString Key;
		FString Value;
		if (!Line.Split(TEXT("="), &Key, &Value))
		{
			continue;
		}

		Key = Key.TrimStartAndEnd();
		Key.RemoveFromStart(TEXT("+"));
		Value = Value.TrimStartAndEnd().TrimQuotes();

		if (Key.Equals(TEXT("SourcePath"), ESearchCase::IgnoreCase))
		{
			OutConfig.SourcePath = Value;
		}
		else if (Key.Equals(TEXT("NativeCulture"), ESearchCase::IgnoreCase))
		{
			OutConfig.NativeCulture = Value;
			AddUniqueCulture(OutConfig.ExportCultures, Value);
		}
		else if (Key.Equals(TEXT("CulturesToGenerate"), ESearchCase::IgnoreCase))
		{
			AddUniqueCulture(OutConfig.ExportCultures, Value);
		}
		else if (Key.Equals(TEXT("ManifestName"), ESearchCase::IgnoreCase))
		{
			OutConfig.ManifestName = Value;
		}
		else if (Key.Equals(TEXT("ArchiveName"), ESearchCase::IgnoreCase))
		{
			OutConfig.ArchiveName = Value;
		}
	}

	OutConfig.ExportCultures.RemoveAll([](const FString& Culture)
	{
		return Culture.IsEmpty();
	});
	OutConfig.Cultures = OutConfig.ExportCultures;

	return !OutConfig.TargetName.IsEmpty()
		&& !OutConfig.SourcePath.IsEmpty()
		&& !OutConfig.NativeCulture.IsEmpty()
		&& !OutConfig.ExportCultures.IsEmpty();
}

bool LoadGeneratorJsonObject(const FString& Filename, TSharedPtr<FJsonObject>& OutObject)
{
	FString JsonText;
	if (!FFileHelper::LoadFileToString(JsonText, *Filename))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Warning, TEXT("Could not read '%s'."), *Filename);
		return false;
	}

	const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonText);
	if (!FJsonSerializer::Deserialize(Reader, OutObject) || !OutObject.IsValid())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Warning, TEXT("Could not parse '%s'."), *Filename);
		return false;
	}

	return true;
}

FString GetObjectStringField(const TSharedPtr<FJsonObject>& Object, const TCHAR* FieldName)
{
	FString Value;
	if (Object.IsValid())
	{
		Object->TryGetStringField(FieldName, Value);
	}
	return Value;
}

FString GetNestedTextField(const TSharedPtr<FJsonObject>& Object, const TCHAR* ObjectFieldName)
{
	const TSharedPtr<FJsonObject>* NestedObject = nullptr;
	if (Object.IsValid() && Object->TryGetObjectField(ObjectFieldName, NestedObject) && NestedObject && NestedObject->IsValid())
	{
		return GetObjectStringField(*NestedObject, TEXT("Text"));
	}
	return FString();
}

bool CollectManifestEntries(
	const TSharedPtr<FJsonObject>& Object,
	const FString& InheritedNamespace,
	const FString& ManifestFilename,
	TMap<FTextIdentity, FString>& OutSources,
	TArray<FTextIdentity>& OutOrder)
{
	if (!Object.IsValid())
	{
		return true;
	}

	FString Namespace = InheritedNamespace;
	Object->TryGetStringField(TEXT("Namespace"), Namespace);
	bool bSuccess = true;

	const FString Source = GetNestedTextField(Object, TEXT("Source"));
	const TArray<TSharedPtr<FJsonValue>>* Keys = nullptr;
	if (!Source.IsEmpty() && Object->TryGetArrayField(TEXT("Keys"), Keys))
	{
		for (const TSharedPtr<FJsonValue>& KeyValue : *Keys)
		{
			const TSharedPtr<FJsonObject>* KeyObject = nullptr;
			if (!KeyValue.IsValid() || !KeyValue->TryGetObject(KeyObject) || !KeyObject || !KeyObject->IsValid())
			{
				continue;
			}

			const FString Key = GetObjectStringField(*KeyObject, TEXT("Key"));
			if (Key.IsEmpty())
			{
				continue;
			}
			if (Source.IsEmpty())
			{
				UE_LOG(
					LogLocalizationOverridesGenerator,
					Error,
					TEXT("Manifest entry Namespace='%s', Key='%s' has an empty source in '%s'."),
					*Namespace,
					*Key,
					*ManifestFilename);
				bSuccess = false;
				continue;
			}

			const FTextIdentity Identity{ Namespace, Key };
			if (const FString* ExistingSource = OutSources.Find(Identity))
			{
				if (*ExistingSource != Source)
				{
					UE_LOG(
						LogLocalizationOverridesGenerator,
						Error,
						TEXT("Conflicting manifest source for %s in '%s': '%s' versus '%s'."),
						*DescribeIdentity(Identity),
						*ManifestFilename,
						**ExistingSource,
						*Source);
					bSuccess = false;
				}
				continue;
			}

			OutSources.Add(Identity, Source);
			OutOrder.Add(Identity);
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* Children = nullptr;
	if (Object->TryGetArrayField(TEXT("Children"), Children))
	{
		for (const TSharedPtr<FJsonValue>& ChildValue : *Children)
		{
			const TSharedPtr<FJsonObject>* ChildObject = nullptr;
			if (ChildValue.IsValid() && ChildValue->TryGetObject(ChildObject) && ChildObject && ChildObject->IsValid())
			{
				bSuccess &= CollectManifestEntries(*ChildObject, Namespace, ManifestFilename, OutSources, OutOrder);
			}
		}
	}

	return bSuccess;
}

bool CollectArchiveTranslations(
	const TSharedPtr<FJsonObject>& Object,
	const FString& InheritedNamespace,
	const FString& Culture,
	const FString& ArchiveFilename,
	TMap<FTextIdentity, FString>& OutTranslations)
{
	if (!Object.IsValid())
	{
		return true;
	}

	FString Namespace = InheritedNamespace;
	Object->TryGetStringField(TEXT("Namespace"), Namespace);
	bool bSuccess = true;

	const FString Key = GetObjectStringField(Object, TEXT("Key"));
	const FString Translation = GetNestedTextField(Object, TEXT("Translation"));
	if (!Key.IsEmpty() && !Translation.IsEmpty())
	{
		const FTextIdentity Identity{ Namespace, Key };
		if (const FString* ExistingTranslation = OutTranslations.Find(Identity))
		{
			if (*ExistingTranslation != Translation)
			{
				UE_LOG(
					LogLocalizationOverridesGenerator,
					Error,
					TEXT("Conflicting '%s' archive translation for %s in '%s': '%s' versus '%s'."),
					*Culture,
					*DescribeIdentity(Identity),
					*ArchiveFilename,
					**ExistingTranslation,
					*Translation);
				bSuccess = false;
			}
		}
		else
		{
			OutTranslations.Add(Identity, Translation);
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* Children = nullptr;
	if (Object->TryGetArrayField(TEXT("Children"), Children))
	{
		for (const TSharedPtr<FJsonValue>& ChildValue : *Children)
		{
			const TSharedPtr<FJsonObject>* ChildObject = nullptr;
			if (ChildValue.IsValid() && ChildValue->TryGetObject(ChildObject) && ChildObject && ChildObject->IsValid())
			{
				bSuccess &= CollectArchiveTranslations(*ChildObject, Namespace, Culture, ArchiveFilename, OutTranslations);
			}
		}
	}

	return bSuccess;
}

bool CollectExistingTranslations(
	const FString& ExistingOutputFilename,
	const TArray<FString>& Cultures,
	TMap<FString, TMap<FTextIdentity, FString>>& OutTranslationsByCulture)
{
	if (!FPaths::FileExists(ExistingOutputFilename))
	{
		return true;
	}

	TSharedPtr<FJsonObject> ExistingOutputObject;
	if (!LoadGeneratorJsonObject(ExistingOutputFilename, ExistingOutputObject))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing target JSON is invalid: '%s'."), *ExistingOutputFilename);
		return false;
	}

	const TArray<TSharedPtr<FJsonValue>>* ExistingEntries = nullptr;
	if (!ExistingOutputObject->TryGetArrayField(TEXT("entries"), ExistingEntries))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing target JSON has no entries array: '%s'."), *ExistingOutputFilename);
		return false;
	}

	TMap<FTextIdentity, FString> ExistingSources;
	TMap<FString, TMap<FTextIdentity, FString>> ExistingTranslationsByCulture;
	for (const TSharedPtr<FJsonValue>& ExistingEntryValue : *ExistingEntries)
	{
		const TSharedPtr<FJsonObject>* ExistingEntryObjectPtr = nullptr;
		if (!ExistingEntryValue.IsValid() || !ExistingEntryValue->TryGetObject(ExistingEntryObjectPtr) || !ExistingEntryObjectPtr || !ExistingEntryObjectPtr->IsValid())
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing target JSON contains a non-object entry: '%s'."), *ExistingOutputFilename);
			return false;
		}

		const TSharedPtr<FJsonObject>& ExistingEntryObject = *ExistingEntryObjectPtr;
		const FTextIdentity Identity{
			GetObjectStringField(ExistingEntryObject, TEXT("namespace")),
			GetObjectStringField(ExistingEntryObject, TEXT("key"))
		};
		const FString Source = GetObjectStringField(ExistingEntryObject, TEXT("source"));
		if (Identity.Key.IsEmpty() || Source.IsEmpty())
		{
			UE_LOG(
				LogLocalizationOverridesGenerator,
				Error,
				TEXT("Existing target JSON contains an entry without a key or source in '%s'."),
				*ExistingOutputFilename);
			return false;
		}

		if (const FString* ExistingSource = ExistingSources.Find(Identity))
		{
			if (*ExistingSource != Source)
			{
				UE_LOG(
					LogLocalizationOverridesGenerator,
					Error,
					TEXT("Existing target JSON contains conflicting sources for %s in '%s'."),
					*DescribeIdentity(Identity),
					*ExistingOutputFilename);
				return false;
			}
		}
		else
		{
			ExistingSources.Add(Identity, Source);
		}

		const TSharedPtr<FJsonObject>* ExistingTranslationsPtr = nullptr;
		if (!ExistingEntryObject->TryGetObjectField(TEXT("translations"), ExistingTranslationsPtr)
			|| !ExistingTranslationsPtr
			|| !ExistingTranslationsPtr->IsValid())
		{
			UE_LOG(
				LogLocalizationOverridesGenerator,
				Error,
				TEXT("Existing target JSON entry %s has no translations object in '%s'."),
				*DescribeIdentity(Identity),
				*ExistingOutputFilename);
			return false;
		}

		for (const FString& Culture : Cultures)
		{
			FString ExistingTranslation;
			if (!(*ExistingTranslationsPtr)->TryGetStringField(Culture, ExistingTranslation) || ExistingTranslation.IsEmpty())
			{
				continue;
			}

			TMap<FTextIdentity, FString>& CultureTranslations = ExistingTranslationsByCulture.FindOrAdd(Culture);
			if (const FString* DuplicateTranslation = CultureTranslations.Find(Identity))
			{
				if (*DuplicateTranslation != ExistingTranslation)
				{
					UE_LOG(
						LogLocalizationOverridesGenerator,
						Error,
						TEXT("Existing target JSON contains conflicting '%s' translations for %s in '%s'."),
						*Culture,
						*DescribeIdentity(Identity),
						*ExistingOutputFilename);
					return false;
				}
			}
			else
			{
				CultureTranslations.Add(Identity, ExistingTranslation);
			}
		}
	}

	// Archive data remains authoritative. Existing values only fill identities
	// that have no non-empty archive translation, which also preserves cultures
	// maintained exclusively by the external editor.
	for (const TPair<FString, TMap<FTextIdentity, FString>>& CulturePair : ExistingTranslationsByCulture)
	{
		TMap<FTextIdentity, FString>& Destination = OutTranslationsByCulture.FindOrAdd(CulturePair.Key);
		for (const TPair<FTextIdentity, FString>& TranslationPair : CulturePair.Value)
		{
			Destination.FindOrAdd(TranslationPair.Key, TranslationPair.Value);
		}
	}

	return true;
}

bool BuildTargetOutput(
	const FLocalizationTargetConfig& Config,
	const FString& OutputDirectory,
	FGeneratedOutput& OutOutput)
{
	const FString TargetLocalizationDirectory = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / Config.SourcePath);
	const FString ManifestFilename = TargetLocalizationDirectory / Config.ManifestName;

	TSharedPtr<FJsonObject> ManifestObject;
	if (!LoadGeneratorJsonObject(ManifestFilename, ManifestObject))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Target '%s' cannot read its manifest '%s'."), *Config.TargetName, *ManifestFilename);
		return false;
	}

	TMap<FTextIdentity, FString> ManifestSources;
	TArray<FTextIdentity> ManifestOrder;
	if (!CollectManifestEntries(ManifestObject, FString(), ManifestFilename, ManifestSources, ManifestOrder))
	{
		return false;
	}
	if (ManifestOrder.IsEmpty())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("No manifest entries found in '%s'."), *ManifestFilename);
		return false;
	}

	TMap<FString, TMap<FTextIdentity, FString>> TranslationsByCulture;
	for (const FString& Culture : Config.Cultures)
	{
		const FString ArchiveFilename = TargetLocalizationDirectory / Culture / Config.ArchiveName;
		const bool bArchiveIsRequired = ContainsCulture(Config.ExportCultures, Culture);
		if (!FPaths::FileExists(ArchiveFilename))
		{
			if (bArchiveIsRequired)
			{
				UE_LOG(
					LogLocalizationOverridesGenerator,
					Error,
					TEXT("Target '%s' requires culture '%s' archive '%s', but the file does not exist."),
					*Config.TargetName,
					*Culture,
					*ArchiveFilename);
				return false;
			}

			UE_LOG(
				LogLocalizationOverridesGenerator,
				Verbose,
				TEXT("Manual culture '%s' has no archive for target '%s'; preserving existing JSON translations."),
				*Culture,
				*Config.TargetName);
			continue;
		}

		TSharedPtr<FJsonObject> ArchiveObject;
		if (!LoadGeneratorJsonObject(ArchiveFilename, ArchiveObject))
		{
			UE_LOG(
				LogLocalizationOverridesGenerator,
				Error,
				TEXT("Target '%s' has an invalid culture '%s' archive '%s'."),
				*Config.TargetName,
				*Culture,
				*ArchiveFilename);
			return false;
		}

		TMap<FTextIdentity, FString>& CultureTranslations = TranslationsByCulture.FindOrAdd(Culture);
		if (!CollectArchiveTranslations(ArchiveObject, FString(), Culture, ArchiveFilename, CultureTranslations))
		{
			return false;
		}
	}

	const FString ExistingOutputFilename = OutputDirectory / (Config.TargetName + TEXT(".json"));
	if (!CollectExistingTranslations(ExistingOutputFilename, Config.Cultures, TranslationsByCulture))
	{
		return false;
	}

	TArray<TSharedPtr<FJsonValue>> EntryValues;
	EntryValues.Reserve(ManifestOrder.Num());
	for (const FTextIdentity& Identity : ManifestOrder)
	{
		const FString* Source = ManifestSources.Find(Identity);
		if (!Source)
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Internal manifest identity lookup failed for %s."), *DescribeIdentity(Identity));
			return false;
		}

		TSharedPtr<FJsonObject> EntryObject = MakeShared<FJsonObject>();
		EntryObject->SetStringField(TEXT("namespace"), Identity.Namespace);
		EntryObject->SetStringField(TEXT("key"), Identity.Key);
		EntryObject->SetStringField(TEXT("source"), *Source);

		TSharedPtr<FJsonObject> TranslationsObject = MakeShared<FJsonObject>();
		for (const FString& Culture : Config.Cultures)
		{
			const TMap<FTextIdentity, FString>* CultureTranslations = TranslationsByCulture.Find(Culture);
			const FString* Translation = CultureTranslations ? CultureTranslations->Find(Identity) : nullptr;
			if (Translation && !Translation->IsEmpty())
			{
				TranslationsObject->SetStringField(Culture, *Translation);
			}
		}

		EntryObject->SetObjectField(TEXT("translations"), TranslationsObject);
		EntryValues.Add(MakeShared<FJsonValueObject>(EntryObject));
	}

	TSharedPtr<FJsonObject> RootObject = MakeShared<FJsonObject>();
	RootObject->SetNumberField(TEXT("version"), 1);
	RootObject->SetStringField(TEXT("target"), Config.TargetName);
	RootObject->SetStringField(TEXT("nativeCulture"), Config.NativeCulture);
	RootObject->SetArrayField(TEXT("entries"), EntryValues);

	OutOutput.Name = Config.TargetName;
	OutOutput.FinalFilename = ExistingOutputFilename;
	OutOutput.JsonObject = MoveTemp(RootObject);
	OutOutput.EntryCount = EntryValues.Num();
	return true;
}

bool SerializeJsonObject(const TSharedPtr<FJsonObject>& Object, FString& OutJsonText)
{
	if (!Object.IsValid())
	{
		return false;
	}

	const TSharedRef<TJsonWriter<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>> Writer =
		TJsonWriterFactory<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>::Create(&OutJsonText);
	return FJsonSerializer::Serialize(Object.ToSharedRef(), Writer);
}

bool ValidateTemporaryJson(const FString& Filename)
{
	TArray<uint8> Bytes;
	if (!FFileHelper::LoadFileToArray(Bytes, *Filename)
		|| Bytes.Num() < 2
		|| Bytes[0] != 0xFF
		|| Bytes[1] != 0xFE)
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Generated temporary JSON is not UTF-16 LE with BOM: '%s'."), *Filename);
		return false;
	}

	TSharedPtr<FJsonObject> ParsedObject;
	if (!LoadGeneratorJsonObject(Filename, ParsedObject))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Generated temporary JSON failed validation: '%s'."), *Filename);
		return false;
	}
	return true;
}

void DeleteTransactionFileIfPresent(const FString& Filename)
{
	if (!Filename.IsEmpty() && FPaths::FileExists(Filename))
	{
		IFileManager::Get().Delete(*Filename, false, true, true);
	}
}

bool CommitGeneratedOutputs(const TArray<FGeneratedOutput>& Outputs)
{
	if (Outputs.IsEmpty())
	{
		return false;
	}

	const FString TransactionId = FGuid::NewGuid().ToString(EGuidFormats::Digits);
	TArray<FTransactionFile> Files;
	Files.Reserve(Outputs.Num());

	for (const FGeneratedOutput& Output : Outputs)
	{
		FTransactionFile& File = Files.AddDefaulted_GetRef();
		File.Name = Output.Name;
		File.FinalFilename = Output.FinalFilename;
		File.TemporaryFilename = Output.FinalFilename + TEXT(".tmp.") + TransactionId;
		File.BackupFilename = Output.FinalFilename + TEXT(".rollback.") + TransactionId;
		File.JsonObject = Output.JsonObject;

		FString JsonText;
		if (!SerializeJsonObject(File.JsonObject, JsonText)
			|| !FFileHelper::SaveStringToFile(JsonText, *File.TemporaryFilename, FFileHelper::EEncodingOptions::ForceUnicode)
			|| !ValidateTemporaryJson(File.TemporaryFilename))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Could not prepare generated output '%s'."), *File.FinalFilename);
			for (const FTransactionFile& CleanupFile : Files)
			{
				DeleteTransactionFileIfPresent(CleanupFile.TemporaryFilename);
				DeleteTransactionFileIfPresent(CleanupFile.BackupFilename);
			}
			return false;
		}
	}

	IFileManager& FileManager = IFileManager::Get();
	for (FTransactionFile& File : Files)
	{
		File.bHadOriginal = FPaths::FileExists(File.FinalFilename);
		if (File.bHadOriginal && FileManager.Copy(*File.BackupFilename, *File.FinalFilename, true, true) != COPY_OK)
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Could not back up '%s' before generation."), *File.FinalFilename);
			for (const FTransactionFile& CleanupFile : Files)
			{
				DeleteTransactionFileIfPresent(CleanupFile.TemporaryFilename);
				DeleteTransactionFileIfPresent(CleanupFile.BackupFilename);
			}
			return false;
		}
	}

	bool bCommitSucceeded = true;
	for (FTransactionFile& File : Files)
	{
		if (!FileManager.Move(*File.FinalFilename, *File.TemporaryFilename, true, true, false, true))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Could not commit generated output '%s'."), *File.FinalFilename);
			bCommitSucceeded = false;
			break;
		}
		File.bWasCommitted = true;
	}

	if (bCommitSucceeded)
	{
		for (const FTransactionFile& File : Files)
		{
			if (!ValidateTemporaryJson(File.FinalFilename))
			{
				UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Committed output failed validation: '%s'."), *File.FinalFilename);
				bCommitSucceeded = false;
				break;
			}
		}
	}

	if (!bCommitSucceeded)
	{
		bool bRollbackSucceeded = true;
		for (const FTransactionFile& File : Files)
		{
			// Files whose Move failed or was never attempted still contain their
			// original bytes. Restoring them is both unnecessary and may fail for
			// the same lock/permission condition that rejected the commit.
			if (!File.bWasCommitted)
			{
				continue;
			}

			if (File.bHadOriginal)
			{
				if (!FPaths::FileExists(File.BackupFilename)
					|| FileManager.Copy(*File.FinalFilename, *File.BackupFilename, true, true) != COPY_OK)
				{
					UE_LOG(
						LogLocalizationOverridesGenerator,
						Error,
						TEXT("Rollback failed for '%s'. Recovery copy is '%s'."),
						*File.FinalFilename,
						*File.BackupFilename);
					bRollbackSucceeded = false;
				}
			}
			else if (FPaths::FileExists(File.FinalFilename) && !FileManager.Delete(*File.FinalFilename, false, true, true))
			{
				UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Rollback could not remove newly created '%s'."), *File.FinalFilename);
				bRollbackSucceeded = false;
			}
		}

		for (const FTransactionFile& File : Files)
		{
			DeleteTransactionFileIfPresent(File.TemporaryFilename);
			if (bRollbackSucceeded)
			{
				DeleteTransactionFileIfPresent(File.BackupFilename);
			}
		}

		if (!bRollbackSucceeded)
		{
			UE_LOG(
				LogLocalizationOverridesGenerator,
				Error,
				TEXT("Localization override transaction '%s' could not be fully rolled back. Recovery files were preserved."),
				*TransactionId);
		}
		return false;
	}

	for (const FTransactionFile& File : Files)
	{
		DeleteTransactionFileIfPresent(File.TemporaryFilename);
		if (!File.BackupFilename.IsEmpty() && FPaths::FileExists(File.BackupFilename)
			&& !FileManager.Delete(*File.BackupFilename, false, true, true))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Warning, TEXT("Generated output is valid, but rollback file could not be removed: '%s'."), *File.BackupFilename);
		}
	}
	return true;
}

bool LoadExistingLanguageSettings(
	const FString& LanguagesFilename,
	FString& OutDefaultCulture,
	TArray<FString>& OutCultures)
{
	if (!FPaths::FileExists(LanguagesFilename))
	{
		return true;
	}

	TSharedPtr<FJsonObject> ExistingLanguagesObject;
	if (!LoadGeneratorJsonObject(LanguagesFilename, ExistingLanguagesObject))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON is invalid: '%s'."), *LanguagesFilename);
		return false;
	}

	double Version = 0.0;
	if (!ExistingLanguagesObject->TryGetNumberField(TEXT("version"), Version) || Version != 1.0)
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON must have version 1: '%s'."), *LanguagesFilename);
		return false;
	}
	if (!ExistingLanguagesObject->TryGetStringField(TEXT("defaultCulture"), OutDefaultCulture) || OutDefaultCulture.IsEmpty())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON has no valid defaultCulture: '%s'."), *LanguagesFilename);
		return false;
	}

	const TArray<TSharedPtr<FJsonValue>>* ExistingCultureValues = nullptr;
	if (!ExistingLanguagesObject->TryGetArrayField(TEXT("cultures"), ExistingCultureValues) || ExistingCultureValues->IsEmpty())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON has no cultures: '%s'."), *LanguagesFilename);
		return false;
	}

	for (const TSharedPtr<FJsonValue>& CultureValue : *ExistingCultureValues)
	{
		FString Culture;
		if (!CultureValue.IsValid() || !CultureValue->TryGetString(Culture) || Culture.IsEmpty())
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON contains an invalid culture: '%s'."), *LanguagesFilename);
			return false;
		}
		if (ContainsCulture(OutCultures, Culture))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Existing languages JSON contains duplicate culture '%s': '%s'."), *Culture, *LanguagesFilename);
			return false;
		}
		OutCultures.Add(Culture);
	}

	if (!ContainsCulture(OutCultures, OutDefaultCulture))
	{
		UE_LOG(
			LogLocalizationOverridesGenerator,
			Error,
			TEXT("Existing languages JSON defaultCulture '%s' is not declared in cultures: '%s'."),
			*OutDefaultCulture,
			*LanguagesFilename);
		return false;
	}
	return true;
}
}

bool FLocalizationOverridesGenerator::GenerateFromProjectLocalization()
{
	const FString LocalizationConfigDirectory = FPaths::ProjectConfigDir() / TEXT("Localization");
	TArray<FString> ExportConfigFiles;
	IFileManager::Get().FindFiles(ExportConfigFiles, *(LocalizationConfigDirectory / TEXT("*_Export.ini")), true, false);
	ExportConfigFiles.Sort();
	if (ExportConfigFiles.IsEmpty())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("No localization export configs found in '%s'."), *LocalizationConfigDirectory);
		return false;
	}

	const FString OutputDirectory = FPaths::ProjectDir() / GeneratedOverridesFolderName;
	if (!IFileManager::Get().MakeDirectory(*OutputDirectory, true))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Could not create '%s'."), *OutputDirectory);
		return false;
	}

	const FString LanguagesFilename = OutputDirectory / GeneratedLanguagesFileName;
	FString ExistingDefaultCulture;
	TArray<FString> ExistingCultures;
	if (!LoadExistingLanguageSettings(LanguagesFilename, ExistingDefaultCulture, ExistingCultures))
	{
		return false;
	}

	TArray<FLocalizationTargetConfig> TargetConfigs;
	TArray<FString> AllCultures;
	FString DefaultCulture;
	TSet<FString> CanonicalTargetNames;
	TArray<FString> FailedTargets;
	for (const FString& ExportConfigFile : ExportConfigFiles)
	{
		const FString ExportConfigFilename = LocalizationConfigDirectory / ExportConfigFile;
		FLocalizationTargetConfig TargetConfig;
		if (!ParseLocalizationExportConfig(ExportConfigFilename, TargetConfig))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Could not parse localization export config '%s'."), *ExportConfigFilename);
			FailedTargets.Add(FPaths::GetBaseFilename(ExportConfigFile));
			continue;
		}

		const FString CanonicalTargetName = TargetConfig.TargetName.ToLower();
		if (CanonicalTargetName == TEXT("languages"))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Localization target name '%s' is reserved by languages.json."), *TargetConfig.TargetName);
			FailedTargets.Add(TargetConfig.TargetName);
			continue;
		}
		if (CanonicalTargetNames.Contains(CanonicalTargetName))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Duplicate localization target name '%s'."), *TargetConfig.TargetName);
			FailedTargets.Add(TargetConfig.TargetName);
			continue;
		}
		CanonicalTargetNames.Add(CanonicalTargetName);

		if (DefaultCulture.IsEmpty())
		{
			DefaultCulture = TargetConfig.NativeCulture;
		}
		for (const FString& Culture : TargetConfig.ExportCultures)
		{
			AddUniqueCulture(AllCultures, Culture);
		}
		TargetConfigs.Add(MoveTemp(TargetConfig));
	}

	for (const FString& Culture : ExistingCultures)
	{
		AddUniqueCulture(AllCultures, Culture);
	}

	TArray<FGeneratedOutput> Outputs;
	int32 TotalEntryCount = 0;
	for (FLocalizationTargetConfig& TargetConfig : TargetConfigs)
	{
		for (const FString& Culture : ExistingCultures)
		{
			AddUniqueCulture(TargetConfig.Cultures, Culture);
		}

		FGeneratedOutput TargetOutput;
		if (!BuildTargetOutput(TargetConfig, OutputDirectory, TargetOutput))
		{
			UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Generation failed for localization target '%s'."), *TargetConfig.TargetName);
			FailedTargets.Add(TargetConfig.TargetName);
			continue;
		}
		TotalEntryCount += TargetOutput.EntryCount;
		Outputs.Add(MoveTemp(TargetOutput));
	}

	if (!FailedTargets.IsEmpty())
	{
		UE_LOG(
			LogLocalizationOverridesGenerator,
			Error,
			TEXT("Localization override generation failed: %d target(s) built, %d target(s) failed [%s], %d entries prepared. No output files were changed."),
			Outputs.Num(),
			FailedTargets.Num(),
			*FString::Join(FailedTargets, TEXT(", ")),
			TotalEntryCount);
		return false;
	}

	TArray<TSharedPtr<FJsonValue>> CultureValues;
	for (const FString& Culture : AllCultures)
	{
		CultureValues.Add(MakeShared<FJsonValueString>(Culture));
	}

	const FString* MatchingDefaultCulture = AllCultures.FindByPredicate([&ExistingDefaultCulture](const FString& Culture)
	{
		return Culture.Equals(ExistingDefaultCulture, ESearchCase::IgnoreCase);
	});
	const FString CultureToWrite = MatchingDefaultCulture ? *MatchingDefaultCulture : DefaultCulture;
	if (CultureToWrite.IsEmpty() || AllCultures.IsEmpty())
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("No valid cultures were collected from localization export configs."));
		return false;
	}

	TSharedPtr<FJsonObject> LanguagesObject = MakeShared<FJsonObject>();
	LanguagesObject->SetNumberField(TEXT("version"), 1);
	LanguagesObject->SetStringField(TEXT("defaultCulture"), CultureToWrite);
	LanguagesObject->SetArrayField(TEXT("cultures"), CultureValues);
	FGeneratedOutput LanguagesOutput;
	LanguagesOutput.Name = GeneratedLanguagesFileName;
	LanguagesOutput.FinalFilename = LanguagesFilename;
	LanguagesOutput.JsonObject = MoveTemp(LanguagesObject);
	Outputs.Add(MoveTemp(LanguagesOutput));

	if (!CommitGeneratedOutputs(Outputs))
	{
		UE_LOG(LogLocalizationOverridesGenerator, Error, TEXT("Localization override transaction failed. No partial generation was accepted."));
		return false;
	}

	for (const FGeneratedOutput& Output : Outputs)
	{
		if (Output.EntryCount > 0)
		{
			UE_LOG(
				LogLocalizationOverridesGenerator,
				Log,
				TEXT("Generated %d localization override entries in '%s'."),
				Output.EntryCount,
				*Output.FinalFilename);
		}
	}
	UE_LOG(
		LogLocalizationOverridesGenerator,
		Log,
		TEXT("Localization override generation completed: %d target(s), %d total entries, %d culture(s)."),
		TargetConfigs.Num(),
		TotalEntryCount,
		AllCultures.Num());
	return true;
}

#else

bool FLocalizationOverridesGenerator::GenerateFromProjectLocalization()
{
	return false;
}

#endif
