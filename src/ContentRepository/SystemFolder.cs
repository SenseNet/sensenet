﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler class that can contain child Contents, usually technical or hidden items.
    /// The instance of this type and all Content under its subree are considered system content.
    /// </summary>
    [ContentHandler]
    public class SystemFolder : Folder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public SystemFolder(Node parent) : this(parent, null) { Initialize(); }
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public SystemFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { Initialize(); }
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFolder"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected SystemFolder(NodeToken nt) : base(nt) { }

        /// <summary>
        /// Sets IsSystem property to true.
        /// Initializes default field values in case of a new instance that is not yet saved to the database.
        /// If this method is overridden in a derived class, the base.Initialize must be called.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            this.IsSystem = true;
        }

        /// <summary>
        /// Returns the nearest Content in the parent chain that is an instance of or 
        /// derived from the <see cref="SystemFolder"/> type.
        /// If there is no such Content, returns null.
        /// </summary>
        /// <param name="child">The <see cref="Node"/> to search an ancestor for.</param>
        /// <returns>An instance of <see cref="GenericContent"/> or null.</returns>
        public static GenericContent GetSystemContext(Node child)
        {
            SystemFolder ancestor = null;

            while ((child != null) && ((ancestor = child as SystemFolder) == null))
                child = child.Parent;

            return (ancestor != null) ? ancestor.Parent as GenericContent : null;
        }
    }
}
