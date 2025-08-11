using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class TrialResponse : INamedElement
{
    public ResponseId Response { get; set; }

    string INamedElement.Name
    {
        get { return Response.ToString(); }
    }

    public IObservable<ResponseId> Process<TSource>(IObservable<TSource> source)
    {
        return source.Select(value => Response);
    }
}
