using System;
using System.Web.Security;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Security
{
    public class SenseNetMembershipProvider : MembershipProvider
    {
        public SenseNetMembershipProvider()
        {
            SnLog.WriteInformation("MembershipProvider instantiated: " + typeof(SenseNetMembershipProvider).FullName, EventId.RepositoryLifecycle);
        }

        private string _path;
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(name))
                name = "SenseNetMembershipProvider";
            if (!String.IsNullOrEmpty(config["path"]))
                _path = config["path"];
            config.Remove("path");

            base.Initialize(name, config);

            SnTrace.Repository.Write("SenseNetMembershipProvider initialized: " + this);
        }


        public override string ApplicationName
        {
            get
            {
                throw new SnNotSupportedException();
            }
            set
            {
                throw new SnNotSupportedException();
            }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new SnNotSupportedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new SnNotSupportedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new SnNotSupportedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new SnNotSupportedException();
        }

        public override bool EnablePasswordReset
        {
            get { return false; }
        }


        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new SnNotSupportedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new SnNotSupportedException();
        }


        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <param name="pageIndex">Index of the page. (currently ignored)</param>
        /// <param name="pageSize">Size of the page. (currently ignored)</param>
        /// <param name="totalRecords">The total records.</param>
        /// <returns></returns>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection users = new MembershipUserCollection();

            var cq = $"+TypeIs:{typeof(User).Name}";
            if (_path != null)
                cq += " +InTree:{_path}";
            cq += $" .TOP:{pageSize} .SKIP:{pageIndex*pageSize}";
            var result = ContentQuery.Query(cq, QuerySettings.AdminSettings);

            // get paged resultlist
            foreach (Node node in result.Nodes)
                users.Add(GetMembershipUser((User)node));

            // get total number of users
            totalRecords = result.Count;

            return users;
        }



        public override int GetNumberOfUsersOnline()
        {
            throw new SnNotSupportedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new SnNotSupportedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new SnNotSupportedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (userIsOnline)
                throw new SnNotSupportedException("UserIsOnline must be false.");

            int nodeId;

            try
            {
                nodeId = (int)providerUserKey;
            }
            catch (Exception ex) // rethrow
            {
                throw new ArgumentException("Cannot convert the user primary key.", "providerUserKey", ex);
            }

            User user = Node.Load<User>(nodeId);

            return new MembershipUser(
                this.Name, // providerName
                user.Name,
                providerUserKey,
                user.Email,
                string.Empty,
                string.Empty,
                user.Enabled,
                !user.Enabled,
                user.CreationDate,
                DateTime.MinValue,
                DateTime.MinValue,
                DateTime.MinValue,
                DateTime.MinValue);
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new SnNotSupportedException();
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new SnNotSupportedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new SnNotSupportedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new SnNotSupportedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new SnNotSupportedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new SnNotSupportedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new SnNotSupportedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new SnNotSupportedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new SnNotSupportedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new SnNotSupportedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new SnNotSupportedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            int indexBackSlash = username.IndexOf("\\");
            string domain =
                indexBackSlash > 0 ? username.Substring(0, indexBackSlash) : IdentityManagement.DefaultDomain;

            username = username.Substring(username.IndexOf("\\") + 1);

            if (string.IsNullOrEmpty(username))
                return false;

            // if forms AD auth is configured, authenticate user with AD
            var adProvider = DirectoryProvider.Current;
            if (adProvider != null)
            {
                if (adProvider.IsADAuthEnabled(domain))
                {
                    return adProvider.IsADAuthenticated(domain, username, password);
                }
            }

            // we need to load the user with admin account here
            using (new SystemAccount())
            {
                var user = User.Load(domain, username);
                if (user == null || !user.Enabled)
                    return false;

                return user.CheckPasswordMatch(password);
            }
        }

        private MembershipUser GetMembershipUser(User portalUser)
        {
            MembershipUser membershipUser = new MembershipUser(
                                    Name,                       // Provider name
                                    portalUser.Username,                   // Username
                                    portalUser.Username,                   // providerUserKey
                                    portalUser.Email,                      // Email
                                    String.Empty,               // passwordQuestion
                                    String.Empty,               // Comment
                                    true,                       // isApproved
                                    false,                      // isLockedOut
                                    DateTime.UtcNow,               // creationDate
                                    DateTime.UtcNow,                  // lastLoginDate
                                    DateTime.UtcNow,               // lastActivityDate
                                    DateTime.UtcNow,               // lastPasswordChangedDate
                                    new DateTime(1980, 1, 1)    // lastLockoutDate
                                );
            return membershipUser;
        }
    }
}
