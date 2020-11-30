namespace SenseNet.Configuration
{
    public class Identifiers
    {
        public const int AdministratorUserId = 1;
        public const int StartupUserId = 12;
        public const int SystemUserId = -1;
        public const int PortalRootId = 2;
        public const int PortalOrgUnitId = 5;
        public const int VisitorUserId = 6;
        public const int AdministratorsGroupId = 7;
        public const int EveryoneGroupId = 8;
        public const int OwnersGroupId = 9;
        public const string OperatorsGroupPath = "/Root/IMS/BuiltIn/Portal/Operators";
        public const string PublicAdminPath = "/Root/IMS/BuiltIn/Portal/PublicAdmin";
        public static readonly int SomebodyUserId = 10;

        public const string RootPath = "/Root";

        public const int MaximumPathLength = 450;

        public static string[] SpecialGroupPaths { get; internal set; } = {
            "/Root/IMS/BuiltIn/Portal/Everyone",
            "/Root/IMS/BuiltIn/Portal/Owners",
            "/Root/IMS/BuiltIn/Portal/LastModifiers"
        };
    }
}
