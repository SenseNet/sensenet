﻿using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace Compatibility.SenseNet.ContentRepository.Security
{
    public class UserAccessProvider : AccessProvider
    {
        private readonly AsyncLocal<IUser> _currentUser = new AsyncLocal<IUser>();

        public override IUser GetCurrentUser()
        {
            var user = _currentUser.Value;
            if (user != null)
                return user;

            // not authenticated yet
            var visitor = User.Visitor;
            SetCurrentUser(visitor);

            return visitor;
        }

        protected override void DoSetCurrentUser(IUser user)
        {
            _currentUser.Value = user;
        }

        public override bool IsAuthenticated
        {
            get
            {
                var user = _currentUser?.Value;
                if (user == null)
                    return false;

                return user.Id != Identifiers.StartupUserId && user.Id != Identifiers.VisitorUserId;
            }
        }
    }
}
