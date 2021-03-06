﻿using Aguacongas.TheIdServer.BlazorApp.Models;
using FluentValidation;

namespace Aguacongas.TheIdServer.BlazorApp.Validators
{
    public class ExternalProviderValidator: AbstractValidator<ExternalProvider>
    {
        public ExternalProviderValidator(ExternalProvider externalProvider)
        {
            RuleFor(m => m.DisplayName).NotEmpty().WithMessage("The display name is required.");
            RuleFor(m => m.KindName).NotEmpty().WithMessage("The kind of provider is required.");
            RuleFor(m => m.Options).SetValidator(p => new RemoteAuthenticationOptionsValidator(externalProvider));
        }
    }
}
