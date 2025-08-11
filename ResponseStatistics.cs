using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

public struct ResponseDescriptor
{
    public int Epoch;
    public int Hits;
    public int Misses;
    public int FalseAlarms;
    public int CorrectRejections;
    public int PullPenalty;
    public int EarlyResponse;

    public int TotalHits;
    public int TotalMisses;
    public int TotalFalseAlarms;

    public int TotalGoTrials;

    public int TotalNoGoTrials;

}

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Combinator)]
public class ResponseStatistics
{
    void UpdateSlidingStatistics(ref ResponseDescriptor stats, ResponseId response)
    {
        switch (response)
        {
            case ResponseId.Hit: stats.Hits++; break;
            case ResponseId.Miss: stats.Misses++; break;
            case ResponseId.PullPenalty: stats.Misses++; break;
            case ResponseId.FalseAlarm: stats.FalseAlarms++; break;
            case ResponseId.CorrectRejection: stats.CorrectRejections++; break;
        }
    }
    void UpdateTotalStatistics(ref ResponseDescriptor stats, ResponseId response)
    {
        switch (response)
        {
            case ResponseId.Hit: stats.TotalGoTrials++; stats.TotalHits++; break;
            case ResponseId.Miss: stats.TotalMisses++; stats.TotalGoTrials++; break;
            case ResponseId.PullPenalty: stats.TotalGoTrials++; break;
            case ResponseId.FalseAlarm: stats.TotalFalseAlarms++; stats.TotalNoGoTrials++; break;
            case ResponseId.CorrectRejection: stats.TotalNoGoTrials++; break;
        }
    }
    public IObservable<ResponseDescriptor> Process(IObservable<ResponseId> source)
    {
        return source.Scan(new ResponseDescriptor(), (stats, response) =>
        {
            stats.Epoch++;
            UpdateSlidingStatistics(ref stats, response);
            return stats;
        });
        
    }

    public IObservable<ResponseDescriptor> Process(IObservable<IList<ResponseId>> source)
    {
        return source.Scan(new ResponseDescriptor(), (stats, responses) =>
        {
            stats.Epoch++;
            stats.Hits = 0; 
            stats.Misses = 0;
            stats.FalseAlarms = 0;
            stats.CorrectRejections = 0;
            UpdateTotalStatistics (ref stats, responses[responses.Count - 1]);
            foreach(var response in responses)
            {
                 UpdateSlidingStatistics(ref stats, response);
            }
            return stats;
        });
        
    }
}
