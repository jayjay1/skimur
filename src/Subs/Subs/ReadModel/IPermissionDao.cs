﻿using System;
using Membership;

namespace Subs.ReadModel
{
    public interface IPermissionDao
    {
        bool CanUserDeleteComment(User user, Comment comment);

        bool CanUserMarkCommentAsSpam(User user, Comment comment);

        bool CanUserMarkPostAsSpam(User user, Post post);

        bool CanUserManageSubAccess(User user, Guid subId);

        bool CanUserManageSubConfig(User user, Guid subId);

        bool CanUserManageSubFlair(User user, Guid subId);

        bool CanUserManageSubMail(User user, Guid subId);

        bool CanUserManageSubPosts(User user, Guid subId);

        ModeratorPermissions? GetUserPermissionsForSub(User user, Guid subId);
    }
}
