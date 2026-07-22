#include "LocalizationOverridesModule.h"

#include "LocalizationOverridesSubsystem.h"
#include "Misc/CoreDelegates.h"

void FLocalizationOverridesModule::StartupModule()
{
	FLocalizationOverridesSubsystem::Initialize();
	PostEngineInitHandle = FCoreDelegates::OnPostEngineInit.AddLambda([]()
	{
		FLocalizationOverridesSubsystem::ReloadOverrides();
	});
}

void FLocalizationOverridesModule::ShutdownModule()
{
	if (PostEngineInitHandle.IsValid())
	{
		FCoreDelegates::OnPostEngineInit.Remove(PostEngineInitHandle);
		PostEngineInitHandle.Reset();
	}
}

IMPLEMENT_MODULE(FLocalizationOverridesModule, LocalizationOverrides)
