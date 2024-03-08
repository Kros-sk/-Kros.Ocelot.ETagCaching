﻿using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Request.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class DefaultPolicy : IETagCachePolicy
{
    private DefaultPolicy()
    {
    }

    public static DefaultPolicy Instance { get; } = new DefaultPolicy();

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.EnableETagCache = !HaveRequestNoCacheHeader(context);
        context.AllowCacheResponseETag = IsGetMethod(context);
        context.ETagExpirationTimeSpan = TimeSpan.FromSeconds(30);
        context.CacheKey = CreateCacheKey(context.DownstreamRequest);
        if (AllowServeFromCache(context, out var entityTag))
        {
            context.ETag = entityTag;
            context.AllowNotModified = true;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.StatusCode = System.Net.HttpStatusCode.NotModified;
        AddCacheHeaders(context.CachedResponseHeaders,context.ETag.ToString());

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.AllowCacheResponseETag = false;

        if (AllowCacheResponseETag(context))
        {
            context.AllowCacheResponseETag = true;
            context.ETag = new EntityTagHeaderValue($"\"{Guid.NewGuid()}\"");
            AddCacheHeaders(context.ResponseHeaders, context.ETag.ToString());
        }

        return ValueTask.CompletedTask;
    }

    private static void AddCacheHeaders(HeaderDictionary headers, string etag)
    {
        headers[HttpHeadersHelper.CacheControlHeaderName] = CacheControlHeaderValue.PrivateString;
        headers[HttpHeadersHelper.ETagHeaderName] = etag;
    }

    private static bool AllowCacheResponseETag(ETagCacheContext context)
    {
        if (!IsGetMethod(context))
        {
            return false;
        }

        if (context.DownstreamResponse is null
            || context.DownstreamResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return false;
        }

        if (HaveCacheHeaderValue(context.DownstreamRequest.Headers, HttpHeadersHelper.NoStoreHeaderValue))
        {
            return false;
        }

        return true;
    }

    private static bool HaveRequestNoCacheHeader(ETagCacheContext context)
        => HaveCacheHeaderValue(context.DownstreamRequest.Headers, HttpHeadersHelper.NoCacheHeaderValue);

    private static bool HaveCacheHeaderValue(System.Net.Http.Headers.HttpHeaders headers, string value)
        => headers.TryGetValues(HttpHeadersHelper.CacheControlHeaderName, out var values)
            && values.Contains(value);

    private static bool IsGetMethod(ETagCacheContext context)
        => context.DownstreamRequest.Method == HttpMethods.Get;

    private static string CreateCacheKey(DownstreamRequest downstreamRequest)
    {
        const char delimiter = ':';
        var sb = new StringBuilder();
        sb.Append(downstreamRequest.Method.ToLower());
        sb.Append(delimiter);
        sb.Append(downstreamRequest.Scheme.ToLower());
        sb.Append(delimiter);
        sb.Append(downstreamRequest.Host.ToLower());
        sb.Append(delimiter);
        sb.Append(downstreamRequest.AbsolutePath.ToLower());
        sb.Append(delimiter);
        sb.Append(downstreamRequest.Query.ToLower());

        return sb.ToString();
    }

    private static bool AllowServeFromCache(ETagCacheContext context, [NotNullWhen(true)] out EntityTagHeaderValue? entityTag)
    {
        entityTag = null;
        if (context.DownstreamRequest.Headers.TryGetValues(HttpHeadersHelper.IfNoneMatchHeaderName, out var values)
            && values.Count() == 1)
        {
            entityTag = EntityTagHeaderValue.Parse(values.First());
            return true;
        }

        return false;
    }
}
