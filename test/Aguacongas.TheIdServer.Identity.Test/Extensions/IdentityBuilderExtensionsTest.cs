﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aguacongas.TheIdServer.Identity.Test.Extensions
{
    public class IdentityBuilderExtensionsTest
    {
        [Fact]
        public void AddTheIdServerStores_should_add_identity_stores()
        {
            var services = new ServiceCollection();
            services.AddIdentityProviderStore()
                .AddConfigurationHttpStores(options => options.ApiUrl = "http://exemple.com")
                .AddOperationalHttpStores()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddTheIdServerStores(options => options.ApiUrl = "http://exemple.com");

            var userStore = services.BuildServiceProvider().GetService<IUserStore<IdentityUser>>();
            Assert.NotNull(userStore);

            var roleStore = services.BuildServiceProvider().GetService<IRoleStore<IdentityRole>>();
            Assert.NotNull(roleStore);


            services = new ServiceCollection();
            services.AddIdentityProviderStore()
                .AddConfigurationHttpStores(options => options.ApiUrl = "http://exemple.com")
                .AddOperationalHttpStores()
                .AddIdentityCore<IdentityUser>()
                .AddTheIdServerStores(options => options.ApiUrl = "http://exemple.com");


            var userOnlyStore = services.BuildServiceProvider().GetService<IUserStore<IdentityUser>>();
            Assert.NotNull(userOnlyStore);
        }
    }
}
