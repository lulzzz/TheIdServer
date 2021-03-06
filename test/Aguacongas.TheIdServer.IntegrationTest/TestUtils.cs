﻿using Aguacongas.TheIdServer.BlazorApp.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using RichardSzalay.MockHttp;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit.Abstractions;
using blazorApp = Aguacongas.TheIdServer.BlazorApp;

namespace Aguacongas.TheIdServer.IntegrationTest
{
    public static class TestUtils
    {

        public static TestServer CreateTestServer(
                    Action<IServiceCollection> configureServices = null,
                    IEnumerable<KeyValuePair<string, string>> configurationOverrides = null)
        {
            Startup startup = null;
            var webHostBuilder = new WebHostBuilder()
                .UseEnvironment("Development")
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\src\Aguacongas.TheIdServer\appsettings.json"));
                    builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, @"appsettings.Test.json"), true);
                    if (configurationOverrides != null)
                    {
                        builder.AddInMemoryCollection(configurationOverrides);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    startup = new Startup(context.Configuration, context.HostingEnvironment);
                    configureServices?.Invoke(services);
                    startup.ConfigureServices(services);
                    services.AddSingleton<TestUserService>()
                        .AddMvc().AddApplicationPart(startup.GetType().Assembly);
                    configureServices?.Invoke(services);
                })
                .Configure(builder =>
                {
                    builder.Use(async (context, next) =>
                    {
                        var testService = context.RequestServices.GetRequiredService<TestUserService>();
                        context.User = testService.User;
                        await next();
                    });
                    startup.Configure(builder);
                });

            var testServer = new TestServer(webHostBuilder);

            return testServer;
        }

        public static void CreateTestHost(string userName,
            IEnumerable<Claim> claims,
            string url,
            TestServer sut,
            ITestOutputHelper testOutputHelper,
            out TestHost host,
            out RenderedComponent<blazorApp.App> component,
            out MockHttpMessageHandler mockHttp)
        {
            CreateTestHost(userName, claims, url, sut, testOutputHelper, out host, out mockHttp);

            component = host.AddComponent<blazorApp.App>();
        }

        public static void CreateTestHost(string userName, IEnumerable<Claim> claims, string url, TestServer sut, ITestOutputHelper testOutputHelper, out TestHost host, out MockHttpMessageHandler mockHttp)
        {
            var navigationInterceptionMock = new Mock<INavigationInterception>();
            var jsRuntimeMock = new Mock<IJSRuntime>();
            host = new TestHost();
            var httpMock = host.AddMockHttp();
            mockHttp = httpMock;
            host.ConfigureServices(services =>
            {
                blazorApp.Program.ConfigureServices(services);
                var httpClient = sut.CreateClient();
                
                sut.Services.GetRequiredService<TestUserService>()
                    .SetTestUser(true, claims.Select(c => new Claim(c.Type, c.Value)));

                services
                    .AddLogging(configure =>
                    {
                        configure.AddProvider(new TestLoggerProvider(testOutputHelper));
                    })
                    .AddIdentityServer4AdminHttpStores(p =>
                    {
                        
                        var client = new HttpClient(new blazorApp.OidcDelegationHandler(p.GetRequiredService<IAccessTokenProvider>(), sut.CreateHandler()));
                        client.BaseAddress = new Uri(httpClient.BaseAddress, "api");
                        return Task.FromResult(client);
                    })
                    .AddSingleton(p => new TestNavigationManager(uri: url))
                    .AddSingleton<NavigationManager>(p => p.GetRequiredService<TestNavigationManager>())
                    .AddSingleton(p => navigationInterceptionMock.Object)
                    .AddSingleton(p => jsRuntimeMock.Object)                    
                    .AddSingleton<Settings>()
                    .AddSingleton<SignOutSessionStateManager, FakeSignOutSessionStateManager>()
                    .AddSingleton<AuthenticationStateProvider>(p => new FakeAuthenticationStateProvider(userName, claims));
            });
        }

        public class FakeAuthenticationStateProvider : AuthenticationStateProvider, IAccessTokenProvider
        {
            private readonly AuthenticationState _state;

            public FakeAuthenticationStateProvider(string userName, IEnumerable<Claim> claims)
            {
                if (claims != null && !claims.Any(c => c.Type == "name"))
                {
                    var list = claims.ToList();
                    list.Add(new Claim("name", userName));
                    claims = list;
                }
                _state = new AuthenticationState(new FakeClaimsPrincipal(new FakeIdendity(userName, claims)));
            }

            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                return Task.FromResult(_state);
            }

            public ValueTask<AccessTokenResult> RequestAccessToken()
            {
                return new ValueTask<AccessTokenResult>(new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken
                {
                    Expires = DateTimeOffset.Now.AddDays(1),
                    GrantedScopes = new string[] { "openid", "profile", "theidseveradminaoi" },
                    Value = "test"
                }, "http://exemple.com"));
            }

            public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public class FakeClaimsPrincipal : ClaimsPrincipal
        {
            public FakeClaimsPrincipal(FakeIdendity idendity) : base(idendity)
            {

            }

            public override bool IsInRole(string role)
            {
                return Identity.IsAuthenticated && Claims != null && Claims.Any(c => c.Type == "role" && c.Value == role);
            }
        }

        public class FakeIdendity : ClaimsIdentity
        {
            private readonly string _userName;
            private bool _IsAuthenticated = true;

            public FakeIdendity(string userName, IEnumerable<Claim> claims) : base(claims)
            {
                _userName = userName;
            }
            public override string AuthenticationType => "Bearer";

            public override bool IsAuthenticated => _IsAuthenticated;

            public void SetIsAuthenticated(bool value)
            {
                _IsAuthenticated = value;
            }

            public override string Name => _userName;
        }

        class FakeSignOutSessionStateManager : SignOutSessionStateManager
        {
            public FakeSignOutSessionStateManager(IJSRuntime jsRuntime) : base(jsRuntime)
            { }

            public override ValueTask SetSignOutState()
            {
                return new ValueTask();
            }

            public override Task<bool> ValidateSignOutState()
            {
                return Task.FromResult(true);
            }
        }
    }

}
