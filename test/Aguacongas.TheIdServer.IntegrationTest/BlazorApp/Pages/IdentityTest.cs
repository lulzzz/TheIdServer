﻿using Aguacongas.IdentityServer.EntityFramework.Store;
using Aguacongas.IdentityServer.Store;
using Aguacongas.IdentityServer.Store.Entity;
using Aguacongas.TheIdServer.BlazorApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.EntityFrameworkCore;
using RichardSzalay.MockHttp;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Aguacongas.TheIdServer.IntegrationTest.BlazorApp.Pages
{
    [Collection("api collection")]
    public class IdentityTest : EntityPageTestBase
    {
        public override string Entity => "identityresource";
        public IdentityTest(ApiFixture fixture, ITestOutputHelper testOutputHelper):base(fixture, testOutputHelper)
        {
        }



        [Fact]
        public async Task OnFilterChanged_should_filter_properties_and_claims()
        {
            string identityId = await CreateEntity();

            CreateTestHost("Alice Smith",
                SharedConstants.WRITER,
                identityId,
                out TestHost host,
                out RenderedComponent<App> component,
                out MockHttpMessageHandler mockHttp);

            string markup = WaitForLoaded(host, component);

            WaitForContains(host, component, "filtered");

            var filterInput = component.Find("input[placeholder=\"filter\"]");

            Assert.NotNull(filterInput);

            await host.WaitForNextRenderAsync(() => filterInput.TriggerEventAsync("oninput", new ChangeEventArgs
            {
                Value = identityId
            }));

            markup = component.GetMarkup();

            Assert.DoesNotContain("filtered", markup);
        }

        [Fact]
        public async Task SaveClick_should_update_entity()
        {
            string identityId = await CreateEntity();

            CreateTestHost("Alice Smith",
                SharedConstants.WRITER,
                identityId,
                out TestHost host,
                out RenderedComponent<App> component,
                out MockHttpMessageHandler mockHttp);

            WaitForLoaded(host, component);

            var input = component.Find("#displayName");

            Assert.NotNull(input);

            var expected = GenerateId();
            await host.WaitForNextRenderAsync(() => input.ChangeAsync(expected));

            var markup = component.GetMarkup();

            Assert.Contains(expected, markup);

            var form = component.Find("form");

            Assert.NotNull(form);

            await host.WaitForNextRenderAsync(() => form.SubmitAsync());

            WaitForSavedToast(host, component);

            await DbActionAsync<ConfigurationDbContext>(async context =>
            {
                var identity = await context.Identities.FirstOrDefaultAsync(a => a.Id == identityId);
                Assert.Equal(expected, identity.DisplayName);
            });
        }

        private async Task<string> CreateEntity()
        {
            var identityId = GenerateId();
            await DbActionAsync<ConfigurationDbContext>(context =>
            {
                context.Identities.Add(new IdentityResource
                {
                    Id = identityId,
                    DisplayName = identityId,
                    IdentityClaims = new List<IdentityClaim>
                    {
                        new IdentityClaim { Id = GenerateId(), Type = "filtered" }
                    },
                    Properties = new List<IdentityProperty>
                    {
                        new IdentityProperty { Id = GenerateId(), Key = "filtered", Value = "filtered" }
                    }
                });

                return context.SaveChangesAsync();
            });
            return identityId;
        }
    }
}
