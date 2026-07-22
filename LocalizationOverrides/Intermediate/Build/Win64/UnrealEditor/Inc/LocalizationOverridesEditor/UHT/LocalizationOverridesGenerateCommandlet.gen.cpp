// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "LocalizationOverridesGenerateCommandlet.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
static_assert(!UE_WITH_CONSTINIT_UOBJECT, "This generated code can only be compiled with !UE_WITH_CONSTINIT_OBJECT");
void EmptyLinkFunctionForGeneratedCodeLocalizationOverridesGenerateCommandlet() {}

// ********** Begin Cross Module References ********************************************************
ENGINE_API UClass* Z_Construct_UClass_UCommandlet();
LOCALIZATIONOVERRIDESEDITOR_API UClass* Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet();
LOCALIZATIONOVERRIDESEDITOR_API UClass* Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_NoRegister();
UPackage* Z_Construct_UPackage__Script_LocalizationOverridesEditor();
// ********** End Cross Module References **********************************************************

// ********** Begin Class ULocalizationOverridesGenerateCommandlet *********************************
FClassRegistrationInfo Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet;
UClass* ULocalizationOverridesGenerateCommandlet::GetPrivateStaticClass()
{
	using TClass = ULocalizationOverridesGenerateCommandlet;
	if (!Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.InnerSingleton)
	{
		GetPrivateStaticClassBody(
			TClass::StaticPackage(),
			TEXT("LocalizationOverridesGenerateCommandlet"),
			Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.InnerSingleton,
			StaticRegisterNativesULocalizationOverridesGenerateCommandlet,
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
	return Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.InnerSingleton;
}
UClass* Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_NoRegister()
{
	return ULocalizationOverridesGenerateCommandlet::GetPrivateStaticClass();
}
struct Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Class_MetaDataParams[] = {
#if !UE_BUILD_SHIPPING
		{ "Comment", "/** Headless generator used by the packaging staging hook. */" },
#endif
		{ "IncludePath", "LocalizationOverridesGenerateCommandlet.h" },
		{ "ModuleRelativePath", "Private/LocalizationOverridesGenerateCommandlet.h" },
#if !UE_BUILD_SHIPPING
		{ "ToolTip", "Headless generator used by the packaging staging hook." },
#endif
	};
#endif // WITH_METADATA

// ********** Begin Class ULocalizationOverridesGenerateCommandlet constinit property declarations *
// ********** End Class ULocalizationOverridesGenerateCommandlet constinit property declarations ***
	static UObject* (*const DependentSingletons[])();
	static constexpr FCppClassTypeInfoStatic StaticCppClassTypeInfo = {
		TCppClassTypeTraits<ULocalizationOverridesGenerateCommandlet>::IsAbstract,
	};
	static const UECodeGen_Private::FClassParams ClassParams;
}; // struct Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics
UObject* (*const Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::DependentSingletons[])() = {
	(UObject* (*)())Z_Construct_UClass_UCommandlet,
	(UObject* (*)())Z_Construct_UPackage__Script_LocalizationOverridesEditor,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::DependentSingletons) < 16);
const UECodeGen_Private::FClassParams Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::ClassParams = {
	&ULocalizationOverridesGenerateCommandlet::StaticClass,
	nullptr,
	&StaticCppClassTypeInfo,
	DependentSingletons,
	nullptr,
	nullptr,
	nullptr,
	UE_ARRAY_COUNT(DependentSingletons),
	0,
	0,
	0,
	0x001000A8u,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::Class_MetaDataParams), Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::Class_MetaDataParams)
};
void ULocalizationOverridesGenerateCommandlet::StaticRegisterNativesULocalizationOverridesGenerateCommandlet()
{
}
UClass* Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet()
{
	if (!Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.OuterSingleton)
	{
		UECodeGen_Private::ConstructUClass(Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.OuterSingleton, Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet_Statics::ClassParams);
	}
	return Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet.OuterSingleton;
}
DEFINE_VTABLE_PTR_HELPER_CTOR_NS(, ULocalizationOverridesGenerateCommandlet);
ULocalizationOverridesGenerateCommandlet::~ULocalizationOverridesGenerateCommandlet() {}
// ********** End Class ULocalizationOverridesGenerateCommandlet ***********************************

// ********** Begin Registration *******************************************************************
struct Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverridesEditor_Private_LocalizationOverridesGenerateCommandlet_h__Script_LocalizationOverridesEditor_Statics
{
	static constexpr FClassRegisterCompiledInInfo ClassInfo[] = {
		{ Z_Construct_UClass_ULocalizationOverridesGenerateCommandlet, ULocalizationOverridesGenerateCommandlet::StaticClass, TEXT("ULocalizationOverridesGenerateCommandlet"), &Z_Registration_Info_UClass_ULocalizationOverridesGenerateCommandlet, CONSTRUCT_RELOAD_VERSION_INFO(FClassReloadVersionInfo, sizeof(ULocalizationOverridesGenerateCommandlet), 3822261868U) },
	};
}; // Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverridesEditor_Private_LocalizationOverridesGenerateCommandlet_h__Script_LocalizationOverridesEditor_Statics 
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverridesEditor_Private_LocalizationOverridesGenerateCommandlet_h__Script_LocalizationOverridesEditor_2279741224{
	TEXT("/Script/LocalizationOverridesEditor"),
	Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverridesEditor_Private_LocalizationOverridesGenerateCommandlet_h__Script_LocalizationOverridesEditor_Statics::ClassInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_localizedlanguage_Plugins_LocalizationOverrides_Source_LocalizationOverridesEditor_Private_LocalizationOverridesGenerateCommandlet_h__Script_LocalizationOverridesEditor_Statics::ClassInfo),
	nullptr, 0,
	nullptr, 0,
};
// ********** End Registration *********************************************************************

PRAGMA_ENABLE_DEPRECATION_WARNINGS
