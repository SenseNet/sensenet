using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Security.Cryptography;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security.ADSync
{
    [ContentHandler]
    public class ADSettings : Settings
    {
        protected static readonly string PASSWORDKEY = "CachedPassword";

        // ================================================================================= Constructors

        public ADSettings(Node parent) : this(parent, null) {}
        public ADSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected ADSettings(NodeToken nt) : base(nt) {}

        // ================================================================================= Properties

        private IDictionary<string, string> _cachedPasswords;

        // ================================================================================= Overrides

        public override void Save(NodeSaveSettings settings)
        {
            var settingsObject = DeserializeToJObject(this.Binary.GetStream());
            if (settingsObject != null)
            {
                ReplaceOrEncodePasswords(settingsObject);

                this.Binary.SetStream(RepositoryTools.GetStreamFromString(settingsObject.ToString()));
            }

            base.Save(settings);
        }

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _cachedPasswords = (IDictionary<string, string>)base.GetCachedData(PASSWORDKEY);

            if (_cachedPasswords == null)
            {
                _cachedPasswords = GetPasswords(BinaryAsJObject);

                if (_cachedPasswords != null)
                    base.SetCachedData(PASSWORDKEY, _cachedPasswords);
            }
        }

        // ================================================================================= Internal API

        protected static List<JToken> GetCredentialsTokens(JObject settings)
        {
            var tokens = new List<JToken>();
            var servers = settings["Servers"] as JArray;
            if (servers != null)
            {
                tokens.AddRange(servers.Select(server => server["LogonCredentials"]).Where(cred => cred != null));
            }

            return tokens;
        }

        protected static IDictionary<string, string> GetPasswords(JObject settings)
        {
            if (settings == null)
                return null;

            var servers = settings["Servers"] as JArray;
            if (servers == null)
                return null;

            // Build a dictionary from server ids and passwords (we assume that 
            // every stored server has a unique id).
            return servers.Where(st => !string.IsNullOrEmpty(st.Value<string>("Id"))).ToDictionary(
                server => server.Value<string>("Id"),
                server =>
                {
                    var credentials = server["LogonCredentials"];
                    if (credentials != null)
                    {
                        var pw = credentials["Password"];
                        if (pw != null)
                            return pw.Value<string>();
                    }

                    return string.Empty;
                });
        }

        protected void ReplaceOrEncodePasswords(JObject settings)
        {
            // Iterate through all server credentials in the json and examine the passwords.
            // If it is a GUID we set before, simply replace the original (already encoded)
            // password. If it is an unknown string (meaning a new password), encode it.
            if (settings == null)
                return;

            var servers = settings["Servers"] as JArray;
            if (servers == null)
                return;

            foreach (var server in servers)
            {
                var serverId = server.Value<string>("Id");
                if (string.IsNullOrEmpty(serverId))
                {
                    // this is a new server, it does not have an id yet
                    server["Id"] = serverId = Guid.NewGuid().ToString();
                }

                var credentials = server["LogonCredentials"];
                if (credentials == null)
                    continue;

                var pw = (string)credentials["Password"];
                if (!string.IsNullOrEmpty(pw))
                {
                    // the client provided a password, we have to encrypt it
                    credentials["Password"] = CryptoServiceProvider.Encrypt(pw);
                }
                else
                {
                    // empty password: try to find it in the cache
                    string cachedPassword;
                    if (_cachedPasswords != null && _cachedPasswords.TryGetValue(serverId, out cachedPassword))
                    {
                        // found the original encoded value, we have to inject it before saving the json
                        credentials["Password"] = cachedPassword;
                    }
                }
            }
        }

        // ================================================================================= Public API

        public Stream RemovePasswords()
        {
            return RemovePasswords(this.Binary.GetStream());
        }
        public Stream RemovePasswords(Stream stream)
        {
            // This method is an instance method and not static because
            // it needs to access the cached password values.
            if (stream == null)
                return null;

            if (_cachedPasswords == null)
                return stream;

            var settings = DeserializeToJObject(stream);
            var servers = settings["Servers"] as JArray;

            if (servers == null || servers.Count == 0)
                return stream;

            foreach (var server in servers)
            {
                var credentials = server["LogonCredentials"];
                if (credentials == null)
                    continue;

                var pw = (string)credentials["Password"];
                if (string.IsNullOrEmpty(pw))
                    continue;

                if (!_cachedPasswords.Values.Contains(pw))
                    continue;

                // erase the password before sending the json to the client
                credentials["Password"] = string.Empty;
            }

            return RepositoryTools.GetStreamFromString(settings.ToString());
        }

        public bool IncludePasswords()
        {
            // Save permission is needed for this setting to be able to se even the encrypted values
            if (!this.Security.HasPermission(PermissionType.Save))
                return false;

            // in case of export or other special scenario, include the encrypted values
            if (HttpContext.Current == null)
                return true;

            var includePassStr = HttpContext.Current.Request["includepasswords"];
            if (!string.IsNullOrEmpty(includePassStr))
            {
                bool includePass;
                if (bool.TryParse(includePassStr, out includePass))
                    return includePass;
            }

            return false;
        }
    }
}
