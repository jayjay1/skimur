﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Membership.Services;
using Skimur.Markdown;
using Subs.Commands;
using Subs.Events;
using Subs.Services;

namespace Subs.Worker.Commands
{
    public class CommentCommandHandler :
        ICommandHandlerResponse<CreateComment, CreateCommentResponse>,
        ICommandHandlerResponse<EditComment, EditCommentResponse>,
        ICommandHandlerResponse<DeleteComment, DeleteCommentResponse>
    {
        private readonly IPostService _postService;
        private readonly IMembershipService _membershipService;
        private readonly ICommentService _commentService;
        private readonly IMarkdownCompiler _markdownCompiler;
        private readonly ICommandBus _commandBus;
        private readonly IPermissionService _permissionService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<CommentCommandHandler> _logger;

        public CommentCommandHandler(IPostService postService,
            IMembershipService membershipService,
            ICommentService commentService,
            IMarkdownCompiler markdownCompiler,
            ICommandBus commandBus,
            IPermissionService permissionService,
            IEventBus eventBus,
            ILogger<CommentCommandHandler> logger)
        {
            _postService = postService;
            _membershipService = membershipService;
            _commentService = commentService;
            _markdownCompiler = markdownCompiler;
            _commandBus = commandBus;
            _permissionService = permissionService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public CreateCommentResponse Handle(CreateComment command)
        {
            var response = new CreateCommentResponse();

            try
            {
                if (string.IsNullOrEmpty(command.Body))
                {
                    response.Error = "A comment is required.";
                    return response;
                }

                command.Body = command.Body.Trim();

                var post = _postService.GetPostById(command.PostId);

                if (post == null)
                {
                    response.Error = "Invalid post.";
                    return response;
                }

                var user = _membershipService.GetUserByUserName(command.AuthorUserName);

                if (user == null)
                {
                    response.Error = "Invalid user.";
                    return response;
                }

                Comment parentComment = null;

                if (command.ParentId.HasValue)
                {
                    // this is a reply to a comment.
                    parentComment = _commentService.GetCommentById(command.ParentId.Value);

                    if (parentComment.PostId != post.Id)
                    {
                        // NOTE: this shouldn't happen, and we may want to log it in the future.
                        response.Error = "Replying to a comment in a different post.";
                        return response;
                    }
                }

                List<string> mentions;
                var compiledBody = _markdownCompiler.Compile(command.Body, out mentions);
                
                var comment = new Comment
                {
                    Id = GuidUtil.NewSequentialId(),
                    DateCreated = command.DateCreated,
                    SubId = post.SubId,
                    ParentId = parentComment != null ? parentComment.Id : (Guid?)null,
                    Parents = new Guid[0],
                    AuthorUserId = user.Id,
                    AuthorIpAddress = command.AuthorIpAddress,
                    PostId = post.Id,
                    Body = command.Body,
                    BodyFormatted = compiledBody,
                    SendReplies = command.SendReplies,
                    VoteUpCount = 1
                };

                _commentService.InsertComment(comment);

                _commandBus.Send(new CastVoteForComment { DateCasted = post.DateCreated, IpAddress = command.AuthorIpAddress, CommentId = comment.Id, UserName = user.UserName, VoteType = VoteType.Up });
                _eventBus.Publish(new CommentCreated { CommentId = comment.Id, PostId = comment.PostId });
                if (mentions.Count > 0)
                    _eventBus.Publish(new UsersMentioned { CommentId = comment.Id, Users = mentions });

                _postService.UpdateNumberOfCommentsForPost(post.Id, _commentService.GetNumberOfCommentsForPost(post.Id));

                response.CommentId = comment.Id;
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            return response;
        }

        public EditCommentResponse Handle(EditComment command)
        {
            Debug.WriteLine(DateTime.Now.ToLongTimeString() + ":Got command..");

            var response = new EditCommentResponse();

            try
            {
                if (string.IsNullOrEmpty(command.Body))
                {
                    response.Error = "A comment is required.";
                    return response;
                }

                command.Body = command.Body.Trim();

                var comment = _commentService.GetCommentById(command.CommentId);

                if (comment == null)
                {
                    response.Error = "Invalid comment.";
                    return response;
                }

                List<string> oldMentions = null;
                List<string> newMentions = null;

                try
                {
                    _markdownCompiler.Compile(comment.Body, out oldMentions);
                }
                catch (Exception ex)
                {
                    _logger.Error("There was an errror compiling previous mentions for a comment edit.", ex);
                }

                var bodyFormatted = _markdownCompiler.Compile(command.Body, out newMentions);
                _commentService.UpdateCommentBody(comment.Id, command.Body, bodyFormatted, command.DateEdited);

                if (oldMentions != null && oldMentions.Count > 0)
                {
                    // we have some mentions in our previous comment. let's see if they were removed
                    var removed = oldMentions.Except(newMentions).ToList();
                    if (removed.Count > 0)
                    {
                        _eventBus.Publish(new UsersUnmentioned
                        {
                            CommentId = comment.Id,
                            Users = removed
                        });
                    }
                }

                if (newMentions != null)
                {
                    // the are some mentions in this comment.
                    // let's get only the new mentions that were previously in the comment
                    if (oldMentions != null && oldMentions.Count > 0)
                    {
                        newMentions = newMentions.Except(oldMentions).ToList();
                    }

                    if (newMentions.Count > 0)
                    {
                        _eventBus.Publish(new UsersMentioned
                        {
                            CommentId = comment.Id,
                            Users = newMentions
                        });
                    }
                }

                response.Body = command.Body;
                response.FormattedBody = bodyFormatted;
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            Debug.WriteLine(DateTime.Now.ToLongTimeString() + ":Finished command..");

            return response;
        }

        public DeleteCommentResponse Handle(DeleteComment command)
        {
            var response = new DeleteCommentResponse();

            try
            {
                var comment = _commentService.GetCommentById(command.CommentId);

                if (comment == null)
                {
                    response.Error = "Invalid comment.";
                    return response;
                }

                var user = _membershipService.GetUserByUserName(command.UserName);

                if (user == null)
                {
                    response.Error = "Invalid user.";
                    return response;
                }

                if (!_permissionService.CanUserDeleteComment(user, comment))
                {
                    response.Error = "You are not allowed to delete this comment.";
                    return response;
                }

                _commentService.DeleteComment(comment.Id, command.DateDeleted);

                _eventBus.Publish(new CommentDeleted
                {
                    CommentId = comment.Id,
                    PostId = comment.PostId,
                    SubId = comment.SubId,
                    DeletedByUserId = user.Id
                });
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            return response;
        }
    }
}