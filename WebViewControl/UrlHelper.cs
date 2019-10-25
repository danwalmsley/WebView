﻿using System;

namespace WebViewControl {

    internal static class UrlHelper {

        private const string ChromeInternalProtocol = "chrome-devtools:";

        public const string AboutBlankUrl = "about:blank";

        public static ResourceUrl DefaultLocalUrl = new ResourceUrl(ResourceUrl.LocalScheme, "index.html");

        public static bool IsChromeInternalUrl(string url) {
            return url == AboutBlankUrl || (url != null && url.StartsWith(ChromeInternalProtocol, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsInternalUrl(string url) {
            return IsChromeInternalUrl(url) || url.StartsWith(DefaultLocalUrl.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
