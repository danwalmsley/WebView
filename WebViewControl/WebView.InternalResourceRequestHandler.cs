﻿using System;
using Xilium.CefGlue;

namespace WebViewControl {

    partial class WebView {

        private class InternalResourceRequestHandler : CefResourceRequestHandler, IDisposable {

            public InternalResourceRequestHandler(WebView ownerWebView) {
                OwnerWebView = ownerWebView;
            }

            private WebView OwnerWebView { get; }

            private ResourcesProvider ResourcesProvider { get; } = new ResourcesProvider();

            public void Dispose() {
                ResourcesProvider.Dispose();
            }

            protected override CefCookieAccessFilter GetCookieAccessFilter(CefBrowser browser, CefFrame frame, CefRequest request) {
                return null;
            }

            protected override CefResourceHandler GetResourceHandler(CefBrowser browser, CefFrame frame, CefRequest request) {
                if (request.Url == OwnerWebView.DefaultLocalUrl) {
                    return OwnerWebView.htmlToLoad != null ? AsyncResourceHandler.FromText(OwnerWebView.htmlToLoad) : null;
                }

                if (UrlHelper.IsChromeInternalUrl(request.Url)) {
                    return null;
                }

                var resourceHandler = new ResourceHandler(request, OwnerWebView.GetRequestUrl(request.Url, (ResourceType)request.ResourceType));

                void TriggerBeforeResourceLoadEvent() {
                    var beforeResourceLoad = OwnerWebView.BeforeResourceLoad;
                    if (beforeResourceLoad != null) {
                        OwnerWebView.ExecuteWithAsyncErrorHandling(() => beforeResourceLoad(resourceHandler));
                    }
                }

                if (Uri.TryCreate(resourceHandler.Url, UriKind.Absolute, out var url) && url.Scheme == ResourceUrl.EmbeddedScheme) {
                    resourceHandler.BeginAsyncResponse(() => {
                        var urlWithoutQuery = new UriBuilder(url);
                        if (!string.IsNullOrEmpty(url.Query)) {
                            urlWithoutQuery.Query = string.Empty;
                        }

                        OwnerWebView.ExecuteWithAsyncErrorHandling(() => ResourcesProvider.LoadEmbeddedResource(resourceHandler, urlWithoutQuery.Uri));

                        TriggerBeforeResourceLoadEvent();

                        if (resourceHandler.Handled || OwnerWebView.IgnoreMissingResources) {
                            return;
                        }

                        var resourceLoadFailed = OwnerWebView.ResourceLoadFailed;
                        if (resourceLoadFailed != null) {
                            resourceLoadFailed(url.ToString());
                        } else {
                            OwnerWebView.ExecuteWithAsyncErrorHandling(() => throw new InvalidOperationException("Resource not found: " + url));
                        }
                    });
                } else {
                    TriggerBeforeResourceLoadEvent();
                }

                return resourceHandler.Handler;
            }
        }
    }
}