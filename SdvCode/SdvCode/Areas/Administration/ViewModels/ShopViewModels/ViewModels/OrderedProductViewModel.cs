﻿// Copyright (c) SDV Code Project. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SdvCode.Areas.Administration.ViewModels.ShopViewModels.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OrderedProductViewModel
    {
        public string Name { get; set; }

        public decimal Price { get; set; }

        public int WantedQuantity { get; set; }

        public int AvailableQuantity { get; set; }

        public decimal TotalProductPrice { get; set; }
    }
}