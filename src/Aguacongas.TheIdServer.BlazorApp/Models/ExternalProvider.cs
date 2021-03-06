﻿using Aguacongas.IdentityServer.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entity = Aguacongas.IdentityServer.Store.Entity;

namespace Aguacongas.TheIdServer.BlazorApp.Models
{
    public class ExternalProvider : Entity.ExternalProvider, ICloneable<ExternalProvider>
    {
        [JsonIgnore]
        public RemoteAuthenticationOptions Options { get; set; }

        [JsonIgnore]
        public IEnumerable<Entity.ExternalProviderKind> Kinds { get; set; }

        [JsonIgnore]
        public RemoteAuthenticationOptions DefaultOptions
        {
            get
            {
                var optionsType = GetOptionsType(this);
                return Deserialize(Kinds.First(k => k.KindName == KindName).SerializedDefaultOptions, optionsType);
            }
        }

        public override string SerializedHandlerType 
        {
            get => Kinds.First(k => k.KindName == KindName).SerializedHandlerType; 
            set => base.SerializedHandlerType = value; 
        }

        public ExternalProvider Clone()
        {
            return MemberwiseClone() as ExternalProvider;
        }

        public static ExternalProvider FromEntity(Entity.ExternalProvider externalProvider)
        {
            var optionsType = GetOptionsType(externalProvider);
            return new ExternalProvider
            {
                DisplayName = externalProvider.DisplayName,
                Id = externalProvider.Id,
                KindName = externalProvider.KindName,
                Options = Deserialize(externalProvider.SerializedOptions, optionsType)
            };
        }

        private static RemoteAuthenticationOptions Deserialize(string options, Type optionsType)
        {
            return JsonSerializer.Deserialize(options, optionsType) as RemoteAuthenticationOptions;
        }

        private static Type GetOptionsType(Entity.ExternalProvider externalProvider)
        {
            return Type.GetType($"{typeof(RemoteAuthenticationOptions).Namespace}.{externalProvider.KindName}Options");
        }
    }
}
