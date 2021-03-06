﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Xamarin.Forms.Auth
{
    internal static class UriBuilderExtensions
    {
        public static void AppendQueryParameters(this UriBuilder builder, string queryParams)
        {
            if (builder == null || string.IsNullOrEmpty(queryParams))
            {
                return;
            }

            if (builder.Query.Length > 1)
            {
                builder.Query = builder.Query.Substring(1) + "&" + queryParams;
            }
            else
            {
                builder.Query = queryParams;
            }
        }

        public static void AppendQueryParameters(this UriBuilder builder, IDictionary<string, string> queryParams)
        {
            var list = new List<string>();
            foreach (var kvp in queryParams)
            {
                list.Add($"{kvp.Key}={kvp.Value}");
            }

            AppendQueryParameters(builder, string.Join("&", list));
        }

        public static bool TryCombine(this Uri uri1, string uri2, out Uri outputUri)
        {
            var strippedUri1 = uri1.AbsoluteUri.TrimEnd('/');
            var strippedUri2 = uri2.TrimStart('/');
            return Uri.TryCreate($"{strippedUri1}/{strippedUri2}", UriKind.Absolute, out outputUri);
        }
    }
}
