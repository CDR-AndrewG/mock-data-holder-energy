using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CDR.DataHolder.IntegrationTests.BaseTest;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    /// <summary>
    /// Get access token from DataHolder.
    /// Cache request (user/selectedaccounts/scope) and refreshtoken.
    /// If cache miss then perform full E2E auth/consent flow to get accesstoken/refresh token and cache returned refresh token.
    /// If cache hit then use cached refreshtoken to get new accesstoken.
    /// </summary>
    public class BROKEN_DataHolder_AccessToken_Cache
    {
        public int Hits { get; private set; } = 0;
        public int Misses { get; private set; } = 0;

        class CacheItem
        {
            public string? UserId { get; init; }
            public string? Scope { get; init; }
            public string? RefreshToken { get; set; }
        }

        readonly List<CacheItem> cache = new();

        public async Task<string?> GetAccessToken(
            string userId,
            string selectedAccounts,
            string scope = BaseTest.SCOPE
            )
        {
            async Task<(string accessToken, string refreshToken)> FromAuthConsentFlow()
            {
                (var authCode, _, var codeVerifier) = await new DataHolder_Authorise_APIv2
                {
                    UserId = userId,
                    OTP = BaseTest.AUTHORISE_OTP,
                    SelectedAccountIds = selectedAccounts,
                    Scope = scope
                }.Authorise();

                // use authcode to get access and refresh tokens
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode, codeVerifier: codeVerifier);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - access token is null");

                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            async Task<(string accessToken, string refreshToken)> FromRefreshToken(string? refreshToken)
            {
                if (refreshToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - refresh token is null");

                var tokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken, scope: scope);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - access token is null");
                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            // Find refresh token in cache
            var cacheHit = cache.Find(item => item.UserId == userId && item.Scope == scope);

            // Cache hit
            if (cacheHit != null)
            {
                Hits++;

                // Use refresh token from cache to get access token
                (var accessToken, var refreshToken) = await FromRefreshToken(cacheHit.RefreshToken);

                // Update refresh token in cache
                cacheHit.RefreshToken = refreshToken;

                // Return access token
                return accessToken;
            }
            // Cache miss, so perform auth/consent flow to get accesstoken/refreshtoken
            else
            {
                Misses++;

                (var accessToken, var refreshToken) = await FromAuthConsentFlow();

                // Add refresh token to cache
                cache.Add(new CacheItem { UserId = userId, Scope = scope, RefreshToken = refreshToken });

                // Return access token
                return accessToken;
            }
        }
    }

    /// <summary>
    /// Get access token from DataHolder.
    /// Cache request (user/selectedaccounts/scope) and accesstoken.
    /// If cache miss then perform full E2E auth/consent flow to get accesstoken, cache it, and return access token.
    /// If cache hit then use cached access token.
    /// </summary>
    public class DataHolder_AccessToken_Cache
    {
        public int Hits { get; private set; } = 0;
        public int Misses { get; private set; } = 0;

        class CacheItem
        {
            public string? UserId { get; init; }
            public string? SelectedAccounts { get; init; }
            public string? Scope { get; init; }

            public string? AccessToken { get; set; }
        }

        readonly List<CacheItem> cache = new();

        public async Task<string?> GetAccessToken(TokenType tokenType, string scope = BaseTest.SCOPE)
        {
            switch (tokenType)
            {
                case TokenType.MARY_MOSS:
                case TokenType.STEVE_KENNEDY:
                case TokenType.DEWAYNE_STEVE:
                case TokenType.BUSINESS_1:
                case TokenType.BUSINESS_2:
                case TokenType.BEVERAGE:
                case TokenType.KAMILLA_SMITH:
                    {
                        return await GetAccessToken(tokenType.UserId(), tokenType.AllAccountIds(), scope);
                    }

                case TokenType.INVALID_FOO:
                    return "foo";
                case TokenType.INVALID_EMPTY:
                    return "";
                case TokenType.INVALID_OMIT:
                    return null;

                default:
                    throw new ArgumentException($"{nameof(tokenType)} = {tokenType}");
            }
        }

        public async Task<string?> GetAccessToken(
            string userId,
            string selectedAccounts,
            string scope = BaseTest.SCOPE)
        {
            async Task<(string accessToken, string refreshToken)> FromAuthConsentFlow()
            {
                (var authCode, _, var codeVerifier) = await new DataHolder_Authorise_APIv2
                {
                    UserId = userId,
                    OTP = BaseTest.AUTHORISE_OTP,
                    SelectedAccountIds = selectedAccounts,
                    Scope = scope
                }.Authorise();

                // use authcode to get access and refresh tokens
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode, codeVerifier: codeVerifier);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - access token is null");

                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            // Find refresh token in cache
            var cacheHit = cache.Find(item =>
                item.UserId == userId &&
                item.SelectedAccounts == selectedAccounts &&
                item.Scope == scope);

            // Cache hit
            if (cacheHit != null)
            {
                Hits++;

                return cacheHit.AccessToken;
            }
            // Cache miss, so perform auth/consent flow to get accesstoken/refreshtoken
            else
            {
                Misses++;

                (var accessToken, _) = await FromAuthConsentFlow();

                // Add refresh token to cache
                cache.Add(new CacheItem
                {
                    UserId = userId,
                    SelectedAccounts = selectedAccounts,
                    Scope = scope,
                    AccessToken = accessToken
                });

                // Return access token
                return accessToken;
            }
        }
    }
}
