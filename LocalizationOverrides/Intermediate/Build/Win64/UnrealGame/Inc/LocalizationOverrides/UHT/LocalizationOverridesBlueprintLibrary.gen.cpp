// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "LocalizationOverridesBlueprintLibrary.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
static_assert(!UE_WITH_CONSTINIT_UOBJECT, "This generated code can only be compiled with !UE_WITH_CONSTINIT_OBJECT");
void EmptyLinkFunctionForGeneratedCodeLocalizationOverridesBlueprintLibrary() {}

// ********** Begin Cross Module References ********************************************************
ENGINE_API UClass* Z_Construct_UClass_UBlueprintFunctionLibrary();
LOCALIZATIONOVERRIDES_API UClass* Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary();
LOCALIZATIONOVERRIDES_API UClass* Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_NoRegister();
UPackage* Z_Construct_UPackage__Script_LocalizationOverrides();
// ********** End Cross Module References **********************************************************

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function GenerateLocalizationOverrideFiles 
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventGenerateLocalizationOverrideFiles_Parms
	{
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Function GenerateLocalizationOverrideFiles constinit property declarations *****
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function GenerateLocalizationOverrideFiles constinit property declarations *******
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function GenerateLocalizationOverrideFiles Property Definitions ****************
void Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((LocalizationOverridesBlueprintLibrary_eventGenerateLocalizationOverrideFiles_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(LocalizationOverridesBlueprintLibrary_eventGenerateLocalizationOverrideFiles_Parms), &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::PropPointers) < 2048);
// ********** End Function GenerateLocalizationOverrideFiles Property Definitions ******************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "GenerateLocalizationOverrideFiles", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::LocalizationOverridesBlueprintLibrary_eventGenerateLocalizationOverrideFiles_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::LocalizationOverridesBlueprintLibrary_eventGenerateLocalizationOverrideFiles_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execGenerateLocalizationOverrideFiles)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::GenerateLocalizationOverrideFiles();
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function GenerateLocalizationOverrideFiles 

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function GetAvailableCultures *****
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventGetAvailableCultures_Parms
	{
		TArray<FString> ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Function GetAvailableCultures constinit property declarations ******************
	static const UECodeGen_Private::FStrPropertyParams NewProp_ReturnValue_Inner;
	static const UECodeGen_Private::FArrayPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function GetAvailableCultures constinit property declarations ********************
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function GetAvailableCultures Property Definitions *****************************
const UECodeGen_Private::FStrPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::NewProp_ReturnValue_Inner = { "ReturnValue", nullptr, (EPropertyFlags)0x0000000000000000, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, 0, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FArrayPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Array, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(LocalizationOverridesBlueprintLibrary_eventGetAvailableCultures_Parms, ReturnValue), EArrayPropertyFlags::None, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::NewProp_ReturnValue_Inner,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::PropPointers) < 2048);
// ********** End Function GetAvailableCultures Property Definitions *******************************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "GetAvailableCultures", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::LocalizationOverridesBlueprintLibrary_eventGetAvailableCultures_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x14022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::LocalizationOverridesBlueprintLibrary_eventGetAvailableCultures_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execGetAvailableCultures)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(TArray<FString>*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::GetAvailableCultures();
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function GetAvailableCultures *******

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function GetCurrentGameCulture ****
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventGetCurrentGameCulture_Parms
	{
		FString ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Function GetCurrentGameCulture constinit property declarations *****************
	static const UECodeGen_Private::FStrPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function GetCurrentGameCulture constinit property declarations *******************
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function GetCurrentGameCulture Property Definitions ****************************
const UECodeGen_Private::FStrPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(LocalizationOverridesBlueprintLibrary_eventGetCurrentGameCulture_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::PropPointers) < 2048);
// ********** End Function GetCurrentGameCulture Property Definitions ******************************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "GetCurrentGameCulture", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::LocalizationOverridesBlueprintLibrary_eventGetCurrentGameCulture_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x14022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::LocalizationOverridesBlueprintLibrary_eventGetCurrentGameCulture_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execGetCurrentGameCulture)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(FString*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::GetCurrentGameCulture();
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function GetCurrentGameCulture ******

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function GetLocalizationOverridesDirectory 
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventGetLocalizationOverridesDirectory_Parms
	{
		FString ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Function GetLocalizationOverridesDirectory constinit property declarations *****
	static const UECodeGen_Private::FStrPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function GetLocalizationOverridesDirectory constinit property declarations *******
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function GetLocalizationOverridesDirectory Property Definitions ****************
const UECodeGen_Private::FStrPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(LocalizationOverridesBlueprintLibrary_eventGetLocalizationOverridesDirectory_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::PropPointers) < 2048);
// ********** End Function GetLocalizationOverridesDirectory Property Definitions ******************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "GetLocalizationOverridesDirectory", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::LocalizationOverridesBlueprintLibrary_eventGetLocalizationOverridesDirectory_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x14022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::LocalizationOverridesBlueprintLibrary_eventGetLocalizationOverridesDirectory_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execGetLocalizationOverridesDirectory)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(FString*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::GetLocalizationOverridesDirectory();
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function GetLocalizationOverridesDirectory 

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function ReloadLocalizationOverrides 
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventReloadLocalizationOverrides_Parms
	{
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Function ReloadLocalizationOverrides constinit property declarations ***********
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function ReloadLocalizationOverrides constinit property declarations *************
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function ReloadLocalizationOverrides Property Definitions **********************
void Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((LocalizationOverridesBlueprintLibrary_eventReloadLocalizationOverrides_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(LocalizationOverridesBlueprintLibrary_eventReloadLocalizationOverrides_Parms), &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::PropPointers) < 2048);
// ********** End Function ReloadLocalizationOverrides Property Definitions ************************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "ReloadLocalizationOverrides", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::LocalizationOverridesBlueprintLibrary_eventReloadLocalizationOverrides_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::LocalizationOverridesBlueprintLibrary_eventReloadLocalizationOverrides_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execReloadLocalizationOverrides)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::ReloadLocalizationOverrides();
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function ReloadLocalizationOverrides 

// ********** Begin Class ULocalizationOverridesBlueprintLibrary Function SetGameCulture ***********
struct Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics
{
	struct LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms
	{
		FString Culture;
		bool bSaveAsDefaultCulture;
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "Localization Overrides" },
		{ "CPP_Default_bSaveAsDefaultCulture", "false" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_Culture_MetaData[] = {
		{ "NativeConst", "" },
	};
#endif // WITH_METADATA

// ********** Begin Function SetGameCulture constinit property declarations ************************
	static const UECodeGen_Private::FStrPropertyParams NewProp_Culture;
	static void NewProp_bSaveAsDefaultCulture_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_bSaveAsDefaultCulture;
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
// ********** End Function SetGameCulture constinit property declarations **************************
	static const UECodeGen_Private::FFunctionParams FuncParams;
};

// ********** Begin Function SetGameCulture Property Definitions ***********************************
const UECodeGen_Private::FStrPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_Culture = { "Culture", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms, Culture), METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_Culture_MetaData), NewProp_Culture_MetaData) };
void Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_bSaveAsDefaultCulture_SetBit(void* Obj)
{
	((LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms*)Obj)->bSaveAsDefaultCulture = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_bSaveAsDefaultCulture = { "bSaveAsDefaultCulture", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms), &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_bSaveAsDefaultCulture_SetBit, METADATA_PARAMS(0, nullptr) };
void Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms), &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_Culture,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_bSaveAsDefaultCulture,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::PropPointers) < 2048);
// ********** End Function SetGameCulture Property Definitions *************************************
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::FuncParams = { { (UObject*(*)())Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, nullptr, "SetGameCulture", 	Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::PropPointers, 
	UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::PropPointers), 
sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms),
RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::Function_MetaDataParams), Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::Function_MetaDataParams)},  };
static_assert(sizeof(Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::LocalizationOverridesBlueprintLibrary_eventSetGameCulture_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(ULocalizationOverridesBlueprintLibrary::execSetGameCulture)
{
	P_GET_PROPERTY(FStrProperty,Z_Param_Culture);
	P_GET_UBOOL(Z_Param_bSaveAsDefaultCulture);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=ULocalizationOverridesBlueprintLibrary::SetGameCulture(Z_Param_Culture,Z_Param_bSaveAsDefaultCulture);
	P_NATIVE_END;
}
// ********** End Class ULocalizationOverridesBlueprintLibrary Function SetGameCulture *************

// ********** Begin Class ULocalizationOverridesBlueprintLibrary ***********************************
FClassRegistrationInfo Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary;
UClass* ULocalizationOverridesBlueprintLibrary::GetPrivateStaticClass()
{
	using TClass = ULocalizationOverridesBlueprintLibrary;
	if (!Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.InnerSingleton)
	{
		GetPrivateStaticClassBody(
			TClass::StaticPackage(),
			TEXT("LocalizationOverridesBlueprintLibrary"),
			Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.InnerSingleton,
			StaticRegisterNativesULocalizationOverridesBlueprintLibrary,
			sizeof(TClass),
			alignof(TClass),
			TClass::StaticClassFlags,
			TClass::StaticClassCastFlags(),
			TClass::StaticConfigName(),
			(UClass::ClassConstructorType)InternalConstructor<TClass>,
			(UClass::ClassVTableHelperCtorCallerType)InternalVTableHelperCtorCaller<TClass>,
			UOBJECT_CPPCLASS_STATICFUNCTIONS_FORCLASS(TClass),
			&TClass::Super::StaticClass,
			&TClass::WithinClass::StaticClass
		);
	}
	return Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.InnerSingleton;
}
UClass* Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_NoRegister()
{
	return ULocalizationOverridesBlueprintLibrary::GetPrivateStaticClass();
}
struct Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Class_MetaDataParams[] = {
		{ "IncludePath", "LocalizationOverridesBlueprintLibrary.h" },
		{ "ModuleRelativePath", "Public/LocalizationOverridesBlueprintLibrary.h" },
	};
#endif // WITH_METADATA

// ********** Begin Class ULocalizationOverridesBlueprintLibrary constinit property declarations ***
// ********** End Class ULocalizationOverridesBlueprintLibrary constinit property declarations *****
	static constexpr UE::CodeGen::FClassNativeFunction Funcs[] = {
		{ .NameUTF8 = UTF8TEXT("GenerateLocalizationOverrideFiles"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execGenerateLocalizationOverrideFiles },
		{ .NameUTF8 = UTF8TEXT("GetAvailableCultures"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execGetAvailableCultures },
		{ .NameUTF8 = UTF8TEXT("GetCurrentGameCulture"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execGetCurrentGameCulture },
		{ .NameUTF8 = UTF8TEXT("GetLocalizationOverridesDirectory"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execGetLocalizationOverridesDirectory },
		{ .NameUTF8 = UTF8TEXT("ReloadLocalizationOverrides"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execReloadLocalizationOverrides },
		{ .NameUTF8 = UTF8TEXT("SetGameCulture"), .Pointer = &ULocalizationOverridesBlueprintLibrary::execSetGameCulture },
	};
	static UObject* (*const DependentSingletons[])();
	static constexpr FClassFunctionLinkInfo FuncInfo[] = {
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GenerateLocalizationOverrideFiles, "GenerateLocalizationOverrideFiles" }, // 563179256
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetAvailableCultures, "GetAvailableCultures" }, // 3800346834
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetCurrentGameCulture, "GetCurrentGameCulture" }, // 170423950
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_GetLocalizationOverridesDirectory, "GetLocalizationOverridesDirectory" }, // 1006518983
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_ReloadLocalizationOverrides, "ReloadLocalizationOverrides" }, // 1150267539
		{ &Z_Construct_UFunction_ULocalizationOverridesBlueprintLibrary_SetGameCulture, "SetGameCulture" }, // 1460893256
	};
	static_assert(UE_ARRAY_COUNT(FuncInfo) < 2048);
	static constexpr FCppClassTypeInfoStatic StaticCppClassTypeInfo = {
		TCppClassTypeTraits<ULocalizationOverridesBlueprintLibrary>::IsAbstract,
	};
	static const UECodeGen_Private::FClassParams ClassParams;
}; // struct Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics
UObject* (*const Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::DependentSingletons[])() = {
	(UObject* (*)())Z_Construct_UClass_UBlueprintFunctionLibrary,
	(UObject* (*)())Z_Construct_UPackage__Script_LocalizationOverrides,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::DependentSingletons) < 16);
