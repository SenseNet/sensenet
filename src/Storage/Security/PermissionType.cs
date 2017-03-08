using SenseNet.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Security
{
    [DebuggerDisplay("{Index}:{Name}:'{Mask}'")]
    public class PermissionType : PermissionTypeBase
    {
        private PermissionType(string name, int index) : base(name, index) { }

        /// <summary>Index = 0</summary>
        public static readonly PermissionType See;
        /// <summary>Index = 1</summary>
        public static readonly PermissionType Preview;
        /// <summary>Index = 2</summary>
        public static readonly PermissionType PreviewWithoutWatermark;
        /// <summary>Index = 3</summary>
        public static readonly PermissionType PreviewWithoutRedaction;
        /// <summary>Index = 4</summary>
        public static readonly PermissionType Open;
        /// <summary>Index = 5</summary>
        public static readonly PermissionType OpenMinor;
        /// <summary>Index = 6</summary>
        public static readonly PermissionType Save;
        /// <summary>Index = 7</summary>
        public static readonly PermissionType Publish;
        /// <summary>Index = 8</summary>
        public static readonly PermissionType ForceCheckin;
        /// <summary>Index = 9</summary>
        public static readonly PermissionType AddNew;
        /// <summary>Index = 10</summary>
        public static readonly PermissionType Approve;
        /// <summary>Index = 11</summary>
        public static readonly PermissionType Delete;
        /// <summary>Index = 12</summary>
        public static readonly PermissionType RecallOldVersion;
        /// <summary>Index = 13</summary>
        public static readonly PermissionType DeleteOldVersion;
        /// <summary>Index = 14</summary>
        public static readonly PermissionType SeePermissions;
        /// <summary>Index = 15</summary>
        public static readonly PermissionType SetPermissions;
        /// <summary>Index = 16</summary>
        public static readonly PermissionType RunApplication;
        /// <summary>Index = 17</summary>
        public static readonly PermissionType ManageListsAndWorkspaces;
        /// <summary>Index = 18</summary>
        public static readonly PermissionType TakeOwnership;

        // Unused but reserved slots in reverse name order.
        // DO NOT USE the "UnusedXX" permissions in application level code because this set is part of the system permission set
        //   and it can be changed at any time.
        // The "Unused01" will be the last item when we expand the system permissions...

        /// <summary>Index = 19</summary>
        public static readonly PermissionType Unused13;
        /// <summary>Index = 20</summary>
        public static readonly PermissionType Unused12;
        /// <summary>Index = 21</summary>
        public static readonly PermissionType Unused11;
        /// <summary>Index = 22</summary>
        public static readonly PermissionType Unused10;
        /// <summary>Index = 23</summary>
        public static readonly PermissionType Unused09;
        /// <summary>Index = 24</summary>
        public static readonly PermissionType Unused08;
        /// <summary>Index = 25</summary>
        public static readonly PermissionType Unused07;
        /// <summary>Index = 26</summary>
        public static readonly PermissionType Unused06;
        /// <summary>Index = 27</summary>
        public static readonly PermissionType Unused05;
        /// <summary>Index = 28</summary>
        public static readonly PermissionType Unused04;
        /// <summary>Index = 29</summary>
        public static readonly PermissionType Unused03;
        /// <summary>Index = 30</summary>
        public static readonly PermissionType Unused02;
        /// <summary>Index = 31</summary>
        public static readonly PermissionType Unused01;

        /// <summary>Index = 32</summary>
        public static readonly PermissionType Custom01;
        /// <summary>Index = 33</summary>
        public static readonly PermissionType Custom02;
        /// <summary>Index = 34</summary>
        public static readonly PermissionType Custom03;
        /// <summary>Index = 35</summary>
        public static readonly PermissionType Custom04;
        /// <summary>Index = 36</summary>
        public static readonly PermissionType Custom05;
        /// <summary>Index = 37</summary>
        public static readonly PermissionType Custom06;
        /// <summary>Index = 38</summary>
        public static readonly PermissionType Custom07;
        /// <summary>Index = 39</summary>
        public static readonly PermissionType Custom08;
        /// <summary>Index = 40</summary>
        public static readonly PermissionType Custom09;
        /// <summary>Index = 41</summary>
        public static readonly PermissionType Custom10;
        /// <summary>Index = 42</summary>
        public static readonly PermissionType Custom11;
        /// <summary>Index = 43</summary>
        public static readonly PermissionType Custom12;
        /// <summary>Index = 44</summary>
        public static readonly PermissionType Custom13;
        /// <summary>Index = 45</summary>
        public static readonly PermissionType Custom14;
        /// <summary>Index = 46</summary>
        public static readonly PermissionType Custom15;
        /// <summary>Index = 47</summary>
        public static readonly PermissionType Custom16;
        /// <summary>Index = 48</summary>
        public static readonly PermissionType Custom17;
        /// <summary>Index = 49</summary>
        public static readonly PermissionType Custom18;
        /// <summary>Index = 50</summary>
        public static readonly PermissionType Custom19;
        /// <summary>Index = 51</summary>
        public static readonly PermissionType Custom20;
        /// <summary>Index = 52</summary>
        public static readonly PermissionType Custom21;
        /// <summary>Index = 53</summary>
        public static readonly PermissionType Custom22;
        /// <summary>Index = 54</summary>
        public static readonly PermissionType Custom23;
        /// <summary>Index = 55</summary>
        public static readonly PermissionType Custom24;
        /// <summary>Index = 56</summary>
        public static readonly PermissionType Custom25;
        /// <summary>Index = 57</summary>
        public static readonly PermissionType Custom26;
        /// <summary>Index = 58</summary>
        public static readonly PermissionType Custom27;
        /// <summary>Index = 59</summary>
        public static readonly PermissionType Custom28;
        /// <summary>Index = 60</summary>
        public static readonly PermissionType Custom29;
        /// <summary>Index = 61</summary>
        public static readonly PermissionType Custom30;
        /// <summary>Index = 62</summary>
        public static readonly PermissionType Custom31;
        /// <summary>Index = 63</summary>
        public static readonly PermissionType Custom32;

        private static readonly PermissionType[] _permissionTypes;

        public static PermissionType[] PermissionTypes { get { return _permissionTypes.ToArray(); } }

        private static readonly PermissionType[] _builtinPermissionTypes;

        public static PermissionType[] BuiltInPermissionTypes { get { return _builtinPermissionTypes.ToArray(); } }

        public static PermissionType GetByIndex(int index)
        {
            return (PermissionType)GetPermissionTypeByIndex(index);
        }
        public static PermissionType GetByName(string name)
        {
            return (PermissionType)GetPermissionTypeByName(name);
        }

        internal static PermissionType[] GetByMask(ulong mask)
        {
            return PermissionTypes.Where(p => (~p.Mask & mask) != 0).ToArray();
        }

        static PermissionType()
        {
            See = new PermissionType("See", 0);
            Preview = new PermissionType("Preview", 1) { Allows = new[] { See } };
            PreviewWithoutWatermark = new PermissionType("PreviewWithoutWatermark", 2) { Allows = new[] { Preview } };
            PreviewWithoutRedaction = new PermissionType("PreviewWithoutRedaction", 3) { Allows = new[] { Preview } };
            Open = new PermissionType("Open", 4) { Allows = new[] { PreviewWithoutWatermark, PreviewWithoutRedaction } };
            OpenMinor = new PermissionType("OpenMinor", 5) { Allows = new[] { Open } };
            Save = new PermissionType("Save", 6) { Allows = new[] { OpenMinor } };
            Publish = new PermissionType("Publish", 7) { Allows = new[] { OpenMinor } };
            ForceCheckin = new PermissionType("ForceCheckin", 8) { Allows = new[] { OpenMinor } };
            AddNew = new PermissionType("AddNew", 9) { Allows = new[] { OpenMinor } };
            Approve = new PermissionType("Approve", 10) { Allows = new[] { OpenMinor } };
            Delete = new PermissionType("Delete", 11) { Allows = new[] { OpenMinor } };
            RecallOldVersion = new PermissionType("RecallOldVersion", 12) { Allows = new[] { OpenMinor } };
            DeleteOldVersion = new PermissionType("DeleteOldVersion", 13) { Allows = new[] { OpenMinor } };
            SeePermissions = new PermissionType("SeePermissions", 14);
            SetPermissions = new PermissionType("SetPermissions", 15) { Allows = new[] { SeePermissions } };
            RunApplication = new PermissionType("RunApplication", 16);
            ManageListsAndWorkspaces = new PermissionType("ManageListsAndWorkspaces", 17) { Allows = new[] { Save, AddNew, DeleteOldVersion } };
            TakeOwnership = new PermissionType("TakeOwnership", 18) { Allows = new[] { See } };
            
            Unused13 = new PermissionType("Unused13", 19);
            Unused12 = new PermissionType("Unused12", 20);
            Unused11 = new PermissionType("Unused11", 21);
            Unused10 = new PermissionType("Unused10", 22);
            Unused09 = new PermissionType("Unused09", 23);
            Unused08 = new PermissionType("Unused08", 24);
            Unused07 = new PermissionType("Unused07", 25);
            Unused06 = new PermissionType("Unused06", 26);
            Unused05 = new PermissionType("Unused05", 27);
            Unused04 = new PermissionType("Unused04", 28);
            Unused03 = new PermissionType("Unused03", 29);
            Unused02 = new PermissionType("Unused02", 30);
            Unused01 = new PermissionType("Unused01", 31);

            Custom01 = new PermissionType("Custom01", 32);
            Custom02 = new PermissionType("Custom02", 33);
            Custom03 = new PermissionType("Custom03", 34);
            Custom04 = new PermissionType("Custom04", 35);
            Custom05 = new PermissionType("Custom05", 36);
            Custom06 = new PermissionType("Custom06", 37);
            Custom07 = new PermissionType("Custom07", 38);
            Custom08 = new PermissionType("Custom08", 39);
            Custom09 = new PermissionType("Custom09", 40);
            Custom10 = new PermissionType("Custom10", 41);
            Custom11 = new PermissionType("Custom11", 42);
            Custom12 = new PermissionType("Custom12", 43);
            Custom13 = new PermissionType("Custom13", 44);
            Custom14 = new PermissionType("Custom14", 45);
            Custom15 = new PermissionType("Custom15", 46);
            Custom16 = new PermissionType("Custom16", 47);
            Custom17 = new PermissionType("Custom17", 48);
            Custom18 = new PermissionType("Custom18", 49);
            Custom19 = new PermissionType("Custom19", 50);
            Custom20 = new PermissionType("Custom20", 51);
            Custom21 = new PermissionType("Custom21", 52);
            Custom22 = new PermissionType("Custom22", 53);
            Custom23 = new PermissionType("Custom23", 54);
            Custom24 = new PermissionType("Custom24", 55);
            Custom25 = new PermissionType("Custom25", 56);
            Custom26 = new PermissionType("Custom26", 57);
            Custom27 = new PermissionType("Custom27", 58);
            Custom28 = new PermissionType("Custom28", 59);
            Custom29 = new PermissionType("Custom29", 60);
            Custom30 = new PermissionType("Custom30", 61);
            Custom31 = new PermissionType("Custom31", 62);
            Custom32 = new PermissionType("Custom32", 63);

            _permissionTypes = new PermissionType[] {
                See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, Open, OpenMinor, Save, Publish, ForceCheckin, AddNew, Approve, Delete, 
                RecallOldVersion, DeleteOldVersion, SeePermissions, SetPermissions, RunApplication, ManageListsAndWorkspaces, 
                TakeOwnership, Unused13, Unused12, Unused11, Unused10, Unused09, Unused08, Unused07, Unused06, Unused05, Unused04, Unused03, Unused02,Unused01, 
                Custom01, Custom02, Custom03, Custom04, Custom05, Custom06, Custom07, Custom08, Custom09, Custom10, Custom11, Custom12, Custom13, Custom14, Custom15, Custom16, 
                Custom17, Custom18, Custom19, Custom20, Custom21, Custom22, Custom23, Custom24, Custom25, Custom26, Custom27, Custom28, Custom29, Custom30, Custom31, Custom32
            };

            _builtinPermissionTypes = new PermissionType[] {
                See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, Open, OpenMinor, Save, Publish, ForceCheckin, AddNew, Approve, Delete, 
                RecallOldVersion, DeleteOldVersion, SeePermissions, SetPermissions, RunApplication, ManageListsAndWorkspaces, TakeOwnership
            };
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
