using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines a class that contains a list of group ids for extending the membership of a user.
    /// The user will be a member of these groups temporarily, for the lifetime of the request.
    /// </summary>
    public class MembershipExtension
    {
        public static readonly MembershipExtension Placeholder = new MembershipExtension((IEnumerable<int>)null);

        /// <summary>
        /// Initializes a new instance of the <see cref="MembershipExtension"/>.
        /// </summary>
        /// <param name="extension">The collection of groups that extends the membership of a user.</param>
        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            ExtensionIds = extension?.Select(x => x.Id).ToArray() ?? new int[0];
        }
        public MembershipExtension(IEnumerable<int> identities)
        {
            ExtensionIds = identities?.ToArray() ?? new int[0];
        }

        internal void AddIdentities(params int[] identities)
        {
            if (!(identities?.Any() ?? false))
                return;

            if (this == MembershipExtenderBase.EmptyExtension || this == Placeholder)
            {
                // cannot add ids to the pinned empty extension object
                return;
            }

            ExtensionIds = ExtensionIds.Union(identities).ToArray();
        }

        /// <summary>
        /// Gets the collection of group ids that extends the membership of a user.
        /// </summary>
        public IEnumerable<int> ExtensionIds { get; private set; }
    }

    /// <summary>
    /// Defines a base class for extending a users's membership.
    /// Inherited classes can customize the algorithm of selecting additional groups.
    /// </summary>
    public class MembershipExtenderBase
    {
        /// <summary>
        /// Defines a constant for empty extension groups.
        /// </summary>
        public static readonly MembershipExtension EmptyExtension = new MembershipExtension(new ISecurityContainer[0]);
        private static MembershipExtenderBase Instance => Providers.Instance.MembershipExtender;

        private static readonly string[] InternalMembershipExtenderTypeNames =
        {
            "SenseNet.Services.Sharing.SharingMembershipExtender"
        };

        private static readonly Lazy<MembershipExtenderBase[]> InternalExtenders = new Lazy<MembershipExtenderBase[]>(
            () =>
            {
                return InternalMembershipExtenderTypeNames.Select(tn =>
                {
                    try
                    {
                        if (TypeResolver.GetType(tn, false) == null)
                        {
                            SnTrace.System.WriteError($"Membership extender type not found: {tn}");
                            return null;
                        }

                        return TypeResolver.CreateInstance<MembershipExtenderBase>(tn);
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, $"Error loading internal membership extender: {tn}");
                    }

                    return null;
                }).Where(me => me != null).ToArray();
            });

        private static MembershipExtenderBase[] InternalInstances => InternalExtenders.Value;

        static MembershipExtenderBase()
        {
        }

        /// <summary>
        /// Extends the specified user's membership by setting its MembershipExtension property.
        /// </summary>
        /// <param name="user">The <see cref="IUser"/> instance that's membership will be extended.</param>
        public static void Extend(IUser user)
        {
            Instance?.ExtendPrivate(user);

            // Enumerate all internal extenders and collect additional
            // identities to add them to the user's membership list.
            var internalIds = InternalInstances.SelectMany(ime => ime.GetExtension(user).ExtensionIds).ToArray();
            if (!internalIds.Any())
                return;

            // create the initial extender or add ids to the existing one
            user.AddMembershipIdentities(internalIds);
        }
        private void ExtendPrivate(IUser user)
        {
            user.MembershipExtension = GetExtension(user) ?? EmptyExtension;
        }

        /// <summary>
        /// Produces a <see cref="MembershipExtension"/> instance for the given user.
        /// The return value can be assigned to the given <see cref="IUser"/>'s MembershipExtension property.
        /// </summary>
        /// <param name="user">The <see cref="IUser"/> instance that's <see cref="MembershipExtension"/> will be created.</param>
        public virtual MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    /// <summary>
    /// Implements the default class inheriting <see cref="MembershipExtenderBase"/>.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class DefaultMembershipExtender : MembershipExtenderBase
    {
        /// <summary>
        /// The return value is always <see cref="MembershipExtenderBase.EmptyExtension"/>.
        /// </summary>
        public override MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    internal static class MembershipUserExtensions
    {
        /// <summary>
        /// Adds identities to the extended membership list of the user. If the current
        /// list is the pinned empty or placeholder object, this method replaces it
        /// with a new one, containing the additional identities.
        /// </summary>
        public static void AddMembershipIdentities(this IUser user, params int[] identities)
        {
            if (user == null || !(identities?.Any() ?? false))
                return;

            var currentExtension = user.MembershipExtension;

            if (currentExtension == null || 
                currentExtension == MembershipExtenderBase.EmptyExtension || 
                currentExtension == MembershipExtension.Placeholder)
            {
                // Pinned empty extension object found, creating a new one.
                user.MembershipExtension = new MembershipExtension(identities);
                return;
            }
            
            user.MembershipExtension.AddIdentities(identities);
        }
    }
}
