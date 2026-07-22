#pragma once

#include "Delegates/Delegate.h"
#include "Modules/ModuleManager.h"

class FLocalizationOverridesModule final : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

private:
	FDelegateHandle PostEngineInitHandle;
};
