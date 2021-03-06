﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aguacongas.IdentityServer.Store.Entity
{
    /// <summary>
    /// Define a protected resources
    /// </summary>
    /// <seealso cref="IAuditable" />
    public class ProtectResource : IAuditable, ICloneable<ProtectResource>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ProtectResource"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        [MaxLength(200)]
        [Required]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [MaxLength(1000)] 
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [non editable].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [non editable]; otherwise, <c>false</c>.
        /// </value>
        public bool NonEditable { get; set; }

        /// <summary>
        /// Gets or sets the secrets.
        /// </summary>
        /// <value>
        /// The secrets.
        /// </value>
        public virtual ICollection<ApiSecret> Secrets { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        /// <value>
        /// The scopes.
        /// </value>
        public virtual ICollection<ApiScope> Scopes { get; set; }

        /// <summary>
        /// Gets or sets the user claims.
        /// </summary>
        /// <value>
        /// The user claims.
        /// </value>
        public virtual ICollection<ApiClaim> ApiClaims { get; set; }

        /// <summary>
        /// Gets or sets the API scope claims.
        /// </summary>
        /// <value>
        /// The API scope claims.
        /// </value>
        public virtual ICollection<ApiScopeClaim> ApiScopeClaims { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public virtual ICollection<ApiProperty> Properties { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        /// <value>
        /// The created at.
        /// </value>
        public DateTime CreatedAt { get ; set; }

        /// <summary>
        /// Gets or sets the modified at.
        /// </summary>
        /// <value>
        /// The modified at.
        /// </value>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public ProtectResource Clone()
        {
            var clone = MemberwiseClone() as ProtectResource;
            if (clone.Scopes != null)
            {
                var scopes = clone.Scopes;
                clone.Scopes = new List<ApiScope>(scopes.Count);
                foreach(var scope in scopes)
                {
                    clone.Scopes.Add(scope.Clone());
                }
            }

            return clone;
        }
    }
}
