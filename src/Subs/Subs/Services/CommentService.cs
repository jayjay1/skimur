﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Data;
using ServiceStack.OrmLite;
using Subs.ReadModel;

namespace Subs.Services
{
    public class CommentService : ICommentService
    {
        private readonly IDbConnectionProvider _conn;

        public CommentService(IDbConnectionProvider conn)
        {
            _conn = conn;
        }

        public Comment GetCommentById(Guid id)
        {
            if (id == Guid.Empty)
                return null;

            return _conn.Perform(conn => conn.SingleById<Comment>(id));
        }

        public void InsertComment(Comment comment)
        {
            _conn.Perform(conn =>
            {
                conn.Insert(comment);
            });
        }

        public void UpdateCommentBody(Guid commentId, string body, string bodyFormatted, DateTime dateEdited)
        {
            _conn.Perform(conn =>
            {
                conn.Update<Comment>(new
                {
                    Body = body,
                    BodyFormatted = bodyFormatted,
                    DateEdited = dateEdited
                }, x => x.Id == commentId);
            });
        }

        public List<Comment> GetAllCommentsForPost(string postSlug, CommentSortBy? sortBy = null)
        {
            if (string.IsNullOrEmpty(postSlug))
                return new List<Comment>();

            return _conn.Perform(conn =>
            {
                var query = conn.From<Comment>().Where(x => x.PostSlug == postSlug);

                if (sortBy.HasValue)
                {
                    switch (sortBy)
                    {
                        case CommentSortBy.Best:
                            query.OrderByDescending(x => x.SortBest);
                            break;
                        case CommentSortBy.Top:
                            query.OrderByExpression = "ORDER BY (score(vote_up_count, vote_down_count), date_created) DESC";
                            break;
                        case CommentSortBy.New:
                            query.OrderByDescending(x => x.DateCreated);
                            break;
                        case CommentSortBy.Controversial:
                            query.OrderByExpression = "ORDER BY (controversy(vote_up_count, vote_down_count), date_created) DESC";
                            break;
                        case CommentSortBy.Old:
                            query.OrderBy(x => x.DateCreated);
                            break;
                        case CommentSortBy.Qa:
                            query.OrderByDescending(x => x.SortQa);
                            break;
                        default:
                            throw new Exception("unknown sort");
                    }
                }

                return conn.Select(query);
            });
        }

        public void UpdateCommentVotes(Guid commentId, int? upVotes, int? downVotes)
        {
            if (downVotes.HasValue || upVotes.HasValue)
            {
                _conn.Perform(conn =>
                {
                    if (upVotes.HasValue && downVotes.HasValue)
                    {
                        conn.Update<Comment>(new { VoteUpCount = upVotes.Value, VoteDownCount = downVotes.Value }, x => x.Id == commentId);
                    }
                    else if (upVotes.HasValue)
                    {
                        conn.Update<Comment>(new { VoteUpCount = upVotes.Value }, x => x.Id == commentId);
                    }
                    else if (downVotes.HasValue)
                    {
                        conn.Update<Comment>(new { VoteDownCount = downVotes.Value }, x => x.Id == commentId);
                    }
                });
            }
        }

        public void DeleteComment(Guid commentId, DateTime deletedOn)
        {
            _conn.Perform(conn =>
            {
                conn.Update<Comment>(new
                    {
                        Deleted = true,
                        AuthorUserName = "deleted",
                        Body = "deleted on " + deletedOn.ToLongTimeString(),
                        BodyFormatted = "deleted"
                    },
                    x => x.Id == commentId);
            });
        }
    }
}
