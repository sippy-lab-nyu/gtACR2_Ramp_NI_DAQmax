using Bonsai;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Combinator)]
public class DistinctUntilStateChanged
{
    public IObservable<StateInfo> Process(IObservable<StateInfo> source)
    {
        return Observable.Create<StateInfo>(observer =>
        {
            var currentState = default(StateId?);
            var stateObserver = Observer.Create<StateInfo>(
                info =>
                {
                    if (info.Id < StateId.Annotation)
                    {
                        if (info.Id != currentState) observer.OnNext(info);
                        currentState = info.Id;
                    }
                    else observer.OnNext(info);
                },
                observer.OnError,
                observer.OnCompleted);
            return source.SubscribeSafe(stateObserver);
        });
    }
}