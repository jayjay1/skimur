﻿using System;
using Skimur.Messaging;

namespace Skimur.App.Commands
{
    public class EditComment : ICommandReturns<EditCommentResponse>
    {
        public Guid EditedBy { get; set; }

        public DateTime DateEdited { get; set; }

        public Guid CommentId { get; set; }

        public string Body { get; set; }
    }

    public class EditCommentResponse
    {
        public string Error { get; set; }

        public string Body { get; set; }

        public string FormattedBody { get; set; }
    }
}
