using System;
using System.Globalization;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// VersionNumber is for handling version numbers in major.minor form.
    /// </summary>
	public class VersionNumber : IComparable, ICloneable
    {
        private enum AbstractVersion { LastAccessible, LastFinalized, LastMajor, LastMinor, Header, Defined }

        public static readonly VersionNumber LastAccessible = new VersionNumber(AbstractVersion.LastAccessible);
        public static readonly VersionNumber LastFinalized = new VersionNumber(AbstractVersion.LastFinalized);
        public static readonly VersionNumber LastMajor = new VersionNumber(AbstractVersion.LastMajor);
        public static readonly VersionNumber LastMinor = new VersionNumber(AbstractVersion.LastMinor);
        public static readonly VersionNumber Header = new VersionNumber(AbstractVersion.Header);

        private VersionNumber(AbstractVersion abstractVersion)
        {
            _abstractVersion = abstractVersion;
            _major = -1;
            _minor = -1 - (int)abstractVersion;
        }

        private AbstractVersion _abstractVersion = AbstractVersion.Defined;
        public bool IsAbstractVersion
        {
            get { return _abstractVersion != AbstractVersion.Defined; }
        }

        // =========================================================================== Fields

        private int _major;
        private int _minor;
        private VersionStatus _status = VersionStatus.Approved;

        // =========================================================================== Properties

        /// <summary>
        /// Gets the version string.
        /// </summary>
        /// <value>The version string.</value>
        public string VersionString
        {
            get { return this.ToString(); }
        }

        /// <summary>
        /// Gets the major version of the given VersionNumber.
        /// </summary>
        /// <value>The major.</value>
		public int Major
        {
            get { return _major; }
        }
        /// <summary>
        /// Gets the minor version of the given VersionNumber.
        /// </summary>
        /// <value>The minor.</value>
		public int Minor
        {
            get { return _minor; }
        }

        /// <summary>
        /// Gets the status of the given VersionNumber.
        /// </summary>
        /// <value>The status.</value>
        public VersionStatus Status
        {
            get { return _status; }
        }

        /// <summary>
        /// Gets a bool value that indicates if the represented version is a major version (eg. 2.0).
        /// </summary>
        /// <value>A bool value, true if the instance represents a major version (eg. 2.0), otherwise false.</value>
		public bool IsMajor
        {
            get { return _minor == 0; }
        }

        // =========================================================================== Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionNumber"/> class.
        /// </summary>
        /// <param name="major">The major verion number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="status">The version status.</param>
        public VersionNumber(int major, int minor, VersionStatus status)
        {
            if (major < 0 || minor < 0)
                throw new ArgumentException("Major or minor must be greater than or equal zero");

            _abstractVersion = AbstractVersion.Defined;
            _major = major;
            _minor = minor;
            _status = status;
        }

        public VersionNumber(int major, int minor) : this(major, minor, minor == 0 ? VersionStatus.Approved : VersionStatus.Draft) { }


        // =========================================================================== Operators

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(VersionNumber v1, VersionNumber v2)
        {
            if (object.ReferenceEquals(v1, null))
                return object.ReferenceEquals(v2, null);
            return v1.Equals(v2);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
		public static bool operator !=(VersionNumber v1, VersionNumber v2)
        {
            return !(v1 == v2);
        }
        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
		public static bool operator >(VersionNumber v1, VersionNumber v2)
        {
            return (v2 < v1);
        }
        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
		public static bool operator <(VersionNumber v1, VersionNumber v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            return (v1.CompareTo(v2) < 0);
        }
        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
		public static bool operator <=(VersionNumber v1, VersionNumber v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            return (v1.CompareTo(v2) <= 0);
        }
        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="v1">The VersionNumber (v1).</param>
        /// <param name="v2">The VersionNumber (v2).</param>
        /// <returns>The result of the operator.</returns>
		public static bool operator >=(VersionNumber v1, VersionNumber v2)
        {
            return (v2 <= v1);
        }

        /////=========================================================================== Methods

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            VersionNumber verNum = (VersionNumber)obj;
            if (verNum == null)
                return false;
            return (verNum._major == this._major) && (verNum._minor == this._minor);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {
            return _major.GetHashCode() | _minor.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
		public override string ToString()
        {
            return String.Concat("V", _major, ".", _minor, ".", _status.ToString()[0]);
        }

        public string ToDisplayText()
        {
            return $"{Major}.{Minor} {SR.GetStringOrDefault($"$Portal:{Status.ToString()}", "Portal", Status.ToString())}";
        }

        // --------------------------------------------------------------------------- IComparable Members

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than obj. Zero This instance is equal to obj. Greater than zero This instance is greater than obj.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">obj is not the same type as this instance. </exception>
        public int CompareTo(object obj)
        {
            VersionNumber x = (VersionNumber)obj;
            if (_major != x._major)
                return _major.CompareTo(x._major);
            if (_minor != x._minor)
                return _minor.CompareTo(x._minor);
            return _status.CompareTo(x._status);
        }

        // --------------------------------------------------------------------------- ICloneable Members

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public VersionNumber Clone()
        {
            return new VersionNumber(this.Major, this.Minor, this.Status);
        }
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
		object ICloneable.Clone()
        {
            return (object)this.Clone();
        }

        // --------------------------------------------------------------------------- Static Tools

        /// <summary>
        /// Parses the specified version string. Valid format is the following:
        /// Optional prefix: "V" or "v", major number, ".", minor number.
        /// For example: V1.0, 2.3, v12.3456
        /// </summary>
        /// <param name="versionStr">The version string.</param>
        /// <returns></returns>
        public static VersionNumber Parse(string versionStr)
        {
            if (versionStr == null)
                throw new ArgumentNullException("versionStr");
            string[] sa = versionStr.ToLower(CultureInfo.CurrentCulture).Replace("version", "").Replace("v", "").Split('.');
            if (sa.Length != 3)
                throw new ArgumentException(SR.GetString(SR.Exceptions.VersionNumber.InvalidVersionFormat), "versionStr");
            return new VersionNumber(Convert.ToInt32(sa[0], CultureInfo.CurrentCulture), Convert.ToInt32(sa[1], CultureInfo.CurrentCulture), GetVersionStatus(sa[2]));
        }
        /// <summary>
        /// Parses the specified version string. Valid format is the following:
        /// ((['V'|'v'])?[majornumber][.][minornumber]([.][*]+)?)|'lastmajor'|'lastminor'
        /// For example: V1.0, 2.3, v12.3456, lastmajor
        /// </summary>
        /// <param name="versionString">The version string</param>
        /// <param name="version">Parsed VersionNumber if conversion was successful.</param>
        /// <returns>True if versionString was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string versionString, out VersionNumber version)
        {
            version = null;
            var input = versionString.ToLower();
            if (input == "lastmajor")
            {
                version = VersionNumber.LastMajor;
                return true;
            }
            if (input == "lastminor")
            {
                version = VersionNumber.LastMinor;
                return true;
            }
            if (input[0] == 'v')
                input = input.Substring(1);
            var sa = input.Split('.');
            if (sa.Length < 2)
                return false;
            int major, minor;
            if (!int.TryParse(sa[0], out major))
                return false;
            if (!int.TryParse(sa[1], out minor))
                return false;
            if (sa.Length == 2)
            {
                version = new VersionNumber(major, minor);
                return true;
            }

            VersionStatus status;
            switch (sa[2])
            {
                case "a":
                case "approved": status = VersionStatus.Approved; break;
                case "l":
                case "locked": status = VersionStatus.Locked; break;
                case "d":
                case "draft": status = VersionStatus.Draft; break;
                case "r":
                case "rejected": status = VersionStatus.Rejected; break;
                case "p":
                case "pending": status = VersionStatus.Pending; break;
                default:
                    return false;
            }
            version = new VersionNumber(major, minor, status);
            return true;
        }

        public static VersionStatus GetVersionStatus(string statusString)
        {
            switch (statusString.ToLower())
            {
                case "p": return VersionStatus.Pending;
                case "d": return VersionStatus.Draft;
                case "l": return VersionStatus.Locked;
                case "a": return VersionStatus.Approved;
                case "r": return VersionStatus.Rejected;
            }
            throw new ApplicationException(SR.GetString(SR.Exceptions.VersionNumber.InvalidVersionStatus_1, statusString));
        }

        public VersionNumber ChangeStatus(VersionStatus versionStatus)
        {
            return new VersionNumber(this._major, this._minor, versionStatus);
        }

    }
}
