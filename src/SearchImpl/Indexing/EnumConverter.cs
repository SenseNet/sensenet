using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;
using LucField = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    public class EnumConverter
    {
        public static LucField.Index ToLuceneIndexingMode(IndexingMode mode)
        {
            if (mode == IndexingMode.Default)
                mode = PerFieldIndexingInfo.DefaultIndexingMode;

            switch (mode)
            {
                case IndexingMode.Analyzed: return LucField.Index.ANALYZED;
                case IndexingMode.AnalyzedNoNorms: return LucField.Index.ANALYZED_NO_NORMS;
                case IndexingMode.No: return LucField.Index.NO;
                case IndexingMode.NotAnalyzed: return LucField.Index.NOT_ANALYZED;
                case IndexingMode.NotAnalyzedNoNorms: return LucField.Index.NOT_ANALYZED_NO_NORMS;
                default: throw new SnNotSupportedException("Not supported IndexingMode: " + mode);
            }
        }

        public static LucField.Store ToLuceneIndexStoringMode(IndexStoringMode mode)
        {
            if (mode == IndexStoringMode.Default)
                mode = PerFieldIndexingInfo.DefaultIndexStoringMode;

            switch (mode)
            {
                case IndexStoringMode.No: return LucField.Store.NO;
                case IndexStoringMode.Yes: return LucField.Store.YES;
                default: throw new SnNotSupportedException("Not supported IndexStoringMode: " + mode);
            }
        }

        public static LucField.TermVector ToLuceneIndexTermVector(IndexTermVector mode)
        {
            if (mode == IndexTermVector.Default)
                mode = PerFieldIndexingInfo.DefaultTermVectorStoringMode;

            switch (mode)
            {
                case IndexTermVector.No: return LucField.TermVector.NO;
                case IndexTermVector.WithOffsets: return LucField.TermVector.WITH_OFFSETS;
                case IndexTermVector.WithPositions: return LucField.TermVector.WITH_POSITIONS;
                case IndexTermVector.WithPositionsOffsets: return LucField.TermVector.WITH_POSITIONS_OFFSETS;
                case IndexTermVector.Yes: return LucField.TermVector.YES;
                default: throw new SnNotSupportedException("Not supported IndexTermVector: " + mode);
            }
        }

    }
}
