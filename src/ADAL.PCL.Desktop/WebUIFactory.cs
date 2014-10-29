﻿//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Net;
using System.Threading;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUIFactory : IWebUIFactory
    {
        private AuthorizationParameters parameters;

        public IWebUI CreateAuthenticationDialog(IAuthorizationParameters parameters)
        {
            this.parameters = parameters as AuthorizationParameters;
            if (this.parameters == null)
            {
                throw new ArgumentException("parameters should be of type AuthorizationParameters", "parameters");
            }

            switch (this.parameters.PromptBehavior)
            {
                case PromptBehavior.Auto:
                    return new InteractiveWebUI { OwnerWindow = this.parameters.OwnerWindow };
                case PromptBehavior.Always:
                case PromptBehavior.RefreshSession:
                    return new InteractiveWebUI { OwnerWindow = this.parameters.OwnerWindow };
                case PromptBehavior.Never:
                    return new SilentWebUI { OwnerWindow = this.parameters.OwnerWindow };
                default:
                    throw new InvalidOperationException("Unexpected PromptBehavior value");
            }
        }

        public async Task<IHttpWebResponse> GetResponseWithTimeoutSyncOrAsync(HttpWebRequest request, int timeoutInMilliSeconds, CallState callState)
        {
            if (callState != null && callState.CallSync)
            {
                request.Timeout = timeoutInMilliSeconds;
                return PlatformPlugin.HttpWebRequestFactory.CreateResponse(request.GetResponse());
            }

            Task<WebResponse> getResponseTask = request.GetResponseAsync();
            ThreadPool.RegisterWaitForSingleObject(
                ((IAsyncResult)getResponseTask).AsyncWaitHandle,
                delegate(object state, bool timedOut)
                {
                    if (timedOut)
                    {
                        ((HttpWebRequest)state).Abort();
                    }
                },
                request,
                timeoutInMilliSeconds,
                true);

            return PlatformPlugin.HttpWebRequestFactory.CreateResponse(await getResponseTask);
        }       
    }
}
