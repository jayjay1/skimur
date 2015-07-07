﻿using System;
using ServiceStack.DataAnnotations;

namespace Infrastructure.Membership
{
    [Alias("UserRoles")]
    public class UserRole
    {
        /// <summary>
        /// UserId for the user that is in the role
        /// 
        /// </summary>
        public virtual Guid UserId { get; set; }

        /// <summary>
        /// RoleId for the role
        /// </summary>
        public virtual Guid RoleId { get; set; }
    }
}
