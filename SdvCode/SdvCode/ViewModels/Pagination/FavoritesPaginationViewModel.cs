﻿// Copyright (c) SDV Code Project. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SdvCode.ViewModels.Pagination
{
    using SdvCode.ViewModels.Profile;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class FavoritesPaginationViewModel
    {
        public string Username { get; set; }

        public IEnumerable<FavoritesViewModel> Favorites { get; set; } = new HashSet<FavoritesViewModel>();
    }
}