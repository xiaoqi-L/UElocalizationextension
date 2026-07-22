#pragma once

#include "Commandlets/Commandlet.h"
#include "LocalizationOverridesGenerateCommandlet.generated.h"

/** Headless generator used by the packaging staging hook. */
UCLASS()
class LOCALIZATIONOVERRIDESEDITOR_API ULocalizationOverridesGenerateCommandlet : public UCommandlet
{
	GENERATED_BODY()

public:
	ULocalizationOverridesGenerateCommandlet();

	virtual int32 Main(const FString& Params) override;
};
