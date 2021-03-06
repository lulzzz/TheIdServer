﻿using Aguacongas.AspNetCore.Authentication;
using Aguacongas.IdentityServer.Abstractions;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Aguacongas.IdentityServer.Admin.Services
{
    /// <summary>
    /// Subscribe to provider configuration change
    /// </summary>
    public class SchemeChangeSubscriber<TSchemeDefinition> where TSchemeDefinition : SchemeDefinitionBase, new()
    {
        private readonly HubConnectionFactory _factory;
        private readonly NoPersistentDynamicManager<TSchemeDefinition> _manager;
        private readonly IDynamicProviderStore<TSchemeDefinition> _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemeChangeSubscriber{TSchemeDefinition}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="manager">The manager.</param>
        /// <param name="store">The store.</param>
        /// <exception cref="ArgumentNullException">
        /// factory
        /// or
        /// manager
        /// or
        /// manager
        /// </exception>
        public SchemeChangeSubscriber(HubConnectionFactory factory,
            NoPersistentDynamicManager<TSchemeDefinition> manager,
            IDynamicProviderStore<TSchemeDefinition> store)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _store = store ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <summary>
        /// Subscribes this instance.
        /// </summary>
        public void Subscribe()
        {
            Task.Delay(1000).ContinueWith(t =>
            {
                var connection = _factory.GetConnection();
                if (connection == null)
                {
                    return;
                }

                connection.On<string>(nameof(IProviderHub.ProviderAdded), async scheme =>
                {
                    var definition = await _store.FindBySchemeAsync(scheme).ConfigureAwait(false);
                    await _manager.AddAsync(definition).ConfigureAwait(false);
                });

                connection.On<string>(nameof(IProviderHub.ProviderRemoved), async scheme =>
                {
                    await _manager.RemoveAsync(scheme).ConfigureAwait(false);
                });

                connection.On<string>(nameof(IProviderHub.ProviderUpdated), async scheme =>
                {
                    var definition = await _store.FindBySchemeAsync(scheme).ConfigureAwait(false);
                    await _manager.UpdateAsync(definition).ConfigureAwait(false);
                });
            });
        }
    }
}
