using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationDescriptor
{
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
    public Dictionary<string, string> DiversityControl { get; set; }
    [JsonIgnore]
    public IDictionary<string, IDiversity> Diversity { get; set; }

    private char[] _whitespaces = " \t\r\n".ToCharArray();
    public void Initialize()
    {
        if (CountMax < 1)
            CountMax = 10;
        if (MaxItemsPerFolder < 1)
            MaxItemsPerFolder = 100;
        if (MaxFoldersPerFolder < 1)
            MaxFoldersPerFolder = 100;

        var diversity = new Dictionary<string, IDiversity>();
        Diversity = diversity;
        if (DiversityControl == null)
            return;

        foreach (var item in DiversityControl)
        {
            var name = item.Key;
            var src = item.Value;

            if (name == "Index")
            {
                var type = DiversityType.Constant;
                var min = 0;
                var max = 0;
                var words = src.Split(_whitespaces, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 1)
                {
                    if (!int.TryParse(words[0], out min))
                        throw new ArgumentException($"Invalid constant value in the IntDiversity of the '{name}' field: '{src}'");
                }
                else
                {
                    if (words.Length == 4)
                    {
                        if (words[3].Equals("RANDOM", StringComparison.InvariantCultureIgnoreCase))
                            type = DiversityType.Random;
                        else if (words[3].Equals("SEQUENCE", StringComparison.InvariantCultureIgnoreCase))
                            type = DiversityType.Sequence;
                        else
                            throw new ArgumentException($"Invalid qualifier in the IntDiversity of the '{name}' field: '{src}'. Expected last word: 'Random' or 'Sequence'");
                    }
                    if (!words[1].Equals("TO", StringComparison.InvariantCultureIgnoreCase))
                        throw new ArgumentException($"Invalid range definition in the IntDiversity of the '{name}' field: '{src}'. Expected format: <min> TO <max> RANDOM|SEQUENCE");
                    if (!int.TryParse(words[0], out min))
                        throw new ArgumentException($"Invalid minimum value in the IntDiversity of the '{name}' field: '{src}'");
                    if (!int.TryParse(words[2], out max))
                        throw new ArgumentException($"Invalid maximum value in the IntDiversity of the '{name}' field: '{src}'");
                }

                diversity.Add(name, new IntDiversity {Type = type, MinValue = min, MaxValue = max});
            }
            else
                throw new NotImplementedException();
        }

    }
}
