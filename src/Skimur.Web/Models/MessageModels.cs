﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subs.ReadModel;

namespace Skimur.Web.Models
{
    public class MessageThreadViewModel
    {
        public MessageThreadViewModel()
        {
            Messages = new List<MessageWrapped>();
        }

        public MessageWrapped FirstMessage { get; set; }

        public MessageWrapped ContextMessage { get; set; }

        public List<MessageWrapped> Messages { get; set; } 
    }

    public class ReplyMessageViewModel
    {
        public Guid ReplyToMessage { get; set; }
        
        public string Body { get; set; }
    }

    public class ComposeMessageViewModel
    {
        public string To { get; set; }

        public string Subject { get; set; }

        public string Message { get; set; }
    }

    public class InboxViewModel
    {
        public InboxType InboxType { get; set; }

        public PagedList<MessageWrapped> Messages { get; set; }
    }

    public enum InboxType
    {
        All,
        Unread,
        Messages,
        CommentReplies,
        PostReplies,
        Mentions,
        Sent
    }
}
