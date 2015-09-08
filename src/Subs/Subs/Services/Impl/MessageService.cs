﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Data;
using ServiceStack.OrmLite;
using Skimur;

namespace Subs.Services.Impl
{
    public class MessageService : IMessageService
    {
        private readonly IDbConnectionProvider _conn;

        public MessageService(IDbConnectionProvider conn)
        {
            _conn = conn;
        }

        public void InsertMessage(Message message)
        {
            _conn.Perform(conn =>
            {
                conn.Insert(message);
            });
        }

        public int GetNumberOfUnreadMessagesForUser(Guid userId)
        {
            return _conn.Perform(conn =>
            {
                return (int)conn.Count<Message>(x => x.ToUser == userId && x.IsNew);
            });
        }

        public List<Message> GetMessagesByIds(List<Guid> ids)
        {
            return _conn.Perform(conn => conn.SelectByIds<Message>(ids));
        }

        public Message GetMessageById(Guid id)
        {
            return _conn.Perform(conn => conn.SingleById<Message>(id));
        }

        public SeekedList<Guid> GetAllMessagesForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => {}, skip, take);
        }

        public SeekedList<Guid> GetUnreadMessagesForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => query.Where(x => x.IsNew), skip, take);
        }

        public SeekedList<Guid> GetPrivateMessagesForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => query.Where(x => x.Type == (int)MessageType.Private), skip, take);
        }

        public SeekedList<Guid> GetCommentRepliesForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => query.Where(x => x.Type == (int)MessageType.CommentReply), skip, take);
        }

        public SeekedList<Guid> GetPostRepliesForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => query.Where(x => x.Type == (int)MessageType.PostReply), skip, take);
        }

        public SeekedList<Guid> GetMentionsForUser(Guid userId, int? skip = null, int? take = null)
        {
            return QueryMessagesForUser(userId, query => query.Where(x => x.Type == (int)MessageType.Mention), skip, take);
        }

        public SeekedList<Guid> GetSentMessagesForUser(Guid userId, int? skip, int? take)
        {
            return _conn.Perform(conn =>
            {
                var query = conn.From<Message>().Where(x => x.AuthorId == userId);
                var totalCount = conn.Count(query);
                query.Skip(skip).Take(take);
                query.SelectExpression = "SELECT \"id\"";
                query.OrderByDescending(x => x.DateCreated);
                return new SeekedList<Guid>(conn.Select(query).Select(x => x.Id), skip ?? 0, take, totalCount);
            });
        }

        public List<Guid> GetMessagesForThread(Guid messageId)
        {
            return _conn.Perform(conn =>
            {
                var query = conn.From<Message>();
                query.Where(x => x.FirstMessage == messageId || x.Id == messageId);
                query.SelectExpression = "SELECT \"id\"";
                return conn.Select(query).Select(x => x.Id).ToList();
            });
        }

        private SeekedList<Guid> QueryMessagesForUser(Guid userId, Action<SqlExpression<Message>> query, int? skip, int? take)
        {
            return _conn.Perform(conn =>
            {
                var q = conn.From<Message>();
                query(q);
                q.Where(x => x.ToUser == userId);
                var totalCount = conn.Count(q);
                q.Skip(skip).Take(take);
                q.SelectExpression = "SELECT \"id\"";
                q.OrderByDescending(x => x.DateCreated);
                return new SeekedList<Guid>(conn.Select(q).Select(x => x.Id), skip ?? 0, take, totalCount);
            });
        } 
    }
}
