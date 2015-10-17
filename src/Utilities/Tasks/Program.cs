﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Membership.Services;
using PowerArgs;
using Skimur;
using Subs.Services;

namespace Tasks
{
    class Program
    {
        static void Main(string[] args)
        {
            SkimurContext.ContainerInitialized += Infrastructure.Cassandra.Migrations.Migrations.Run;
            SkimurContext.ContainerInitialized += Infrastructure.Postgres.Migrations.Migrations.Run;
            SkimurContext.Initialize(new Infrastructure.Registrar(),
                new Infrastructure.Settings.Registrar(),
                new Infrastructure.Caching.Registrar(),
                new Infrastructure.Email.Registrar(),
                new Infrastructure.Messaging.Registrar(),
                new Infrastructure.Messaging.RabbitMQ.Registrar(),
                new Infrastructure.Cassandra.Registrar(),
                new Infrastructure.Postgres.Registrar(),
                new Infrastructure.Logging.Registrar(),
                new Skimur.Markdown.Registrar(),
                new Subs.Registrar(),
                new Membership.Registrar());

            Args.InvokeAction<Tasks>(args);
        }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class Tasks
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Update kudos for all users")]
        public void UpdateKudos()
        {

            var membershipService = SkimurContext.Resolve<IMembershipService>();
            var karmaService = SkimurContext.Resolve<IKarmaService>();
            var postService = SkimurContext.Resolve<IPostService>();
            var commentService = SkimurContext.Resolve<ICommentService>();

            int currentIndex = 0;
            int pageSize = 10;

            var users = membershipService.GetAllUsers(currentIndex, pageSize);

            while (users.Count > 0)
            {
                foreach (var user in users)
                {
                    var postKudos = new Dictionary<Guid, int>();

                    foreach (var post in postService.GetPosts(userId: user.Id, hideRemovedPosts: false, showDeleted: true)
                        .Select(postId => postService.GetPostById(postId))
                        .Where(post => post != null))
                    {
                        if (!postKudos.ContainsKey(post.SubId))
                            postKudos.Add(post.SubId, 0);
                        postKudos[post.SubId] = postKudos[post.SubId] + (post.VoteUpCount - post.VoteDownCount);
                    }

                    var commentKudos = new Dictionary<Guid, int>();

                    foreach (var comment in commentService.GetCommentsForUser(user.Id)
                        .Select(commentId => commentService.GetCommentById(commentId))
                        .Where(comment => comment != null))
                    {
                        if (!commentKudos.ContainsKey(comment.SubId))
                            commentKudos.Add(comment.SubId, 0);
                        commentKudos[comment.SubId] = commentKudos[comment.SubId] + (comment.VoteUpCount - comment.VoteDownCount);
                    }

                    karmaService.DeleteAllKarmaForUser(user.Id);

                    if (postKudos.Count > 0)
                        foreach (var subId in postKudos.Keys)
                            karmaService.AdjustKarma(user.Id, subId, KarmaType.Post, postKudos[subId]);

                    if (commentKudos.Count > 0)
                        foreach (var subId in commentKudos.Keys)
                            karmaService.AdjustKarma(user.Id, subId, KarmaType.Comment, commentKudos[subId]);
                }

                currentIndex += users.Count;
                users = membershipService.GetAllUsers(currentIndex, pageSize);
            }


        }
    }

}