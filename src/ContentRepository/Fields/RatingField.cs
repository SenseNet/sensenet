using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    /// <summary>
    /// Field for storing rating information about content.
    /// </summary>
    [ShortName("Rating")]
    [DataSlot(0, RepositoryDataType.String, typeof (string))]
    [DataSlot(1, RepositoryDataType.Int, typeof (int))]
    [DataSlot(2, RepositoryDataType.Currency, typeof (decimal))]
    [DefaultFieldSetting(typeof (RatingFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.Rating")]
    [FieldDataType(typeof (VoteData))]
    public class RatingField : Field
    {
        /// <summary>
        /// Converts string list to object array to store data in Content Repository.
        /// </summary>
        /// <param name="value">String list containing average rating and numbers of ratings</param>
        /// <returns>String array with a single string containing average rating and numbers of rating separated by pipe character.</returns>
        protected override object[] ConvertFrom(object value)
        {
            var fs = FieldSetting as RatingFieldSetting;

            if (fs == null)
                throw new NotSupportedException("Invalid field setting.");

            if (fs.Range <= 0)
                throw new NotSupportedException("Range is invalid, must be greater than 0.");

            var voteData = value as VoteData;
            if (voteData == null)
                throw new InvalidCastException("Unable to cast the given value to VoteData.");

            var originalValueData = OriginalValue as VoteData;
            if (originalValueData == null) throw new NotSupportedException("Invalid items in the field.");

            // If there was no stored rating or it is invalid, reset votes
            if ((originalValueData.MaxVotes != fs.Range*fs.Split || originalValueData.Split != fs.Split) || voteData.SelectedValue == -1)
            {
                originalValueData = VoteData.CreateVoteData(fs.Range, fs.Split);
            }
            // Adding vote if present
            if (voteData.SelectedValue.HasValue && voteData.SelectedValue.Value > 0)
            {
                if (voteData.OldValue.HasValue && voteData.OldValue.Value > 0)
                    originalValueData.RemoveVote(voteData.OldValue.Value);

                originalValueData.AddVote(voteData.SelectedValue.Value);
            }

            return new object[] {originalValueData.Serialize(), originalValueData.SumVotes, originalValueData.AverageRate};
        }

        /// <summary>
        /// Converts object array containing data from Content Repository to string list.
        /// </summary>
        /// <param name="handlerValues">String array with a single string containing average rating and numbers of rating separated by pipe character.</param>
        /// <returns>String list containing average rating and numbers of ratings</returns>
        protected override object ConvertTo(object[] handlerValues)
        {
            var fs = FieldSetting as RatingFieldSetting;
            if (fs == null)
                throw new NotSupportedException("Invalid field setting.");

            var items = handlerValues[0] as string;
            VoteData data = string.IsNullOrEmpty(items)
                                ? VoteData.CreateVoteData(fs.Range, fs.Split)
                                : VoteData.CreateVoteData(items);

            return data;
        }

        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }

        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteString(GetXmlData());
        }

        protected override string GetXmlData()
        {
            var list = GetData() as VoteData;
            return list == null ? string.Empty : list.Serialize();
        }

        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            var text = fieldNode.InnerText;
            VoteData vd = null;

            var fs = FieldSetting as RatingFieldSetting;
            if (fs == null)
                throw new NotSupportedException("Invalid field setting.");
            if (fs.Range <= 0)
                throw new NotSupportedException("Range is invalid, must be greater than 0.");

            vd = string.IsNullOrEmpty(text) ? VoteData.CreateVoteData(fs.Range, fs.Split) : VoteData.CreateVoteData(text);
            if (vd.MaxVotes != fs.Range*fs.Split || vd.Split != fs.Split)
                vd = VoteData.CreateVoteData(fs.Range, fs.Split);

            this.SetData(vd);
        }
    }

    public class VoteData
    {
        public bool Success = false;
        private bool _enableGrouping;

        /// <summary>
        /// Constructor for Serialization, do not DELETE 
        /// </summary>
        private VoteData()
        {
        }

        private VoteData(List<int> initValue, int split)
        {
            PercentageVotes = new List<decimal>();
            HoverPanelData = new List<HoverPanelDataItem>();
            CountVotes = initValue;
            Split = split < 1 ? 1 : split;
            MaxVotes = CountVotes.Count;

            CalculateData();
        }

        public int Split { get; set; }
        public int MaxVotes { get; set; }

        public decimal AverageRate { get; set; }

        public int SumVotes { get; set; }

        public List<decimal> PercentageVotes { get; set; }

        public List<int> CountVotes { get; set; }

        public List<HoverPanelDataItem> HoverPanelData { get; set; }

        public bool EnableGrouping
        {
            get { return _enableGrouping; }
            set
            {
                _enableGrouping = value;
                CalculateData();
            }
        }

        public string ErrorMessage { get; set; }

        public int? SelectedValue { get; set; }

        // for revoting
        public int? OldValue { get; set; }

        private void CalculateData()
        {
            SumVotes = 0;
            decimal sumVote = 0;
            for (int i = 0; i < CountVotes.Count; i++)
            {
                sumVote += ((decimal) CountVotes[i]*(i + 1)/Split);
                SumVotes += CountVotes[i];
            }

            AverageRate = SumVotes == 0
                              ? 0
                              : Math.Round(sumVote/SumVotes, 2);

            PercentageVotes.Clear();
            foreach (var i in CountVotes)
            {
                PercentageVotes.Add(sumVote == 0
                                        ? 0
                                        : Math.Round(((decimal) i/SumVotes)*100, 2));
            }

            HoverPanelData.Clear();
            if (EnableGrouping)
            {
                var actualIdx = 0;
                var actualGroup = 0;
                var sumvote = 0;
                while ((actualGroup*Split) + actualIdx < CountVotes.Count)
                {
                    sumvote += CountVotes[(actualGroup*Split) + actualIdx++];
                    if (actualIdx == Split)
                    {
                        actualGroup++;
                        actualIdx = 0;

                        HoverPanelData.Add(new HoverPanelDataItem
                                               {
                                                   Index = actualGroup,
                                                   Value = SumVotes > 0
                                                               ? Math.Round(((decimal) sumvote/SumVotes)*100, 2)
                                                               : 0
                                               });

                        sumvote = 0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < PercentageVotes.Count; i++)
                {
                    HoverPanelData.Add(new HoverPanelDataItem
                                           {
                                               Index = i + 1,
                                               Value = PercentageVotes[i]
                                           });
                }
            }
        }

        public void AddVote(int position)
        {
            CountVotes[position - 1] += 1;
            CalculateData();
        }

        public void RemoveVote(int position)
        {
            CountVotes[position - 1] -= 1;
            CalculateData();
        }

        public string Serialize()
        {
            return AverageRate + "|" + Split + "|" + string.Join("|", CountVotes.ConvertAll(i => i.ToString()).ToArray());
        }

        public static VoteData CreateVoteData(string values)
        {
            var stringItemList = values.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            stringItemList.RemoveAt(0);
            var split = Convert.ToInt32(stringItemList[0]);
            stringItemList.RemoveAt(0);
            var data = new VoteData(stringItemList.ConvertAll(s => Convert.ToInt32(s)), split);
            return data;
        }

        public static VoteData CreateVoteData(int maxValue, int split)
        {
            var list = new List<int>(maxValue*split);
            for (int i = 0; i < maxValue*split; i++)
            {
                list.Add(0);
            }
            var data = new VoteData(list, split);
            return data;
        }

        #region Nested type: HoverPanelDataItem

        [Serializable]
        public struct HoverPanelDataItem
        {
            public int Index { get; set; }
            public decimal Value { get; set; }
        }

        #endregion
    }
}