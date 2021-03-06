﻿//----------------------------------------------------------------------
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
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.Cache
{
    internal class CacheFallbackOperations
    {
        public static void WriteMsalRefreshToken(ITokenCacheAccessor tokenCacheAccessor,
            AdalResultWrapper resultWrapper, string authority, string clientId, string displayableId,
            string identityProvider, string givenName, string familyName, string objectId)
        {
            if (string.IsNullOrEmpty(resultWrapper.RawClientInfo))
            {
                CoreLoggerBase.Default.Info("Client Info is missing. Skipping MSAL RT cache write");
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.RefreshToken))
            {
                CoreLoggerBase.Default.Info("Refresh Token is missing. Skipping MSAL RT cache write");
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.Result.IdToken))
            {
                CoreLoggerBase.Default.Info("Id Token is missing. Skipping MSAL RT cache write");
                return;
            }

            var rtItem = new MsalRefreshTokenCacheItem
            {
                Environment = new Uri(authority).Host,
                Secret = resultWrapper.RefreshToken,
                ClientId = clientId,
                RawClientInfo = resultWrapper.RawClientInfo,
                TenantId = resultWrapper.Result.TenantId
            };
            rtItem.InitRawClientInfoDerivedProperties();

            tokenCacheAccessor.SaveRefreshToken(rtItem.GetRefreshTokenItemKey().ToString(), JsonHelper.SerializeToJson(rtItem));

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem()
            {
                Environment = new Uri(authority).Host,
                TenantId = resultWrapper.Result.TenantId,
                RawClientInfo = resultWrapper.RawClientInfo,
                PreferredUsername = displayableId,
                GivenName = givenName,
                FamilyName = familyName,
                LocalAccountId = objectId
            };
            accountCacheItem.InitRawClientInfoDerivedProperties();

            tokenCacheAccessor.SaveAccount(accountCacheItem.GetAccountItemKey(), JsonHelper.SerializeToJson(accountCacheItem));
        }

        public static void WriteAdalRefreshToken(ILegacyCachePersistance legacyCachePersistance, MsalRefreshTokenCacheItem rtItem, MsalIdTokenCacheItem idItem,
            string authority, string uniqueId, string scope)
        {
            if (rtItem == null)
            {
                CoreLoggerBase.Default.Info("rtItem is null. Skipping MSAL RT cache write");
                return;
            }

            //Using scope instead of resource becaue that value does not exist. STS should return it.
            AdalTokenCacheKey key = new AdalTokenCacheKey(authority, scope, rtItem.ClientId, TokenSubjectType.User, 
                uniqueId, idItem.IdToken.PreferredUsername);
            AdalResultWrapper wrapper = new AdalResultWrapper()
            {
                Result = new AdalResult(null, null, DateTimeOffset.MinValue)
                {
                    UserInfo = new AdalUserInfo()
                    {
                        UniqueId = uniqueId,
                        DisplayableId = idItem.IdToken.PreferredUsername
                    }
                },
                RefreshToken = rtItem.Secret,
                RawClientInfo = rtItem.RawClientInfo,
                //ResourceInResponse is needed to treat RT as an MRRT. See IsMultipleResourceRefreshToken 
                //property in AdalResultWrapper and its usage. Stronger design would be for the STS to return resource
                //for which the token was issued as well on v2 endpoint.
                ResourceInResponse = scope
            };

#if !FACADE && !NETSTANDARD1_3
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            dictionary[key] = wrapper;
            legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
#endif
        }

