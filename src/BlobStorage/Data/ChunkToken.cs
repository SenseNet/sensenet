﻿using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Contains information that identifies a certain binary value when uploading binary chunks
    /// or accessing the binary using the blob storage provider in an external tool.
    /// </summary>
    [DebuggerDisplay("vid:{VersionId}|ptid:{PropertyTypeId}|bid:{BinaryPropertyId}|fid:{FileId}")]
    public class ChunkToken
    {
        /// <summary>
        /// Binary property id in the metadata database.
        /// </summary>
        public int BinaryPropertyId { get; set; }
        /// <summary>
        /// File id in the meadata database.
        /// </summary>
        public int FileId { get; set; }
        /// <summary>
        /// Content version id.
        /// </summary>
        public int VersionId { get; set; }
        /// <summary>
        /// Binary property type id.
        /// </summary>
        public int PropertyTypeId { get; set; }

        /// <summary>
        /// Generates a token using the provided ids. The algorithm is encapsulated here and can be changed at
        /// any time. Only the Parse method below can be used to extract the values. The layers above should
        /// NOT parse and use the ids compiled into this token.
        /// </summary>
        public string GetToken()
        {
            return $"{this.VersionId}|{this.PropertyTypeId}|{this.BinaryPropertyId}|{this.FileId}";
        }

        /// <summary>
        /// Extracts the values stored in the token generated by the GetChunkToken method. This class
        /// is the only component that should know about the algorithm that builds the token.
        /// </summary>
        /// <param name="token">String data that will be parsed to ChunkToken instance.</param>
        /// <param name="versionId">Checks version id equality if the parameter value is greater than 0.
        /// Throws a DataException if the version ids are different.</param>
        /// <returns></returns>
        internal static ChunkToken Parse(string token, int versionId = 0)
        {
            var ids = token.Split('|').Select(int.Parse).ToArray();

            var tokenData = new ChunkToken
            {
                VersionId = ids[0],
                PropertyTypeId = ids[1],
                BinaryPropertyId = ids[2],
                FileId = ids[3]
            };

            if(versionId>0 && tokenData.VersionId != versionId)
                throw new DataException("Version id and chunk token mismatch.");

            return tokenData;
        }

        /// <summary>
        /// Converts this token to its string representation. The Parse method is able to work with the result of this method.
        /// </summary>
        public override string ToString()
        {
            return GetToken();
        }
    }
}