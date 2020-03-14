﻿// Copyright (c) SDV Code Project. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SdvCode.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using SdvCode.Areas.Administration.Models.Enums;
    using SdvCode.Data;
    using SdvCode.Models;
    using SdvCode.Models.Enums;
    using SdvCode.ViewModels.Profile;
    using SdvCode.ViewModels.Users;

    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext db;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public ProfileService(
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        public void DeleteActivity(string currentUserId)
        {
            var trash = this.db.UserActions.Where(x => x.ApplicationUserId == currentUserId).ToList();
            this.db.UserActions.RemoveRange(trash);
            this.db.SaveChanges();
        }

        public async Task<string> DeleteActivityById(string currentUserId, int activityId)
        {
            var trash = this.db.UserActions.FirstOrDefault(x => x.ApplicationUserId == currentUserId && x.Id == activityId);
            var activityName = trash.Action.ToString();
            this.db.UserActions.Remove(trash);
            await this.db.SaveChangesAsync();
            return activityName;
        }

        public ApplicationUser ExtractUserInfo(string username, string currentUserId)
        {
            var user = this.db.Users.FirstOrDefault(u => u.UserName == username);
            user.IsFollowed = this.db.FollowUnfollows.Any(x => x.FollowerId == currentUserId && x.PersonId == user.Id && x.IsFollowed == true);
            user.ActionsCount = this.db.UserActions.Count(x => x.ApplicationUserId == user.Id);
            return user;
        }

        public async Task<ApplicationUser> FollowUser(string username, string currentUserId)
        {
            var user = this.db.Users.FirstOrDefault(u => u.UserName == username);
            var currentUser = this.db.Users.FirstOrDefault(u => u.Id == currentUserId);

            if (!this.db.FollowUnfollows.Any(x => x.PersonId == user.Id && x.FollowerId == currentUser.Id))
            {
                this.db.FollowUnfollows.Add(new FollowUnfollow
                {
                    PersonId = user.Id,
                    FollowerId = currentUser.Id,
                    IsFollowed = true,
                });
            }
            else
            {
                this.db.FollowUnfollows.FirstOrDefault(x => x.PersonId == user.Id && x.FollowerId == currentUser.Id).IsFollowed = true;
            }

            await this.db.SaveChangesAsync();

            var targetFollowerEntity = this.db.UserActions.FirstOrDefault(x =>
            x.Action == UserActionsType.Follow &&
            x.FollowerUsername == currentUser.UserName &&
            x.PersonUsername == username &&
            x.ApplicationUserId == currentUser.Id);

            if (targetFollowerEntity == null)
            {
                currentUser.UserActions.Add(new UserAction
                {
                    Action = UserActionsType.Follow,
                    ActionDate = DateTime.UtcNow,
                    PersonUsername = username,
                    PersonProfileImageUrl = user.ImageUrl,
                    FollowerUsername = currentUser.UserName,
                    FollowerProfileImageUrl = currentUser.ImageUrl,
                    ApplicationUserId = currentUser.Id,
                });

                await this.db.SaveChangesAsync();
            }
            else
            {
                targetFollowerEntity.ActionDate = DateTime.UtcNow;
                this.db.UserActions.Update(targetFollowerEntity);
                await this.db.SaveChangesAsync();
            }

            var targetPersonEntity = this.db.UserActions.FirstOrDefault(x => x.Action == UserActionsType.Followed &&
            x.FollowerUsername == currentUser.UserName &&
            x.PersonUsername == user.UserName &&
            x.ApplicationUserId == user.Id);

            if (targetPersonEntity == null)
            {
                user.UserActions.Add(new UserAction
                {
                    Action = UserActionsType.Followed,
                    ActionDate = DateTime.UtcNow,
                    FollowerUsername = currentUser.UserName,
                    FollowerProfileImageUrl = currentUser.ImageUrl,
                    PersonUsername = user.UserName,
                    PersonProfileImageUrl = user.ImageUrl,
                    ApplicationUserId = user.Id,
                });

                await this.db.SaveChangesAsync();
            }
            else
            {
                targetPersonEntity.ActionDate = DateTime.UtcNow;
                this.db.UserActions.Update(targetPersonEntity);
                await this.db.SaveChangesAsync();
            }

            return currentUser;
        }

        public AllUsersViewModel GetAllUsers(string currentUserId)
        {
            var users = new AllUsersViewModel();

            foreach (var user in this.db.Users)
            {
                users.UsersCards.Add(new UserCardViewModel
                {
                    UserId = user.Id,
                    Username = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ImageUrl = user.ImageUrl,
                    CoverImageUrl = user.CoverImageUrl,
                });
            }

            foreach (var user in users.UsersCards)
            {
                user.FollowingsCount = this.db.FollowUnfollows
                    .Count(x => x.FollowerId == user.UserId && x.IsFollowed == true);

                user.FollowersCount = this.db.FollowUnfollows
                    .Count(x => x.PersonId == user.UserId && x.IsFollowed == true);

                user.HasFollowed = this.db.FollowUnfollows
                    .Any(x => x.FollowerId == currentUserId && x.PersonId == user.UserId && x.IsFollowed == true);

                user.Activities = this.db.UserActions
                    .Count(x => x.ApplicationUserId == user.UserId);
            }

            return users;
        }

        public async Task<ApplicationUser> UnfollowUser(string username, string currentUserId)
        {
            var user = this.db.Users.FirstOrDefault(u => u.UserName == username);
            var currentUser = this.db.Users.FirstOrDefault(u => u.Id == currentUserId);

            if (this.db.FollowUnfollows.Any(x => x.PersonId == user.Id && x.FollowerId == currentUser.Id && x.IsFollowed == true))
            {
                this.db.FollowUnfollows
                    .FirstOrDefault(x => x.PersonId == user.Id && x.FollowerId == currentUser.Id && x.IsFollowed == true)
                    .IsFollowed = false;

                await this.db.SaveChangesAsync();

                var targetUnfollowerEntity = this.db.UserActions.FirstOrDefault(x =>
                x.Action == UserActionsType.Unfollow &&
                x.FollowerUsername == currentUser.UserName &&
                x.PersonUsername == username &&
                x.ApplicationUserId == currentUser.Id);

                if (targetUnfollowerEntity == null)
                {
                    currentUser.UserActions.Add(new UserAction
                    {
                        Action = UserActionsType.Unfollow,
                        ActionDate = DateTime.UtcNow,
                        PersonUsername = username,
                        PersonProfileImageUrl = user.ImageUrl,
                        FollowerUsername = currentUser.UserName,
                        FollowerProfileImageUrl = currentUser.ImageUrl,
                        ApplicationUserId = currentUser.Id,
                    });

                    await this.db.SaveChangesAsync();
                }
                else
                {
                    targetUnfollowerEntity.ActionDate = DateTime.UtcNow;
                    this.db.UserActions.Update(targetUnfollowerEntity);
                    await this.db.SaveChangesAsync();
                }

                var targetPersonEntity = this.db.UserActions.FirstOrDefault(x =>
                x.Action == UserActionsType.Unfollowed &&
                x.FollowerUsername == currentUser.UserName &&
                x.PersonUsername == user.UserName &&
                x.ApplicationUserId == user.Id);

                if (targetPersonEntity == null)
                {
                    user.UserActions.Add(new UserAction
                    {
                        Action = UserActionsType.Unfollowed,
                        ActionDate = DateTime.UtcNow,
                        FollowerUsername = currentUser.UserName,
                        FollowerProfileImageUrl = currentUser.ImageUrl,
                        PersonUsername = user.UserName,
                        PersonProfileImageUrl = user.ImageUrl,
                        ApplicationUserId = user.Id,
                    });

                    await this.db.SaveChangesAsync();
                }
                else
                {
                    targetPersonEntity.ActionDate = DateTime.UtcNow;
                    this.db.UserActions.Update(targetPersonEntity);
                    await this.db.SaveChangesAsync();
                }
            }

            return currentUser;
        }

        public async Task<bool> HasAdmin()
        {
            var role = await this.roleManager.FindByNameAsync(Roles.Administrator.ToString());

            if (role != null)
            {
                var roleId = role.Id;
                return this.db.UserRoles.Any(x => x.RoleId == roleId);
            }

            return false;
        }

        public void MakeYourselfAdmin(string username)
        {
            ApplicationUser user = this.db.Users.FirstOrDefault(x => x.UserName == username);
            IdentityRole role = this.db.Roles.FirstOrDefault(x => x.Name == Roles.Administrator.ToString());

            if (user == null || role == null)
            {
                return;
            }

            this.db.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = role.Id,
                UserId = user.Id,
            });

            this.db.SaveChanges();
        }

        public async Task<List<ActivitiesViewModel>> ExtractActivities(string username)
        {
            List<ActivitiesViewModel> allActivities = new List<ActivitiesViewModel>();
            var user = await this.userManager.FindByNameAsync(username);
            var activities = this.db.UserActions.Where(x => x.ApplicationUserId == user.Id).ToList();

            foreach (var item in activities)
            {
                allActivities.Add(new ActivitiesViewModel
                {
                    Id = item.Id,
                    Action = item.Action,
                    ActionDate = item.ActionDate,
                    ApplicationUser = user,
                    ApplicationUserId = user.Id,
                    CoverImageUrl = item.CoverImageUrl,
                    FollowerProfileImageUrl = item.FollowerProfileImageUrl,
                    FollowerUsername = item.FollowerUsername,
                    PersonProfileImageUrl = item.PersonProfileImageUrl,
                    PersonUsername = item.PersonUsername,
                    ProfileImageUrl = item.ProfileImageUrl,
                });
            }

            return allActivities.OrderByDescending(x => x.ActionDate).ToList();
        }

        public async Task<List<FollowersViewModel>> ExtractFollowers(ApplicationUser user, string currentUserId)
        {
            List<FollowersViewModel> allFollowers = new List<FollowersViewModel>();
            var followers = this.db.FollowUnfollows.Where(x => x.PersonId == user.Id && x.IsFollowed == true).ToList();

            foreach (var item in followers)
            {
                var follower = await this.userManager.FindByIdAsync(item.FollowerId);
                var hasFollow = this.db.FollowUnfollows
                    .Any(x => x.FollowerId == currentUserId &&
                    x.PersonId == follower.Id && x.IsFollowed == true);

                allFollowers.Add(new FollowersViewModel
                {
                    Username = follower.UserName,
                    FirstName = follower.FirstName,
                    LastName = follower.LastName,
                    ImageUrl = follower.ImageUrl,
                    IsBlocked = follower.IsBlocked,
                    HasFollow = hasFollow,
                });
            }

            return allFollowers;
        }

        public async Task<List<FollowingViewModel>> ExtractFollowing(ApplicationUser user, string currentUserId)
        {
            List<FollowingViewModel> allFollowing = new List<FollowingViewModel>();
            var followers = this.db.FollowUnfollows.Where(x => x.FollowerId == user.Id && x.IsFollowed == true).ToList();

            foreach (var item in followers)
            {
                var follower = await this.userManager.FindByIdAsync(item.PersonId);
                var hasFollow = this.db.FollowUnfollows
                    .Any(x => x.FollowerId == currentUserId &&
                    x.PersonId == follower.Id && x.IsFollowed == true);

                allFollowing.Add(new FollowingViewModel
                {
                    Username = follower.UserName,
                    FirstName = follower.FirstName,
                    LastName = follower.LastName,
                    ImageUrl = follower.ImageUrl,
                    IsBlocked = follower.IsBlocked,
                    HasFollow = hasFollow,
                });
            }

            return allFollowing;
        }
    }
}