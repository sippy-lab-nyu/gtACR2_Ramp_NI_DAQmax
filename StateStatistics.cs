using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;

public struct EventDescriptor
{
    public StateId Id;
    public double Value;
}

public class StateDescriptor
{
    static readonly int IdLength = (int)StateId.Annotation;

    public readonly int Trial;
    public readonly double[] Statistics;

    public EventDescriptor[] Events;

    public StateDescriptor(int trial)
    {
        Trial = trial;
        Statistics = new double[IdLength];
    }

    public override string ToString()
    {
        return string.Format("Trial:{0}, Statistics:{1}", Trial, Statistics);
    }
}

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Combinator)]
public class StateStatistics
{
    static IObservable<StateInfo> StateInterval(IObservable<StateInfo> source)
    {
        return Observable.Create<StateInfo>(observer =>
        {
            var currentTrial = default(StateInfo);
            var currentState = default(StateInfo);
            var stateObserver = Observer.Create<StateInfo>(
                info =>
                {
                    if (currentState == null) currentState = info;
                    if (currentTrial == null || info.Trial != currentTrial.Trial)
                    {
                        currentTrial = info;
                    }

                    var output = new StateInfo();
                    output.Trial = currentState.Trial;
                    
                    if (info.Id < StateId.Annotation)
                    {
                        output.Id = currentState.Id;
                        output.ElapsedTime = info.ElapsedTime - currentState.ElapsedTime;
                        if (info.Id != currentState.Id)
                        {
                            observer.OnNext(output);
                            currentState = info;

                            output = new StateInfo();
                            output.Trial = info.Trial;
                            output.Id = info.Id;
                            output.ElapsedTime = 0;
                        }
                    }
                    else
                    {
                        output.Id = info.Id;
                        output.ElapsedTime = info.ElapsedTime - currentTrial.ElapsedTime;
                    }
                    observer.OnNext(output);
                },
                observer.OnError,
                observer.OnCompleted);
            return source.SubscribeSafe(stateObserver);
        });
    }

    public IObservable<StateDescriptor> Process(IObservable<StateInfo> source)
    {
        return Observable.Create<StateDescriptor>(observer =>
        {
            var currentTrial = default(StateDescriptor);
            var stateObserver = Observer.Create<StateInfo>(
                info =>
                {
                    if (currentTrial == null || currentTrial.Trial != info.Trial)
                    {
                        currentTrial = new StateDescriptor(info.Trial);
                    }

                    var output = new StateDescriptor(currentTrial.Trial);
                    if (info.Id < StateId.Annotation)
                    {
                        currentTrial.Statistics[(int)info.Id] = info.ElapsedTime;
                    }
                    else output.Events = new[]
                    {
                        new EventDescriptor { Id = info.Id, Value = info.ElapsedTime }
                    };
                    
                    for (int i = 0; i < output.Statistics.Length; i++)
                    {
                        output.Statistics[i] = currentTrial.Statistics[i];
                    }
                    observer.OnNext(output);
                },
                observer.OnError,
                observer.OnCompleted);
            return StateInterval(source).SubscribeSafe(stateObserver);
        });
    }
}
