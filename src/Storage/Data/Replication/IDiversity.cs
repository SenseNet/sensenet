using System;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public enum DiversityType { Constant, Sequence, Random, /* Dictionary, etc*/}

public class Sequence
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int Step { get; set; }
}
public class DateTimeSequence
{
    public DateTime MinValue { get; set; }
    public DateTime MaxValue { get; set; }
    public TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1);
}

public interface IDiversity
{
    DiversityType Type { get; set; }
    DataType DataType { get; }
    object Current { get; set; }
}
public interface IDiversity<T> : IDiversity
{
    new T Current { get; set; }
}
public abstract class Diversity<T> : IDiversity<T>
{
    public DiversityType Type { get; set; }
    public abstract DataType DataType { get; }
    public T Current { get; set; }

    object IDiversity.Current
    {
        get => Current;
        set => Current = (T)value;
    }
}
public class StringDiversity : Diversity<string>
{
    public override DataType DataType => DataType.String;
    public string Pattern { get; set; }
    public Sequence Sequence { get; set; }
}
public class IntDiversity : Diversity<int>
{
    public override DataType DataType => DataType.Int;
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int Step { get; set; }
}
public class DateTimeDiversity : Diversity<DateTime>
{
    public override DataType DataType => DataType.DateTime;
    public DateTimeSequence Sequence { get; set; }
}
