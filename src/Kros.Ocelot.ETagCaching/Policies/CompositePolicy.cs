﻿namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CompositePolicy : IETagCachePolicy
{
    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
