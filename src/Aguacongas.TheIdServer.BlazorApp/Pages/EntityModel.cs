﻿using Aguacongas.IdentityServer.Admin.Http.Store;
using Aguacongas.IdentityServer.Store;
using Aguacongas.IdentityServer.Store.Entity;
using Aguacongas.TheIdServer.BlazorApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aguacongas.TheIdServer.BlazorApp.Pages
{
    [Authorize(Policy = "Is4-Reader")]
    public abstract class EntityModel<T> : ComponentBase, IComparer<Type> where T : class, new()
    {
        const int HEADER_HEIGHT = 95;

        [Inject]
        protected Notifier Notifier { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IAdminStore<T> AdminStore { get; set; }

        [Inject]
        protected IServiceProvider Provider { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public string Id { get; set; }

        protected bool IsNew { get; private set; }

        protected T Model { get; private set; }

        protected T State { get; set; }

        protected EditContext EditContext { get; private set; }

        protected abstract string Expand { get; }

        protected abstract bool NonEditable { get; }

#pragma warning disable CA1056 // Uri properties should not be strings. Nope, because it's used as parameter of NavigationManager.NavigateTo
        protected abstract string BackUrl { get; }
#pragma warning restore CA1056 // Uri properties should not be strings

        protected HandleModificationState HandleModificationState { get; }

        protected EntityModel()
        {
            HandleModificationState = new HandleModificationState
            {
                OnStateChange = OnStateChange
            };
        }

        public virtual int Compare(Type x, Type y)
        {
            if (x == typeof(T))
            {
                return -1;
            }
            if (y == typeof(T))
            {
                return 1;
            }
            return 0;
        }

        protected override async Task OnInitializedAsync()
        {
            if (Id == null)
            {
                IsNew = true;
                Model = Create();
                CreateEditContext(Model);
                EntityCreated(Model);
                State = CloneModel(Model);
                return;
            }

            Model = await GetModelAsync()
                .ConfigureAwait(false);
            CreateEditContext(Model);
            State = CloneModel(Model);
        }

        protected async Task HandleValidSubmit()
        {
            var changes = HandleModificationState.Changes;
            if (!changes.Any())
            {
                Notifier.Notify(new Models.Notification
                {
                    Header = GetModelId(Model),
                    IsError = false,
                    Message = "No changes"
                });
                return;
            }

            State = CloneModel(Model);
            Model = CloneModel(State);
            Id = GetModelId(Model);
            IsNew = false;

            var keys = changes.Keys
                .OrderBy(k => k, this);

            try
            {
                foreach (var key in keys)
                {
                    await HandleMoficationList(key, changes[key])
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                changes.Clear();
            }

            Notifier.Notify(new Models.Notification
            {
                Header = GetModelId(Model),
                Message = "Saved"
            });

            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }

        protected void EntityCreated<TEntity>(TEntity entity) where TEntity : class
        {
            HandleModificationState.EntityCreated(entity);
        }

        protected void EntityDeleted<TEntity>(TEntity entity) where TEntity : class, IEntityId
        {
            HandleModificationState.EntityDeleted(entity);
        }

        protected ValueTask ScrollTo(string id)
        {
            return JSRuntime.InvokeVoidAsync("browserInteropt.scrollTo", id, -HEADER_HEIGHT);
        }

        protected async Task DeleteEntity()
        {
            try
            {
                await AdminStore.DeleteAsync(GetModelId(Model))
                    .ConfigureAwait(false);
            }
            catch (ProblemException pe)
            {
                Notifier.Notify(new Models.Notification
                {
                    Header = "Error",
                    IsError = true,
                    Message = pe.Details.Detail
                });
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types. We want to notify all error
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Notifier.Notify(new Models.Notification
                {
                    Header = "Error",
                    IsError = true,
                    Message = e.Message
                });
                throw;
            }

            NavigationManager.NavigateTo(BackUrl);

            Notifier.Notify(new Models.Notification
            {
                Header = GetModelId(Model),
                Message = "Deleted"
            });
        }

        protected virtual void OnStateChange()
        {
            StateHasChanged();
        }

        protected virtual Task<T> GetModelAsync()
        {
            return AdminStore.GetAsync(Id, new GetRequest
            {
                Expand = Expand
            });
        }

        protected virtual T CloneModel(T entity)
        {
            if (entity is ICloneable<T> cloneable)
            {
                return cloneable.Clone();
            }
            throw new NotSupportedException();
        }
        protected string GetModelId<TEntity>(TEntity model)
        {
            if (model is IEntityId entity)
            {
                return entity.Id;
            }
            throw new NotSupportedException();
        }

        protected virtual void SetModelEntityId(Type entityType, object result)
        {
        }

        protected virtual void SetCreatedEntityId(object entity, object result)
        {
            if (entity is IEntityId entityId)
            {
                entityId.Id = ((IEntityId)result).Id;
                return;
            }
            throw new NotSupportedException();
        }

        protected virtual Type GetEntityType(FieldIdentifier identifier)
        {
            return identifier.Model.GetType();
        }

        protected virtual IEntityId GetEntityModel(FieldIdentifier identifier)
        {
            return identifier.Model as IEntityId;
        }

        protected virtual Task<object> UpdateAsync(Type entityType, object entity)
        {
            return StoreAsync(entityType, entity, (store, e) =>
            {
                return store.UpdateAsync(e);
            });
        }

        protected virtual Task<object> DeleteAsync(Type entityType, object entity)
        {
            return StoreAsync(entityType, entity, async (store, e) =>
            {
                await store.DeleteAsync(GetModelId(e))
                    .ConfigureAwait(false);
                return e;
            });
        }

        protected virtual Task<object> CreateAsync(Type entityType, object entity)
        {
            return StoreAsync(entityType, entity, (store, e) =>
            {
                return store.CreateAsync(e);
            });
        }

        protected virtual IAdminStore GetStore(Type entityType)
        {
            return Provider.GetRequiredService(typeof(IAdminStore<>).MakeGenericType(entityType)) as IAdminStore;
        }

        protected virtual IAdminStore<TEntity> GetStore<TEntity>() where TEntity : class
        {
            return GetStore(typeof(TEntity)) as IAdminStore<TEntity>;
        }

        protected virtual void OnEntityUpdated(Type entityType, IEntityId entityModel)
        {
            HandleModificationState.EntityUpdated(entityType, entityModel);
        }

        protected abstract T Create();
        protected abstract void SetNavigationProperty<TEntity>(TEntity entity);

        protected abstract void SanetizeEntityToSaved<TEntity>(TEntity entity);

        private static IEnumerable<object> GetModifiedEntities(Dictionary<object, ModificationKind> modificationList, ModificationKind kind)
        {
            return modificationList
                            .Where(m => m.Value == kind)
                            .Select(m => m.Key);
        }

        private async Task HandleMoficationList(Type entityType, Dictionary<object, ModificationKind> modificationList)
        {
            Console.WriteLine($"HandleMoficationList for type {entityType.Name}");
            var addList = GetModifiedEntities(modificationList, ModificationKind.Add);
            foreach (var entity in addList)
            {
                SetNavigationProperty(entity);
                SanetizeEntityToSaved(entity);
                try
                {
                    var result = await CreateAsync(entityType, entity).ConfigureAwait(false);

                    SetCreatedEntityId(entity, result);
                    SetModelEntityId(entityType, result);
                }
                catch (AggregateException e)
                {
                    HandleModificationError(e);
                }
            }
            var updateList = GetModifiedEntities(modificationList, ModificationKind.Update);
            foreach (var entity in updateList)
            {
                SanetizeEntityToSaved(entity);
                try
                {
                    await UpdateAsync(entityType, entity).ConfigureAwait(false);
                }
                catch (AggregateException e)
                {
                    HandleModificationError(e);
                }
            }
            var deleteList = GetModifiedEntities(modificationList, ModificationKind.Delete);
            foreach (var entity in deleteList)
            {
                try
                {
                    await DeleteAsync(entityType, entity).ConfigureAwait(false);
                }
                catch (AggregateException e)
                {
                    HandleModificationError(e);
                }
            }
        }

        private void HandleModificationError(AggregateException exception)
        {
            if(exception == null)
            {
                return;
            }

            foreach(var e in exception.InnerExceptions)
            {
                if (e is ProblemException pe)
                {
                    Notifier.Notify(new Models.Notification
                    {
                        Header = "Error",
                        IsError = true,
                        Message = pe.Details.Detail
                    });

                    continue;
                }

                Notifier.Notify(new Models.Notification
                {
                    Header = "Error",
                    IsError = true,
                    Message = e.Message
                });
            }

            throw exception;
        }

        private void CreateEditContext(T model)
        {
            EditContext = new EditContext(model);
            EditContext.OnFieldChanged += (s, e) =>
            {
                var identifier = e.FieldIdentifier;
                var entityType = GetEntityType(identifier);
                var entityModel = GetEntityModel(identifier);
                OnEntityUpdated(entityType, entityModel);
            };
        }

        private Task<object> StoreAsync(Type entityType, object entity, Func<IAdminStore, object, Task<object>> action)
        {
            entity = entity ?? throw new ArgumentNullException(nameof(entity));

            if (NonEditable)
            {
                throw new InvalidOperationException("The entity is non editable");
            }

            var store = GetStore(entityType);
            return action.Invoke(store, entity);
        }
    }
}
