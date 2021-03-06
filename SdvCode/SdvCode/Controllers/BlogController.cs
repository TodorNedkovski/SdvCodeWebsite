﻿// Copyright (c) SDV Code Project. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SdvCode.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CloudinaryDotNet.Actions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using SdvCode.Areas.Administration.Models.Enums;
    using SdvCode.Constraints;
    using SdvCode.Data;
    using SdvCode.Models.User;
    using SdvCode.Services.Blog;
    using SdvCode.Services.Post;
    using SdvCode.ViewModels.Blog.InputModels;
    using SdvCode.ViewModels.Blog.ViewModels;
    using SdvCode.ViewModels.Post.InputModels;
    using Twilio.Rest.Api.V2010.Account.Usage;
    using X.PagedList;

    public class BlogController : Controller
    {
        private readonly IBlogService blogService;
        private readonly UserManager<ApplicationUser> userManager;

        public BlogController(
            IBlogService blogService,
            UserManager<ApplicationUser> userManager)
        {
            this.blogService = blogService;
            this.userManager = userManager;
        }

        [Route("Blog/{page?}/{search?}")]
        public async Task<IActionResult> Index(int? page, string search)
        {
            var currentUser = await this.userManager.GetUserAsync(this.User);
            var pageNumber = page ?? 1;

            if (search != null)
            {
                pageNumber = 1;
            }

            var posts = await this.blogService.ExtraxtAllPosts(currentUser, search);
            var model = new BlogViewModel
            {
                Posts = posts.ToPagedList(pageNumber, GlobalConstants.BlogPostsOnPage),
                Search = search,
            };

            return this.View(model);
        }

        [Authorize]
        public async Task<IActionResult> CreatePost()
        {
            var currentUser = await this.userManager.GetUserAsync(this.User);
            var isBlocked = this.blogService.IsBlocked(currentUser);
            if (isBlocked)
            {
                this.TempData["Error"] = ErrorMessages.YouAreBlock;
                return this.RedirectToAction("Index", "Blog");
            }

            var isInRole = await this.blogService.IsInBlogRole(currentUser);
            if (!isInRole)
            {
                this.TempData["Error"] = string.Format(ErrorMessages.NotInBlogRoles, Roles.Contributor);
                return this.RedirectToAction("Index", "Blog");
            }

            var model = new CreatePostIndexModel
            {
                Categories = await this.blogService.ExtractAllCategoryNames(),
                Tags = await this.blogService.ExtractAllTagNames(),
                PostInputModel = new CreatePostInputModel(),
            };

            return this.View(model);
        }

        [Authorize]
        public async Task<IActionResult> DeletePost(string id)
        {
            var currentUser = await this.userManager.GetUserAsync(this.User);
            var isBlocked = this.blogService.IsBlocked(currentUser);
            if (isBlocked)
            {
                this.TempData["Error"] = ErrorMessages.YouAreBlock;
                return this.RedirectToAction("Index", "Blog");
            }

            var isInRole = await this.blogService.IsInPostRole(currentUser, id);
            if (isInRole == false)
            {
                this.TempData["Error"] = ErrorMessages.NotInDeletePostRoles;
                return this.RedirectToAction("Index", "Blog");
            }

            var tuple = await this.blogService.DeletePost(id, currentUser);
            this.TempData[tuple.Item1] = tuple.Item2;
            return this.RedirectToAction("Index", "Blog");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost(CreatePostIndexModel model)
        {
            if (this.ModelState.IsValid)
            {
                var currentUser = await this.userManager.GetUserAsync(this.User);
                if (this.ModelState.IsValid)
                {
                    var tuple = await this.blogService.CreatePost(model, currentUser);
                    this.TempData[tuple.Item1] = tuple.Item2;
                }
                else
                {
                    this.TempData["Error"] = ErrorMessages.InvalidInputModel;
                }

                return this.RedirectToAction("Index", "Blog");
            }

            this.TempData["Error"] = ErrorMessages.InvalidInputModel;
            return this.RedirectToAction("Index", "Blog");
        }

        [Authorize]
        public async Task<IActionResult> EditPost(string id)
        {
            if (!await this.blogService.IsPostExist(id))
            {
                return this.NotFound();
            }

            var currentUser = await this.userManager.GetUserAsync(this.User);
            var isBlocked = this.blogService.IsBlocked(currentUser);
            if (isBlocked)
            {
                this.TempData["Error"] = ErrorMessages.YouAreBlock;
                return this.RedirectToAction("Index", "Blog");
            }

            var isApproved = await this.blogService.IsPostBlocked(id, currentUser);
            if (isApproved)
            {
                this.TempData["Error"] = ErrorMessages.CannotEditBlogPost;
                return this.RedirectToAction("Index", "Blog");
            }

            var isInRole = await this.blogService.IsInPostRole(currentUser, id);
            if (!isInRole)
            {
                this.TempData["Error"] = ErrorMessages.NotInEditPostRoles;
                return this.RedirectToAction("Index", "Blog");
            }

            EditPostInputModel model = await this.blogService.ExtractPost(id, currentUser);
            model.Categories = await this.blogService.ExtractAllCategoryNames();
            model.Tags = await this.blogService.ExtractAllTagNames();

            return this.View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditPost(EditPostInputModel model)
        {
            if (this.ModelState.IsValid)
            {
                var currentUser = await this.userManager.GetUserAsync(this.User);
                var isBlocked = this.blogService.IsBlocked(currentUser);
                if (isBlocked)
                {
                    this.TempData["Error"] = ErrorMessages.YouAreBlock;
                    return this.RedirectToAction("Index", "Blog");
                }

                var tuple = await this.blogService.EditPost(model, currentUser);
                this.TempData[tuple.Item1] = tuple.Item2;
                return this.RedirectToAction("Index", "Post", new { model.Id });
            }

            this.TempData["Error"] = ErrorMessages.InvalidInputModel;
            return this.RedirectToAction("Index", "Blog");
        }
    }
}