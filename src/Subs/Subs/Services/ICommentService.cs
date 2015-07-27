﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subs.ReadModel;

namespace Subs.Services
{
    public interface ICommentService
    {
        Comment GetCommentById(Guid id);

        void InsertComment(Comment comment);

        void UpdateCommentBody(Guid commentId, string body, string bodyFormatted, DateTime dateEdited);

        List<Comment> GetAllCommentsForPost(string postSlug, CommentSortBy? sortBy = null);

        void UpdateCommentVotes(Guid commentId, int? upVotes, int? downVotes);

        void DeleteComment(Guid commentId, DateTime deletedOn);
    }
}
