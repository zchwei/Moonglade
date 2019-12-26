﻿using System;
using System.Threading.Tasks;
using Edi.Blog.Pingback.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Route("post")]
    public partial class PostController : MoongladeController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;

        public PostController(
            ILogger<PostController> logger,
            IOptions<AppSettings> settings,
            PostService postService,
            CategoryService categoryService,
            IBlogConfig blogConfig)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
        }

        [Route(""), Route("/")]
        public async Task<IActionResult> Index(int page = 1, [FromServices] IMemoryCache memoryCache = null)
        {
            try
            {
                var pagesize = _blogConfig.ContentSettings.PostListPageSize;
                var postList = await _postService.GetPagedPostsAsync(pagesize, page);
                var postCount = memoryCache.GetOrCreate(StaticCacheKeys.PostCount, entry => _postService.CountVisiblePosts().Item);

                var postsAsIPagedList = new StaticPagedList<PostListItem>(postList, page, pagesize, postCount);
                return View(postsAsIPagedList);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting post list.");
                return ServerError("Error getting post list.");
            }
        }

        [Route("{year:int:min(1975):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug}")]
        [AddPingbackHeader("pingback")]
        public async Task<IActionResult> Slug(int year, int month, int day, string slug)
        {
            ViewBag.ErrorMessage = string.Empty;

            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug))
            {
                Logger.LogWarning($"Invalid parameter year: {year}, slug: {slug}");
                return NotFound();
            }

            var rsp = await _postService.GetPostAsync(year, month, day, slug);
            if (!rsp.IsSuccess) return ServerError(rsp.Message);

            var post = rsp.Item;
            if (post == null)
            {
                Logger.LogWarning($"Post not found, parameter '{year}/{month}/{day}/{slug}'.");
                return NotFound();
            }

            var viewModel = new PostSlugViewModelWrapper(post);

            ViewBag.TitlePrefix = $"{post.Title}";
            return View(viewModel);
        }

        [Authorize]
        [Route("preview/{postId}")]
        public async Task<IActionResult> DraftPreview(Guid postId)
        {
            var rsp = await _postService.GetDraftPreviewAsync(postId);
            if (!rsp.IsSuccess) return ServerError(rsp.Message);

            var post = rsp.Item;
            if (post == null)
            {
                Logger.LogWarning($"Post not found, parameter '{postId}'.");
                return NotFound();
            }

            var viewModel = new PostSlugViewModelWrapper(post);

            ViewBag.TitlePrefix = $"{post.Title}";
            ViewBag.IsDraftPreview = true;
            return View("Slug", viewModel);
        }

        [HttpPost("hit")]
        public async Task<IActionResult> Hit([FromForm] Guid postId)
        {
            if (HasCookie(CookieNames.Hit, postId.ToString()))
            {
                return new EmptyResult();
            }

            var response = await _postService.UpdatePostStatisticAsync(postId, StatisticTypes.Hits);
            if (response.IsSuccess)
            {
                SetPostTrackingCookie(CookieNames.Hit, postId.ToString());
            }

            return Json(response);
        }

        [HttpPost("like")]
        public async Task<IActionResult> Like([FromForm] Guid postId)
        {
            if (HasCookie(CookieNames.Liked, postId.ToString()))
            {
                return Json(new
                {
                    IsSuccess = false,
                    Message = "You Have Rated"
                });
            }

            var response = await _postService.UpdatePostStatisticAsync(postId, StatisticTypes.Likes);
            if (response.IsSuccess)
            {
                SetPostTrackingCookie(CookieNames.Liked, postId.ToString());
            }

            return Json(response);
        }

        #region Helper Methods

        private bool HasCookie(CookieNames cookieName, string id)
        {
            var viewCookie = HttpContext.Request.Cookies[cookieName.ToString()];
            if (viewCookie != null)
            {
                return viewCookie == id;
            }
            return false;
        }

        private void SetPostTrackingCookie(CookieNames cookieName, string id)
        {
            var options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(1),
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,

                // Mark as essential to pass GDPR
                // https://docs.microsoft.com/en-us/aspnet/core/security/gdpr?view=aspnetcore-2.1
                IsEssential = true
            };

            Response.Cookies.Append(cookieName.ToString(), id, options);
        }

        #endregion
    }
}
