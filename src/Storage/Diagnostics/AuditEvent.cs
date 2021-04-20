namespace SenseNet.Diagnostics
{
    public class AuditEvent : IAuditEvent
    {
        public string AuditCategory { get; private set; }
        public int EventId { get; private set; }
        public string Message { get; private set; }
        public string Title { get; private set; }

        public static readonly AuditEvent LoginTry = new AuditEvent("LoginTry", 11000);
        public static readonly AuditEvent LoginSuccessful = new AuditEvent("LoginSuccessful", 11001);
        public static readonly AuditEvent LoginUnsuccessful = new AuditEvent("LoginUnsuccessful", 11002);
        public static readonly AuditEvent Logout = new AuditEvent("Logout", 11003);
        public static readonly AuditEvent ContentCreated = new AuditEvent("ContentCreated", 11004);
        public static readonly AuditEvent ContentUpdated = new AuditEvent("ContentUpdated", 11005);
        public static readonly AuditEvent ContentDeleted = new AuditEvent("ContentDeleted", 11006);
        public static readonly AuditEvent VersionChanged = new AuditEvent("VersionChanged", 11007);
        public static readonly AuditEvent PermissionChanged = new AuditEvent("PermissionChanged", 11008);
        public static readonly AuditEvent LockTakenOver = new AuditEvent("LockTakenOver", 11009);
        public static readonly AuditEvent ContentMoved = new AuditEvent("ContentMoved", 11010);
        public static readonly AuditEvent ContentCopied = new AuditEvent("ContentCopied", 11011);
        public static readonly AuditEvent ContentRestored = new AuditEvent("ContentRestored", 11012);

        public AuditEvent(string auditCategory, int eventId)
            : this(auditCategory, eventId, auditCategory, auditCategory)
        {
        }

        public AuditEvent(string auditCategory, int eventId, string title, string message)
        {
            AuditCategory = auditCategory;
            EventId = eventId;
            Title = title;
            Message = message;
        }

        public override string ToString()
        {
            return AuditCategory;
        }
    }

}
