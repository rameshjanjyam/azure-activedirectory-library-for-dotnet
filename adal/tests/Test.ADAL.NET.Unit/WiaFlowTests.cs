//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;

namespace Test.ADAL.NET.Unit
{
    /// <summary>
    /// This test class executes and validates OBO scenarios where token cache may or may not 
    /// contain entries with user assertion hash. It accounts for cases where there is
    /// a single user and when there are multiple users in the cache.
    /// user assertion hash exists so that the API can deterministically identify the user
    /// in the cache when a usernae is not passed in. It also allows the API to acquire
    /// new token when a different assertion is passed for the user. this is needed because
    /// the user may have authenticated with updated claims like MFA/device auth on the client.
    /// </summary>
    [TestClass]
    public class WiaFlowTests
    {
        private readonly DateTimeOffset _expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30);
        private static readonly string[] _cacheNoise = { "", "different" };

        [TestInitialize]
        public void TestInitialize()
        {
            //HttpMessageHandlerFactory.InitializeMockProvider();
            ResetInstanceDiscovery();
        }

        protected void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        [TestCategory("WiaFlowTests")]
        public async Task TestWia_WhenMethodIsCalled_ShouldReturnToken()  // This is currently a happy path real world test
        {
            var authority = "https://login.microsoftonline.com/common"; // TestConstants.DefaultAuthorityHomeTenant
            var resource = "00000002-0000-0000-c000-000000000000";  // Graph
            var clientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";  // A real client ID, borrowed from Azure CLI https://github.com/Azure/azure-cli/blob/azure-cli-vm-2.0.16/src/azure-cli-core/azure/cli/core/_profile.py#L56
            var context = new AuthenticationContext(authority, new TokenCache());
            var result = await context.AcquireTokenAsync(resource, clientId, new UserCredential());
            Assert.IsNotNull(result.AccessToken);
        }
    }
}
