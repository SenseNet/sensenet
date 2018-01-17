using System;
using SenseNet.Search.Indexing;
using LucField = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Lucene29
{
    internal class EnumConverter
    {
        public static LucField.Index ToLuceneIndexingMode(IndexingMode mode)
        {
            if (mode == IndexingMode.Default)
                mode = IndexingInfo.DefaultIndexingMode;

            switch (mode)
            {
                case IndexingMode.Analyzed: return LucField.Index.ANALYZED;
                case IndexingMode.AnalyzedNoNorms: return LucField.Index.ANALYZED_NO_NORMS;
                case IndexingMode.No: return LucField.Index.NO;
                case IndexingMode.NotAnalyzed: return LucField.Index.NOT_ANALYZED;
                case IndexingMode.NotAnalyzedNoNorms: return LucField.Index.NOT_ANALYZED_NO_NORMS;
                default: throw new ArgumentOutOfRangeException("Not supported IndexingMode: " + mode);
            }
        }

        public static LucField.Store ToLuceneIndexStoringMode(IndexStoringMode mode)
        {
            if (mode == IndexStoringMode.Default)
                mode = IndexingInfo.DefaultIndexStoringMode;

            switch (mode)
            {
                case IndexStoringMode.No: return LucField.Store.NO;
                case IndexStoringMode.Yes: return LucField.Store.YES;
                default: throw new ArgumentOutOfRangeException("Not supported IndexStoringMode: " + mode);
            }
        }

        public static LucField.TermVector ToLuceneIndexTermVector(IndexTermVector mode)
        {
            if (mode == IndexTermVector.Default)
                mode = IndexingInfo.DefaultTermVectorStoringMode;

            switch (mode)
            {
                case IndexTermVector.No: return LucField.TermVector.NO;
                case IndexTermVector.WithOffsets: return LucField.TermVector.WITH_OFFSETS;
                case IndexTermVector.WithPositions: return LucField.TermVector.WITH_POSITIONS;
                case IndexTermVector.WithPositionsOffsets: return LucField.TermVector.WITH_POSITIONS_OFFSETS;
                case IndexTermVector.Yes: return LucField.TermVector.YES;
                default: throw new ArgumentOutOfRangeException("Not supported IndexTermVector: " + mode);
            }
        }
    }
}
