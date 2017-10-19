using System;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a node either by its id or its path.
    /// </summary>
    public class NodeIdentifier
    {
        /// <summary>
        /// Node id.
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Node path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Initializes a new instance of the NodeIdentifier class. Do not use this directly, use the Get method instead.
        /// </summary>
        private NodeIdentifier()
        {
        }

        /// <summary>
        /// Gets a node identifier from the provided node.
        /// </summary>
        /// <param name="node">An existing Node.</param>
        public static NodeIdentifier Get(Node node)
        {
            if (node == null)
                return null;

            return new NodeIdentifier{Id = node.Id, Path = node.Path};
        }

        /// <summary>
            /// Gets a new node identifier based on the provided value.
            /// </summary>
            /// <param name="identifier">Can be a path or an id.</param>
            public static NodeIdentifier Get(object identifier)
        {
            if (identifier == null)
                return null;

            var nid = new NodeIdentifier();

            var idAsText = identifier as string;
            if (idAsText != null)
            {
                // We received a string, that can be a path or an id as well.
                int id;

                if (RepositoryPath.IsValidPath(idAsText) == RepositoryPath.PathResult.Correct)
                    nid.Path = idAsText;
                else if (int.TryParse(idAsText, out id))
                    nid.Id = id;
                else
                    throw new SnNotSupportedException("An identifier should be either a path or an id. Invalid value: " + idAsText);
            }
            else
            {
                if (!(identifier is int || identifier is short || identifier is long))
                    throw new SnNotSupportedException("An identifier should be either a path or an id. Invalid value: " + identifier);

                nid.Id = Convert.ToInt32(identifier);
            }

            return nid;
        }
    }
}
