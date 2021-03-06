﻿using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Linq;
using System.Security.Claims;

namespace Aguacongas.IdentityServer.Store
{
    public static class EntityExtensions
    {
        public static Entity.IdentityProvider ToIdentityProvider(this AuthenticationScheme scheme)
        {
            if (scheme == null)
            {
                return null;
            }
            return new Entity.IdentityProvider
            {
                Id = scheme.Name,
                DisplayName = scheme.DisplayName
            };
        }

        public static Client ToClient(this Entity.Client client)
        {
            if (client == null)
            {
                return null;
            }

            var redirectUris = client.RedirectUris
                    .Where(u => (u.Kind & Entity.UriKinds.Redirect) == Entity.UriKinds.Redirect);

            return new Client
            {
                AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = client.AccessTokenLifetime,
                AccessTokenType = (AccessTokenType)client.AccessTokenType,
                AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = client.RedirectUris
                    .Where(u => (u.Kind & Entity.UriKinds.Cors) == Entity.UriKinds.Cors)
                    .Select(u => new Uri(u.Uri))
                    .Select(u => $"{u.Scheme}://{u.Host}{u.UriPortString()}")
                    .ToList(),
                AllowedGrantTypes = client.AllowedGrantTypes.Select(g => g.GrantType).ToList(),
                AllowedScopes = client.AllowedScopes.Select(s => s.Scope).ToList(),
                AllowOfflineAccess = client.AllowOfflineAccess,
                AllowPlainTextPkce = client.AllowPlainTextPkce,
                AllowRememberConsent = client.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = client.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = client.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = client.BackChannelLogoutUri,
                Claims = client.ClientClaims.Select(c => new Claim(c.Type, c.Value)).ToList(),
                ClientClaimsPrefix = client.ClientClaimsPrefix,
                ClientId = client.Id,
                ClientName = client.ClientName,
                ClientSecrets = client.ClientSecrets.Select(s => new Secret(s.Value, s.Expiration)).ToList(),
                ClientUri = client.ClientUri,
                ConsentLifetime = client.ConsentLifetime,
                Description = client.Description,
                DeviceCodeLifetime = client.DeviceCodeLifetime,
                Enabled = client.Enabled,
                EnableLocalLogin = client.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = client.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = client.FrontChannelLogoutUri,
                IdentityProviderRestrictions = client.IdentityProviderRestrictions.Select(r => r.Provider).ToList(),
                IdentityTokenLifetime = client.IdentityTokenLifetime,
                IncludeJwtId = client.IncludeJwtId,
                LogoUri = client.LogoUri,
                PairWiseSubjectSalt = client.PairWiseSubjectSalt,
                PostLogoutRedirectUris = redirectUris
                    .Select(u => u.Uri)
                    .ToList(),
                Properties = client.Properties.ToDictionary(p => p.Key, p => p.Value),
                ProtocolType = client.ProtocolType,
                RefreshTokenExpiration = (TokenExpiration)client.RefreshTokenExpiration,
                RedirectUris = client.RedirectUris
                    .Where(u => (u.Kind & Entity.UriKinds.Redirect) == Entity.UriKinds.Redirect)
                    .Select(u => u.Uri).ToList(),
                RefreshTokenUsage = (TokenUsage)client.RefreshTokenUsage,
                RequireClientSecret = client.RequireClientSecret,
                RequireConsent = client.RequireConsent,
                RequirePkce = client.RequirePkce,
                SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
                UserCodeType = client.UserCodeType,
                UserSsoLifetime = client.UserSsoLifetime,
            };
        }

        public static ApiResource ToApi(this Entity.ProtectResource api)
        {
            if (api == null)
            {
                return null;
            }

            return new ApiResource
            {
                ApiSecrets = api.Secrets.Select(s => new Secret { Value = s.Value, Expiration = s.Expiration }).ToList(),
                Description = api.Description,
                DisplayName = api.DisplayName,
                Enabled = api.Enabled,
                Name = api.Id,
                Properties = api.Properties.ToDictionary(p => p.Key, p => p.Value),
                Scopes = api.Scopes.Select(s => new Scope
                {
                    Description = s.Description,
                    DisplayName = s.DisplayName,
                    Emphasize = s.Emphasize,
                    Name = s.Scope,
                    Required = s.Required,
                    ShowInDiscoveryDocument = s.ShowInDiscoveryDocument,
                    UserClaims = api.ApiScopeClaims
                        .Where(s => s.ApiScpopeId == s.Id)
                        .Select(c => c.Type).ToList()
                }).ToList(),
                UserClaims = api.ApiClaims.Select(c => c.Type).ToList()
            };
        }

        public static IdentityResource ToIdentity(this Entity.IdentityResource identity)
        {
            if (identity == null)
            {
                return null;
            }

            return new IdentityResource
            {
                Description = identity.Description,
                DisplayName = identity.DisplayName,
                Emphasize = identity.Emphasize,
                Enabled = identity.Enabled,
                Name = identity.Id,
                Properties = identity.Properties.ToDictionary(p => p.Key, p => p.Value),
                Required = identity.Required,
                ShowInDiscoveryDocument = identity.ShowInDiscoveryDocument,
                UserClaims = identity.IdentityClaims.Select(c => c.Type).ToList()
            };
        }

        public static bool CorsMatch(this Uri cors, string url)
        {
            cors = cors ?? throw new ArgumentNullException(nameof(cors));
            url = url ?? throw new ArgumentNullException(nameof(url));
            var uri = new Uri(url);
            return uri.Scheme.ToUpperInvariant() == cors.Scheme.ToUpperInvariant() &&
                uri.Host.ToUpperInvariant() == cors.Host.ToUpperInvariant() &&
                uri.Port == cors.Port;
        }

        public static bool CorsMatch(this Uri cors, Uri uri)
        {
            cors = cors ?? throw new ArgumentNullException(nameof(cors));
            uri = uri ?? throw new ArgumentNullException(nameof(uri));

            return uri.Scheme.ToUpperInvariant() == cors.Scheme.ToUpperInvariant() &&
                uri.Host.ToUpperInvariant() == cors.Host.ToUpperInvariant() &&
                uri.Port == cors.Port;
        }

        private static string UriPortString(this Uri uri)
        {
            return uri.IsDefaultPort ? "" : $":{uri.Port}";
        }
    }
}
