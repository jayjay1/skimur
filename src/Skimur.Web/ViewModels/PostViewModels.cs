﻿using Subs;
using Subs.ReadModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Skimur.Web.ViewModels
{
    public class CreatePostModel
    {
        public string Title { get; set; }

        public string Url { get; set; }

        // TODO: [AllowHtml]
        public string Content { get; set; }

        public PostType PostType { get; set; }

        [DisplayName("Sub name")]
        public string PostToSub { get; set; }

        [DisplayName("Notify replies")]
        public bool NotifyReplies { get; set; }

        public SubWrapped Sub { get; set; }
    }

    public class EditPostModel
    {
        public Guid PostId { get; set; }

        // TODO:[AllowHtml]
        public string Content { get; set; }
    }

    public class PostDetailsModel
    {
        public PostWrapped Post { get; set; }

        public SubWrapped Sub { get; set; }

        public CommentListModel Comments { get; set; }

        public bool ViewingSpecificComment { get; set; }
    }
}