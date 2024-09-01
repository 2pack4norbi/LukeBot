using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LukeBot.Logging;


namespace LukeBot.API
{
    // ClientCredentialsFlow relies purely on submitting a POST request
    // and receiving a token in return. It does not tie its user to any
    // specific account, so it's rarely used. But, it also requires zero
    // user interaction (except for providing client ID/secret).
    class ClientCredentialsFlow: Flow
    {
        private readonly HttpClient mHttpClient = new HttpClient();

        public override AuthToken Request(string lbUser, string scope)
        {
            Logger.Log().Info("Requesting OAuth token...");
            Dictionary<string, string> query = new Dictionary<string, string>();

            query.Add("client_id", mClientID);
            query.Add("client_secret", mClientSecret);
            query.Add("grant_type", "client_credentials");
            query.Add("scope", scope);

            FormUrlEncodedContent content = new FormUrlEncodedContent(query);

            Task<string> contentStrTask = content.ReadAsStringAsync();
            contentStrTask.Wait();
            Logger.Log().Debug("Sending POST request");
            Logger.Log().Secure(" -> Content: {0}", contentStrTask.Result);

            Task<HttpResponseMessage> retMessageTask = mHttpClient.PostAsync(mTokenURL, content);
            retMessageTask.Wait(10000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            Logger.Log().Debug("Response status code is " + retMessage.StatusCode);
            retMessage.EnsureSuccessStatusCode();

            HttpContent retContent = retMessage.Content;
            Logger.Log().Debug("Received content type " + retContent.Headers.ContentType);

            Task<string> retContentStrTask = retContent.ReadAsStringAsync();
            retContentStrTask.Wait();
            string retContentStr = retContentStrTask.Result;

            AuthToken token = AuthToken.FromJson(retContentStr);

            Logger.Log().Debug("Response from {0} OAuth:", mService);
            Logger.Log().Secure("  Access token: {0}", token.access_token);
            Logger.Log().Secure("  Refresh token: {0}", token.refresh_token);
            Logger.Log().Debug("  Timestamp: {0}", token.acquiredTimestamp);
            Logger.Log().Debug("  Expires in: {0}", token.expires_in);
            /*Logger.Log().Debug("  Scope: ");
            foreach (var s in token.scope)
            {
                Logger.Log().Debug("    -> {0}", s);
            }*/
            Logger.Log().Debug("  Token type: {0}", token.token_type);

            return token;
        }

        public override AuthToken Refresh(AuthToken token)
        {
            throw new NotImplementedException();
        }

        public override void Revoke(AuthToken token)
        {
            Logger.Log().Info("Revoking previously acquired OAuth token...");
            Dictionary<string, string> query = new Dictionary<string, string>();

            // TODO client_id and client_secret should come from PropertyStore
            query.Add("client_id", mClientID);
            query.Add("token", token.access_token);

            FormUrlEncodedContent content = new FormUrlEncodedContent(query);
            Task<HttpResponseMessage> retMessageTask = mHttpClient.PostAsync(mRevokeURL, content);
            retMessageTask.Wait(10000);
            HttpResponseMessage retMessage = retMessageTask.Result;

            if (!retMessage.IsSuccessStatusCode)
            {
                Logger.Log().Error("Failed to revoke OAuth token");
            }
            else
            {
                Logger.Log().Info("OAuth Token revoked successfully");
            }
        }


        public ClientCredentialsFlow(string service, string authURL, string refreshURL, string revokeURL)
            : base(service, authURL, refreshURL, revokeURL)
        {
        }
    }
}
