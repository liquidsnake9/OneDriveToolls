using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System.Configuration;
using System.Collections;
using System.Windows.Forms;

namespace OneDriveToolls
{
    static class TokenCacheHelper
    {
        
        /// <summary>
        /// Path to the token cache
        /// </summary>
        //public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";

        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

       
        private static readonly string CacheFilePath = "msalcache.bin";
        private static readonly object FileLock = new object();


        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(System.IO.File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(System.IO.File.ReadAllBytes(CacheFilePath),
                                                  null,
                                                  DataProtectionScope.CurrentUser)
                        : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changesgs in the persistent store
                    System.IO.File.WriteAllBytes(CacheFilePath,
                                        ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
                                                                null,
                                                                DataProtectionScope.CurrentUser)
                                        );
                }
            }
        }
    }

    class ConfigSet {
        const string DEFUALT_KEY = "defaultlogin";
        public string ClientID { get; }
        public List<string> Users { get; }
        private Configuration conf;
        public ConfigSet(string clientid)
        {
            this.ClientID = clientid;
            this.conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private bool HasClientID()
        {
            if (conf.AppSettings.Settings.AllKeys.Where(a => a == this.ClientID).Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasUser(string username)
        {
            this.conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string userList = conf.AppSettings.Settings[this.ClientID].Value;
            if (!this.HasClientID()) {
                return false;
            }
            if (userList.Split(',').Where(a => a == username).Any())
            {
                return true;
            }
            else {
                return false;
            }
        }

        public void AddUser(string username, bool defualtlogin=false) {
                
        }
    }

    class DataRequest
    {
        private IPublicClientApplication app;
        private string userName;
        private string cliendID;
        private string[] scopes;
        private string baseEndpoint;

        public string UserName { get => userName; private set => userName = value; }
        public string CliendID { get => cliendID; private set => cliendID = value; }
        public string[] Scopes { get => scopes; private set => scopes = value; }
        public string BaseEndpoint { get => baseEndpoint; set => baseEndpoint = value; }
        public DataRequest(string clientid, string username, string[] scopes)
        {
            this.userName = username;
            this.cliendID = clientid;
            this.scopes = scopes;
            this.app = PublicClientApplicationBuilder.Create(clientid)
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                .Build();
            app = PublicClientApplicationBuilder.Create(clientid)
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                .Build();
            
        }


        public DataRequest(string clientid, string username) : this(clientid, username, new string[] { "files.read", "Files.ReadWrite" })
        {
        }

        public void EnableCache(string cacheFileName) {
            TokenCacheHelper.EnableSerialization(this.app.UserTokenCache);
        }

        public async Task<string> GetTokenAsync()
        {
            AuthenticationResult authResult = null;
            var accounts = await this.app.GetAccountsAsync();
            try
            {
                authResult = await this.app.AcquireTokenSilent(this.scopes, accounts.Where(o => o.Username == this.userName).FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    authResult = await this.app.AcquireTokenInteractive(this.scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    throw msalex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (authResult != null)
            {
                return authResult.AccessToken;
            }
            else
            {
                return "";
            }
        }

        public async Task<string> GetHttpContentAsync(string endpoint)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, endpoint);
                string token = await this.GetTokenAsync();
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public async Task<User> GetRootDriveAsync()
        {
            InteractiveAuthenticationProvider authProvider = new InteractiveAuthenticationProvider(this.app,this.scopes);
            var graphClient = new GraphServiceClient(authProvider);
            return await graphClient.Me.Request().GetAsync();
        }
    }
}
