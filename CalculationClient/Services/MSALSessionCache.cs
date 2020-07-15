//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C Authentication Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System.Threading;

namespace CalculationClient.Services
{
    /// <summary>
    /// Sample implementation of an MSAL token cache leveraging ASP.Net session state as the backing store
    /// </summary>
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        HttpContext httpContext = null;

        ITokenCache _cache;

        public MSALSessionCache(string userId, HttpContext httpcontext, ITokenCache cache)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            _cache = cache;
        }

        public ITokenCache GetMsalCacheInstance()
        {
            _cache.SetBeforeAccess(BeforeAccessNotification);
            _cache.SetAfterAccess(AfterAccessNotification);
            return _cache;
        }

        public void Load(ITokenCacheSerializer cacheSerializer)
        {
            // Retrieve any existing tokens from session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterReadLock();
            byte[] blob = httpContext.Session.Get(CacheId);
            if(blob != null)
            {
                cacheSerializer.DeserializeMsalV3(blob);
            }
            SessionLock.ExitReadLock();
        }

        public void Persist(ITokenCacheSerializer cacheSerializer)
        {
            // Write the tokens to session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterWriteLock();

            // Reflect changes in the persistent store
            httpContext.Session.Set(CacheId, cacheSerializer.SerializeMsalV3());
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load(args.TokenCache);
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist(args.TokenCache);
            }
        }
    }
}