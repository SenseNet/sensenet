using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public enum DiversityType { Constant, Sequence, Random, /* Dictionary, etc*/}

public class Sequence
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
public class DateTimeSequence
{
    // How many seconds is int.max: 2147483647 / 60 / 60 / 24 / 365 = 68.09625973490614
    public DateTime MinValue { get; set; }
    public DateTime MaxValue { get; set; }
    public TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1);
}

public interface IDiversity
{
    DiversityType Type { get; set; }
    Type DataType { get; set; }
    object Current { get; set; }
}
public interface IDiversity<T> : IDiversity
{
    new T Current { get; set; }
}
public abstract class Diversity<T> : IDiversity<T>
{
    public DiversityType Type { get; set; }
    public Type DataType { get; set; }
    public T Current { get; set; }

    object IDiversity.Current
    {
        get => Current;
        set => Current = (T)value;
    }
}
public class StringDiversity : Diversity<string>
{
    public string Pattern { get; set; }
    public Sequence Sequence { get; set; }
}
public class IntDiversity : Diversity<int>
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
public class ReferenceIdDiversity : IntDiversity
{
}
public class DateTimeDiversity : Diversity<DateTime>
{
    public DateTimeSequence Sequence { get; set; }
}
