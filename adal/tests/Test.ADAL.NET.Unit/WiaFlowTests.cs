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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class WiaFlowTests
    {
        // This can be run individually to test the functionality for real,
        // but it won't work when running all test cases, because that way it will somehow still try to consume a nonexistent mock
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
