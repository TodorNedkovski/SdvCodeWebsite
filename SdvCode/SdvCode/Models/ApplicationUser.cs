﻿using Microsoft.AspNetCore.Identity;
using SdvCode.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdvCode.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
        }

        [MaxLength(20)]
        public string Country { get; set; }

        [MaxLength(20)]
        public string City { get; set; }

        public DateTime BirthDate { get; set; }

        public Gender Gender { get; set; }

        [MaxLength(250)]
        public string AboutMe { get; set; }

        [MaxLength(15)]
        public string FirstName { get; set; }

        [MaxLength(15)]
        public string LastName { get; set; }

        public string ImageUrl { get; set; }

        public string CoverImageUrl { get; set; }

        [NotMapped]
        public bool IsFollowed { get; set; }

        [NotMapped]
        public bool HasFollow { get; set; }

        [NotMapped]
        public ICollection<ApplicationUser> Followers { get; set; } = new HashSet<ApplicationUser>();

        [NotMapped]
        public ICollection<ApplicationUser> Followings { get; set; } = new HashSet<ApplicationUser>();
    }
}