/*
        public static List<MsalRefreshTokenCacheItem> GetAllAdalUsersForMsal(ILegacyCachePersistance legacyCachePersistance, 
            string environment, string clientId)
        {
            //returns all the adal entries where client info is present
            List<MsalRefreshTokenCacheItem> list = GetAllAdalEntriesForMsal(legacyCachePersistance, environment, clientId, null, null);
            //TODO return distinct clientinfo only
            return list.Where(p => !string.IsNullOrEmpty(p.RawClientInfo)).ToList();
        }
        */
        public static Dictionary<String, AdalUserInfo> GetAllAdalUsersForMsal(ILegacyCachePersistance legacyCachePersistance,
            string environment, string clientId)
        {
            Dictionary<String, AdalUserInfo> users = new Dictionary<String, AdalUserInfo>();
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
                //filter by client id and environment first
                //TODO - authority check needs to be updated for alias check
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId) && environment.Equals(new Uri(p.Key.Authority).Host)).ToList();

                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in listToProcess)
                {
                    if (!string.IsNullOrEmpty(pair.Value.RawClientInfo))
                    {
                        users.Add(pair.Value.RawClientInfo, pair.Value.Result.UserInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                CoreLoggerBase.Default.Warning("GetAllAdalUsersForMsal falid due to Exception - " + ex);
            }
            return users;
        }

        public static List<MsalRefreshTokenCacheItem> GetAllAdalEntriesForMsal(ILegacyCachePersistance legacyCachePersistance, 
            string environment, string clientId, string upn, string uniqueId, string rawClientInfo)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
                //filter by client id and environment first
                //TODO - authority check needs to be updated for alias check
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId) && environment.Equals(new Uri(p.Key.Authority).Host)).ToList();

                //if client info is provided then use it to filter
                if (!string.IsNullOrEmpty(rawClientInfo))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> clientInfoEntries =
                        listToProcess.Where(p => rawClientInfo.Equals(p.Value.RawClientInfo)).ToList();
                    if (clientInfoEntries.Any())
                    {
                        listToProcess = clientInfoEntries;
                    }
                }

                //if upn is provided then use it to filter
                if (!string.IsNullOrEmpty(upn))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> upnEntries =
                        listToProcess.Where(p => upn.Equals(p.Key.DisplayableId)).ToList();
                    if (upnEntries.Any())
                    {
                        listToProcess = upnEntries;
                    }
                }

                //if userId is provided then use it to filter
                if (!string.IsNullOrEmpty(uniqueId))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> uniqueIdEntries =
                        listToProcess.Where(p => uniqueId.Equals(p.Key.UniqueId)).ToList();
                    if (uniqueIdEntries.Any())
                    {
                        listToProcess = uniqueIdEntries;
                    }
                }

                List<MsalRefreshTokenCacheItem> list = new List<MsalRefreshTokenCacheItem>();
                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in listToProcess)
                {
                    list.Add(
                        new MsalRefreshTokenCacheItem()
                        {
                            RawClientInfo = pair.Value.RawClientInfo,
                            Secret = pair.Value.RefreshToken,
                            ClientId = pair.Key.ClientId,
                            Environment = environment,
                            ClientInfo = ClientInfo.CreateFromJson(pair.Value.RawClientInfo)
                        });
                }

                return list;
            }
            catch (Exception ex)
            {
                CoreLoggerBase.Default.Warning("GetAllAdalEntriesForMsal falid due to Exception - " + ex);

                return new List<MsalRefreshTokenCacheItem>();
            }
        }

        public static MsalRefreshTokenCacheItem GetAdalEntryForMsal(ILegacyCachePersistance legacyCachePersistance, 
            string environment, string clientId, string upn, string uniqueId, string rawClientInfo)
        {
            return GetAllAdalEntriesForMsal(legacyCachePersistance, environment, clientId, upn, uniqueId, rawClientInfo).FirstOrDefault();
        }

        public static AdalResultWrapper FindMsalEntryForAdal(ITokenCacheAccessor tokenCacheAccessor, string authority,
            string clientId, string upn)
        {
            foreach (var rtString in tokenCacheAccessor.GetAllRefreshTokensAsString())
            {
                var rtCacheItem =
                    JsonHelper.DeserializeFromJson<MsalRefreshTokenCacheItem>(rtString);

                var accountStr = tokenCacheAccessor.GetAccount(rtCacheItem.GetAccountItemKey());

                var accountItem =
                    JsonHelper.DeserializeFromJson<MsalAccountCacheItem>(accountStr);

                //TODO - authority check needs to be updated for alias check
                if (new Uri(authority).Host.Equals(rtCacheItem.Environment) && rtCacheItem.ClientId.Equals(clientId) &&
                    accountItem.PreferredUsername.Equals(upn))
                    return new AdalResultWrapper
                    {
                        Result = new AdalResult(null, null, DateTimeOffset.MinValue),
                        RefreshToken = rtCacheItem.Secret,
                        RawClientInfo = rtCacheItem.RawClientInfo
                    };
            }

            return null;
        }
    }
}
