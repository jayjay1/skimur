﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Membership;

namespace Subs.ReadModel
{
    public class MessageWrapped
    {
        public MessageWrapped(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }
        
        public User Author { get; set; }

        public Sub FromSub { get; set; }

        public User ToUser { get; set; }

        public Sub ToSub { get; set; }
        
        public bool CanReply { get; set; }

        public bool UserIsRecipiant { get; set; }

        public bool UserIsSender { get; set; }

        public bool CanUserView { get; set; }

        public bool CanMarkRead { get; set; }
        public bool? IsUnread { get; set; }
    }
}
