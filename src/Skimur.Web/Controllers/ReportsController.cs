﻿using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Skimur.Web.Models;
using Subs.Commands;
using Subs.ReadModel;

namespace Skimur.Web.Controllers
{
    public class ReportsController : BaseController
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly ICommandBus _commandBus;
        private readonly IUserContext _userContext;
        private readonly ISubDao _subDao;
        private readonly ISubWrapper _subWrapper;
        private readonly IPermissionDao _permissionDao;
        private readonly IPostDao _postDao;
        private readonly IPostWrapper _postWrapper;
        private readonly ICommentDao _commentDao;
        private readonly ICommentWrapper _commentWrapper;

        public ReportsController(ILogger<ReportsController> logger,
            ICommandBus commandBus,
            IUserContext userContext,
            ISubDao subDao,
            ISubWrapper subWrapper,
            IPermissionDao permissionDao,
            IPostDao postDao,
            IPostWrapper postWrapper,
            ICommentDao commentDao,
            ICommentWrapper commentWrapper)
        {
            _logger = logger;
            _commandBus = commandBus;
            _userContext = userContext;
            _subDao = subDao;
            _subWrapper = subWrapper;
            _permissionDao = permissionDao;
            _postDao = postDao;
            _postWrapper = postWrapper;
            _commentDao = commentDao;
            _commentWrapper = commentWrapper;
        }

        public ActionResult ReportComment(Guid commentId, ReasonType type, string reason)
        {
            if (!Request.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    error = "You must be logged in to report."
                });
            }

            try
            {
                reason = BuildReasonFromType(type, reason);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }

            try
            {
                _commandBus.Send(new ReportComment
                {
                    ReportBy = _userContext.CurrentUser.Id,
                    CommentId = commentId,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error reporting comment.", ex);
                return Json(new
                {
                    success = false,
                    error = "An unknown error occured."
                });
            }

            return Json(new
            {
                success = true,
                error = (string)null
            });
        }

        public ActionResult ReportPost(Guid postId, ReasonType type, string reason)
        {
            if (!Request.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    error = "You must be logged in to report."
                });
            }

            try
            {
                reason = BuildReasonFromType(type, reason);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }

            try
            {
                _commandBus.Send(new ReportPost
                {
                    ReportBy = _userContext.CurrentUser.Id,
                    PostId = postId,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error reporting comment.", ex);
                return Json(new
                {
                    success = false,
                    error = "An unknown error occured."
                });
            }

            return Json(new
            {
                success = true,
                error = (string)null
            });
        }

        public ActionResult Clear(Guid? postId, Guid? commentId)
        {
            if (!Request.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    error = "You must be logged in to clear reports."
                });
            }
            
            try
            {
                _commandBus.Send(new ClearReports
                {
                    UserId = _userContext.CurrentUser.Id,
                    PostId = postId,
                    CommentId = commentId
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error clearing reports.", ex);
                return Json(new
                {
                    success = false,
                    error = "An unknown error occured."
                });
            }

            return Json(new
            {
                success = true,
                error = (string)null
            });
        }

        public ActionResult Ignore(Guid? postId, Guid? commentId)
        {
            if (!Request.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    error = "You must be logged in to ignore reports."
                });
            }

            try
            {
                _commandBus.Send(new ConfigureReportIgnoring
                {
                    UserId = _userContext.CurrentUser.Id,
                    PostId = postId,
                    CommentId = commentId,
                    IgnoreReports = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error ignoring reports.", ex);
                return Json(new
                {
                    success = false,
                    error = "An unknown error occured."
                });
            }

            return Json(new
            {
                success = true,
                error = (string)null
            });
        }

        public ActionResult Unignore(Guid? postId, Guid? commentId)
        {
            if (!Request.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    error = "You must be logged in to unignore reports."
                });
            }

            try
            {
                _commandBus.Send(new ConfigureReportIgnoring
                {
                    UserId = _userContext.CurrentUser.Id,
                    PostId = postId,
                    CommentId = commentId,
                    IgnoreReports = false
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error unignoring reports.", ex);
                return Json(new
                {
                    success = false,
                    error = "An unknown error occured."
                });
            }

            return Json(new
            {
                success = true,
                error = (string)null
            });
        }

        [Authorize]
        public ActionResult ReportedPosts(string subName)
        {
            ViewBag.ManageNavigationKey = "reportedposts";

            if (string.IsNullOrEmpty(subName))
                throw new NotFoundException();

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new NotFoundException();

            if (!_permissionDao.CanUserManageSubPosts(_userContext.CurrentUser, sub.Id))
                throw new UnauthorizedException();

            var postIds = _postDao.GetReportedPosts(new List<Guid> { sub.Id }, take: 30);

            var model = new ReportedPostsViewModel();
            model.Sub = sub;
            model.Posts = new PagedList<PostWrapped>(_postWrapper.Wrap(postIds, _userContext.CurrentUser), 0, 30, postIds.HasMore);

            return View(model);
        }

        [Authorize]
        public ActionResult ReportedComments(string subName)
        {
            ViewBag.ManageNavigationKey = "reportedcomments";

            if (string.IsNullOrEmpty(subName))
                throw new NotFoundException();

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new NotFoundException();

            if (!_permissionDao.CanUserManageSubPosts(_userContext.CurrentUser, sub.Id))
                throw new UnauthorizedException();

            var commentIds = _commentDao.GetReportedComments(new List<Guid> { sub.Id }, take: 30);

            var model = new ReportedCommentsViewModel();
            model.Sub = sub;
            model.Comments = new PagedList<CommentWrapped>(_commentWrapper.Wrap(commentIds, _userContext.CurrentUser), 0, 30, commentIds.HasMore);

            return View(model);
        }

        private string BuildReasonFromType(ReasonType type, string reason)
        {
            if (string.IsNullOrEmpty(reason))
                reason = string.Empty;

            switch (type)
            {
                case ReasonType.Spam:
                    reason = "Spam";
                    break;
                case ReasonType.VoteManipulation:
                    reason = "Vote manipulation";
                    break;
                case ReasonType.PersonalInformation:
                    reason = "Personal information";
                    break;
                case ReasonType.SexualizingMinors:
                    reason = "Sexualizing minors";
                    break;
                case ReasonType.BreakingSkimur:
                    reason = "Breaking skimur";
                    break;
                case ReasonType.Other:
                    if (string.IsNullOrEmpty(reason))
                        throw new Exception("You must provide a reason.");
                    if (reason.Length > 200)
                        throw new Exception("The reason must not be greater than 200 characters.");
                    break;
                default:
                    throw new Exception("unknown type");
            }

            return reason;
        }

        public enum ReasonType
        {
            Spam = 0,
            VoteManipulation = 1,
            PersonalInformation = 2,
            SexualizingMinors = 3,
            BreakingSkimur = 4,
            Other = 5
        }
    }
}