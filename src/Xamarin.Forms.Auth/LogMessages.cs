﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Xamarin.Forms.Auth
{
    internal static class LogMessages
    {
        public const string BeginningAcquireByRefreshToken = "Begin acquire token by refresh token...";
        public const string NoScopesProvidedForRefreshTokenRequest = "No scopes provided for acquire token by refresh token request. Using default scope instead.";

        public const string CustomWebUiAcquiringAuthorizationCode = "Using CustomWebUi to acquire the authorization code";
        public const string CustomWebUiRedirectUriMatched = "Redirect Uri was matched.  Returning success from CustomWebUiHandler.";
        public const string CustomWebUiOperationCancelled = "CustomWebUi AcquireAuthorizationCode was canceled";

        public const string CustomWebUiCallingAcquireAuthorizationCodeNoPii = "Calling CustomWebUi.AcquireAuthorizationCode";

        public const string CheckMsalTokenResponseReturnedFromBroker = "Checking OAuth2TokenResponse returned from broker. ";

        public const string BrokerResponseContainsAccessToken = "Broker response contains access token. Access token count: ";

        public const string UnknownErrorReturnedInBrokerResponse = "Unknown error returned in broker response. ";

        public const string BrokerInvocationRequired = "Based on auth code received from STS, broker invocation is required. ";

        public const string AddBrokerInstallUrlToPayload = "Broker is required for authentication and broker is not installed on the device. " +
            "Adding BrokerInstallUrl to broker payload. ";

        public const string BrokerInvocationNotRequired = "Based on auth code received from STS, broker invocation is not required. ";

        public const string CanInvokeBrokerAcquireTokenWithBroker = "Can invoke broker. Will attempt to acquire token with broker. ";

        public const string AuthenticationWithBrokerDidNotSucceed = "Broker authentication did not succeed, or the broker install failed. " +
            " for more information. ";

        public static string UsingXScopesForRefreshTokenRequest(int numScopes)
        {
            return string.Format(CultureInfo.InvariantCulture, "Using {0} scopes for acquire token by refresh token request", numScopes);
        }

        public static string ErrorReturnedInBrokerResponse(string error)
        {
            return string.Format(CultureInfo.InvariantCulture, "Error {0} returned in broker response. ", error);
        }

        public static string CustomWebUiCallingAcquireAuthorizationCodePii(Uri authorizationUri, Uri redirectUri)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "calling CustomWebUi.AcquireAuthorizationCode authUri({0}) redirectUri({1})",
                authorizationUri,
                redirectUri);
        }
    }
}