const UECodeGen_Private::FClassParams Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::ClassParams = {
	&ULocalizationOverridesBlueprintLibrary::StaticClass,
	nullptr,
	&StaticCppClassTypeInfo,
	DependentSingletons,
	FuncInfo,
	nullptr,
	nullptr,
	UE_ARRAY_COUNT(DependentSingletons),
	UE_ARRAY_COUNT(FuncInfo),
	0,
	0,
	0x001000A0u,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::Class_MetaDataParams), Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::Class_MetaDataParams)
};
void ULocalizationOverridesBlueprintLibrary::StaticRegisterNativesULocalizationOverridesBlueprintLibrary()
{
	UClass* Class = ULocalizationOverridesBlueprintLibrary::StaticClass();
	FNativeFunctionRegistrar::RegisterFunctions(Class, MakeConstArrayView(Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::Funcs));
}
UClass* Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary()
{
	if (!Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.OuterSingleton)
	{
		UECodeGen_Private::ConstructUClass(Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.OuterSingleton, Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary_Statics::ClassParams);
	}
	return Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary.OuterSingleton;
}
ULocalizationOverridesBlueprintLibrary::ULocalizationOverridesBlueprintLibrary(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {}
DEFINE_VTABLE_PTR_HELPER_CTOR_NS(, ULocalizationOverridesBlueprintLibrary);
ULocalizationOverridesBlueprintLibrary::~ULocalizationOverridesBlueprintLibrary() {}
// ********** End Class ULocalizationOverridesBlueprintLibrary *************************************

// ********** Begin Registration *******************************************************************
struct Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h__Script_LocalizationOverrides_Statics
{
	static constexpr FClassRegisterCompiledInInfo ClassInfo[] = {
		{ Z_Construct_UClass_ULocalizationOverridesBlueprintLibrary, ULocalizationOverridesBlueprintLibrary::StaticClass, TEXT("ULocalizationOverridesBlueprintLibrary"), &Z_Registration_Info_UClass_ULocalizationOverridesBlueprintLibrary, CONSTRUCT_RELOAD_VERSION_INFO(FClassReloadVersionInfo, sizeof(ULocalizationOverridesBlueprintLibrary), 1862698346U) },
	};
}; // Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h__Script_LocalizationOverrides_Statics 
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h__Script_LocalizationOverrides_2765272537{
	TEXT("/Script/LocalizationOverrides"),
	Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h__Script_LocalizationOverrides_Statics::ClassInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverrides_Public_LocalizationOverridesBlueprintLibrary_h__Script_LocalizationOverrides_Statics::ClassInfo),
	nullptr, 0,
	nullptr, 0,
};
// ********** End Registration *********************************************************************

PRAGMA_ENABLE_DEPRECATION_WARNINGS
