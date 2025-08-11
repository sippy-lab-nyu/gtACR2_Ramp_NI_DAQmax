using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class IncrementalBuffer
{
    public IObservable<IList<ResponseId>> Process(IObservable<ResponseId> source)
    {
        return source.Scan(new List<ResponseId>(),(list,value) => 
        {
            var Output = new List<ResponseId>(list);
            Output.Add(value);
            return Output;
        });
    }
}
