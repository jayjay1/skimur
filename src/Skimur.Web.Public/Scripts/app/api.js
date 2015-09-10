﻿; var skimur = (function () {

    var subscribe = function (subName, callback) {
        $.ajax({
            type: "POST",
            url: "/subscribe",
            data: { subName: subName },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var unsubcribe = function (subName, callback) {
        $.ajax({
            type: "POST",
            url: "/unsubscribe",
            data: { subName: subName },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var votePost = function (postId, voteType, callback) {
        $.ajax({
            type: "POST",
            url: "/votepost",
            data: { postId: postId, type: voteType },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var unvotePost = function (postId, callback) {
        $.ajax({
            type: "POST",
            url: "/unvotepost",
            data: { postId: postId },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var voteComment = function (commentId, voteType, callback) {
        $.ajax({
            type: "POST",
            url: "/votecomment",
            data: { commentId: commentId, type: voteType },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var unvoteComment = function (commentId, callback) {
        $.ajax({
            type: "POST",
            url: "/unvotecomment",
            data: { commentId: commentId },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var createComment = function (postId, parentId, body, callback) {
        $.ajax({
            type: "POST",
            url: "/createcomment",
            data: { postId: postId, parentId: parentId, body: body },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var editComment = function (commentId, body, callback) {
        $.ajax({
            type: "POST",
            url: "/editcomment",
            data: { commentId: commentId, body: body },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var deleteComment = function (commentId, reason, callback) {
        $.ajax({
            type: "POST",
            url: "/deletecomment",
            data: { commentId: commentId, reason: reason },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    }

    var moreComments = function (postId, sort, children, depth, callback) {
        $.ajax({
            type: "POST",
            url: "/morecomments",
            data: { postId: postId, sort: sort, children: children, depth: depth },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var approvePost = function (postId, callback) {
        $.ajax({
            type: "POST",
            url: "/posts/approve",
            data: { postId: postId },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var removePost = function (postId, callback) {
        $.ajax({
            type: "POST",
            url: "/posts/remove",
            data: { postId: postId },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var replyToMessage = function (messageId, body, callback) {
        $.ajax({
            type: "POST",
            url: "/messages/reply",
            data: { replyToMessage: messageId, body : body },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    };

    var markMessagesAsRead = function(messages, callback) {
        $.ajax({
            type: "POST",
            url: "/messages/markmessagesasread",
            data: { messages: messages },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    }

    var markMessagesAsUnread = function (messages, callback) {
        $.ajax({
            type: "POST",
            url: "/messages/markmessagesasunread",
            data: { messages: messages },
            dataType: "json",
            success: function (data) {
                if (callback)
                    callback(data);
            },
            error: function () {
                if (callback)
                    callback({ success: false, error: "There was an error processing your request." });
            }
        });
    }

    return {
        subscribe: subscribe,
        unsubcribe: unsubcribe,
        votePost: votePost,
        upvotePost: function (postId, callback) { votePost(postId, 1, callback); },
        downvotePost: function (postId, callback) { votePost(postId, 0, callback); },
        unvotePost: unvotePost,
        upvoteComment: function (commentId, callback) { voteComment(commentId, 1, callback); },
        downvoteComment: function (commentId, callback) { voteComment(commentId, 0, callback); },
        unvoteComment: unvoteComment,
        createComment: createComment,
        editComment: editComment,
        deleteComment: deleteComment,
        moreComments: moreComments,
        approvePost: approvePost,
        removePost: removePost,
        replyToMessage: replyToMessage,
        markMessagesAsRead: markMessagesAsRead,
        markMessagesAsUnread: markMessagesAsUnread
    };

})();

// helper method for capturing the enter key
$.fn.enterKey = function (fnc) {
    return this.each(function () {
        $(this).keypress(function (ev) {
            var keycode = (ev.keyCode ? ev.keyCode : ev.which);
            if (keycode == '13') {
                fnc.call(this, ev);
            }
        });
    });
}