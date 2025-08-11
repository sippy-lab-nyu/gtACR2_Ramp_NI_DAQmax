using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;
using System.Reactive;
using Harp.Behavior;
using System.Reactive.Disposables;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class PwmRamp
{
    public ushort Frequency { get; set; }

    public byte DutyCycle { get; set; }

    public double OnRamp { get; set; }

    public double OffRamp { get; set; }

    public double Duration { get; set; }

    public PwmOutputs Output { get; set; }

    static void UpdatePwmState(IObserver<HarpMessage> observer, PwmOutputs output, byte dutyCycle, ref bool isPwmActive)
    {
        if (dutyCycle > 0 && dutyCycle < 100)
        {
            observer.OnNext(PwmDutyCycleDO0.FromPayload(MessageType.Write, dutyCycle));
            if (!isPwmActive)
            {
                observer.OnNext(PwmStart.FromPayload(MessageType.Write, output));
                isPwmActive = true;
            }
        }
        else if (isPwmActive)
        {
            observer.OnNext(PwmStop.FromPayload(MessageType.Write, output));
            if (dutyCycle <= 0)
                observer.OnNext(OutputClear.FromPayload(MessageType.Write, DigitalOutputs.DO0));
            else
                observer.OnNext(OutputSet.FromPayload(MessageType.Write, DigitalOutputs.DO0));
            isPwmActive = false;
        }
    }

    public IObservable<HarpMessage> Process(IObservable<HarpMessage> source)
    {
        return Observable.Create<HarpMessage>(observer =>
        {
            double startTime = -1;
            double currentTime = -1;
            var output = Output;
            var frequency = Frequency;
            var dutyCycle = DutyCycle;
            var onRamp = OnRamp;
            var offRamp = OffRamp;
            var plateauOffset = onRamp + Duration;
            var totalDuration = plateauOffset + offRamp;
            var onRampStep = dutyCycle / onRamp;
            var offRampStep = dutyCycle / offRamp;
            var isPwmActive = false;

            var sourceObserver = Observer.Create<HarpMessage>(
                value =>
                {
                    var timestamp = value.GetTimestamp();
                    if (startTime < 0)
                    {
                        observer.OnNext(PwmFrequencyDO0.FromPayload(MessageType.Write, frequency));
                        startTime = timestamp;
                    }

                    currentTime = timestamp - startTime;
                    if (currentTime < onRamp)
                    {
                        var rampDuty = (byte)(currentTime * onRampStep);
                        UpdatePwmState(observer, output, rampDuty, ref isPwmActive);
                    }
                    else if (currentTime < plateauOffset)
                    {
                        UpdatePwmState(observer, output, dutyCycle, ref isPwmActive);
                    }
                    else if (currentTime < totalDuration)
                    {
                        var rampDuty = (byte)(dutyCycle - (currentTime - plateauOffset) * offRampStep);
                        UpdatePwmState(observer, output, rampDuty, ref isPwmActive);
                    }
                    else
                    {
                        UpdatePwmState(observer, output, 0, ref isPwmActive);
                        observer.OnCompleted();
                    }
                },
                observer.OnError,
                observer.OnCompleted);
            var disposable = Disposable.Create(() =>
            {
                if (isPwmActive)
                {
                    observer.OnNext(PwmStop.FromPayload(MessageType.Write, Output));
                    isPwmActive = false;
                }
            });
            return new CompositeDisposable(
                disposable,
                source.SubscribeSafe(sourceObserver));
        });
    }
}
