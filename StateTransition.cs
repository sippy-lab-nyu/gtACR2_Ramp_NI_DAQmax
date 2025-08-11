using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

public class StateInfo
{
    public int Trial;
    public StateId Id;
    public double ElapsedTime;

    public StateInfo()
    {
    }

    public StateInfo(int trial, StateId id, double elapsedTime)
    {
        Trial = trial;
        Id = id;
        ElapsedTime = elapsedTime;
    }

    public override string ToString()
    {
        return string.Format(
            "Trial:{0}, Id: {1}, Elapsed:{2}",
            Trial,
            Id,
            ElapsedTime);
    }
}

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class StateTransition
{
    public int Trial { get; set; }

    public StateId Id { get; set; }

    public double ElapsedTime { get; set; }

    public IObservable<StateInfo> Process<TSource>(IObservable<TSource> source)
    {
        return source.Select(value => new StateInfo(Trial, Id, ElapsedTime));
    }
}
