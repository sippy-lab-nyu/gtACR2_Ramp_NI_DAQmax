using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Disposables;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Combinator)]
public class SampleState
{
    public IObservable<StateInfo> Process(IObservable<StateInfo> source)
    {
        return Observable.Create<StateInfo>(observer =>
        {
            var currentState = default(StateInfo);
            var baseTime = HighResolutionScheduler.Now;
            var synchronized = Observer.Create<StateInfo>(
                state =>
                {
                    if (state != null)
                    {
                        observer.OnNext(state);
                        if (state.Id < StateId.Annotation)
                        {
                            baseTime = HighResolutionScheduler.Now;
                            currentState = state;
                        }
                    }
                    else
                    {
                        state = currentState;
                        if (state != null)
                        {
                            var deltaTime = HighResolutionScheduler.Now - baseTime;
                            var output = new StateInfo();
                            output.Id = state.Id;
                            output.Trial = state.Trial;
                            output.ElapsedTime = state.ElapsedTime + deltaTime.TotalSeconds;
                            observer.OnNext(output);
                        }
                    }
                },
                observer.OnError,
                observer.OnCompleted);

            var sampleTimer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
            return source.Merge(sampleTimer.Select(tick => default(StateInfo))).SubscribeSafe(synchronized);
        });
    }
}
