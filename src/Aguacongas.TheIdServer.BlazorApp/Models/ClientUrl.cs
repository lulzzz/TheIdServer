﻿using Entity = Aguacongas.IdentityServer.Store.Entity;

namespace Aguacongas.TheIdServer.BlazorApp.Models
{
    public class ClientUri
    {
        public ClientUri(Entity.ClientUri parent)
        {
            Parent = parent;
            Uri = parent.Uri;
        }
        public Entity.ClientUri Parent { get; }

        public string Id => Parent.Id;
        public string Uri
        {
            get => Parent.Uri;
            set => Parent.Uri = value;
        }
        public bool Cors
        {
            get { return (Parent.Kind & Entity.UriKinds.Cors) == Entity.UriKinds.Cors; }
            set 
            {
                if (value)
                {
                    Parent.Kind |= Entity.UriKinds.Cors;
                }
                else
                {
                    Parent.Kind &= ~Entity.UriKinds.Cors;
                }
            }
        }

        public bool Redirect
        {
            get { return (Parent.Kind & Entity.UriKinds.Redirect) == Entity.UriKinds.Redirect; }
            set
            {
                if (value)
                {
                    Parent.Kind |= Entity.UriKinds.Redirect;
                }
                else
                {
                    Parent.Kind &= ~Entity.UriKinds.Redirect;
                }
            }
        }

        public bool PostLogout
        {
            get { return (Parent.Kind & Entity.UriKinds.PostLogout) == Entity.UriKinds.PostLogout; }
            set
            {
                if (value)
                {
                    Parent.Kind |= Entity.UriKinds.PostLogout;
                }
                else
                {
                    Parent.Kind &= ~Entity.UriKinds.PostLogout;
                }
            }
        }
    }
}
