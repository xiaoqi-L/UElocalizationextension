// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "LocalizationOverridesBlueprintLibrary.h"

#ifdef LOCALIZATIONOVERRIDES_LocalizationOverridesBlueprintLibrary_generated_h
#error "LocalizationOverridesBlueprintLibrary.generated.h already included, missing '#pragma once' in LocalizationOverridesBlueprintLibrary.h"
#endif
#define LOCALIZATIONOVERRIDES_LocalizationOverridesBlueprintLibrary_generated_h

#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS

// ********** Begin Class ULocalizationOverridesBlueprintLibrary ***********************************
#define FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execGenerateLocalizationOverrideFiles); \
	DECLARE_FUNCTION(execGetCurrentGameCulture); \
	DECLARE_FUNCTION(execGetLocalizationOverridesDirectory); \
	DECLARE_FUNCTION(execGetAvailableCultures); \
	DECLARE_FUNCTION(execSetGameCulture); \
	DECLARE_FUNCTION(execReloadLocalizationOverrides);


struct Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics;
LOCALIZATIONOVERRIDES_API UClass* Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_NoRegister();

#define FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesULocalizationOverridesBlueprintLibrary(); \
	friend struct ::Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics; \
	static UClass* GetPrivateStaticClass(); \
	friend LOCALIZATIONOVERRIDES_API UClass* ::Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_NoRegister(); \
public: \
	DECLARE_CLASS2(ULocalizationOverridesBlueprintLibrary, UBlueprintFunctionLibrary, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/LocalizationOverrides"), Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_NoRegister) \
	DECLARE_SERIALIZER(ULocalizationOverridesBlueprintLibrary)


#define FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API ULocalizationOverridesBlueprintLibrary(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
	/** Deleted move- and copy-constructors, should never be used */ \
	ULocalizationOverridesBlueprintLibrary(ULocalizationOverridesBlueprintLibrary&&) = delete; \
	ULocalizationOverridesBlueprintLibrary(const ULocalizationOverridesBlueprintLibrary&) = delete; \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, ULocalizationOverridesBlueprintLibrary); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(ULocalizationOverridesBlueprintLibrary); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(ULocalizationOverridesBlueprintLibrary) \
	NO_API virtual ~ULocalizationOverridesBlueprintLibrary();


#define FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_6_PROLOG
#define FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_INCLASS_NO_PURE_DECLS \
	FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h_9_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


class ULocalizationOverridesBlueprintLibrary;

// ********** End Class ULocalizationOverridesBlueprintLibrary *************************************

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h

PRAGMA_ENABLE_DEPRECATION_WARNINGS
