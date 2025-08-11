using Bonsai;
using Bonsai.Design;
using Bonsai.Design.Visualizers;
using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

[assembly: TypeVisualizer(typeof(StateVisualizer), Target = typeof(StateDescriptor))]

public class StateVisualizer : DialogTypeVisualizer
{
    static readonly TimeSpan TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30);
    static readonly string[] StateLabels = Enum.GetNames(typeof(StateId));
    static readonly StateId[] StateValues = (StateId[])Enum.GetValues(typeof(StateId));

    GraphControl graph;
    IPointListEdit[] barSeries;
    IPointListEdit[] rasterSeries;
    int ordinalCounter;
    DateTimeOffset updateTime;


    public StateVisualizer()
    {
        Capacity = 10;
    }

    public int Capacity { get; set; }

    internal void AddEvents(int index, EventDescriptor[] events)
    {
        if (events == null) return;
        for (int i = 0; i < events.Length; i++)
        {
            var evtIndex = (int)(events[i].Id - StateId.Annotation);
            rasterSeries[evtIndex].Add(events[i].Value, index);
        }
    }

    internal void AddStatistics(string index, double[] values)
    {
        EnsureSeries(values.Length);
        if (values.Length > 0)
        {
            var count = barSeries[0].Count;
            var updateLast = count > 0 && index.Equals(barSeries[0][count - 1].Tag);
            if (updateLast)
            {
                for (int i = 0; i < barSeries.Length; i++)
                    barSeries[i][count - 1].X = values[i];
            }
            else
            {
                ordinalCounter++;
                for (int i = 0; i < barSeries.Length; i++)
                    barSeries[i].Add(new PointPair(values[i], 0, index));
            }
        }
    }

    public static class ColorMap
{
    public static readonly Dictionary<StateId, Color> Default = new Dictionary<StateId, Color>
    {
        { StateId.ITI, Color.Gray },
        { StateId.Go, Color.ForestGreen },
        { StateId.NoGo, Color.Gold },
        { StateId.Response, Color.RoyalBlue },
        { StateId.Timeout, Color.Red },
        { StateId.ITI2, Color.Gray },
        { StateId.Annotation, Color.Black },
        { StateId.Joystick, Color.White },
        { StateId.Lick, Color.HotPink },
        { StateId.Blink, Color.Violet },
        { StateId.Hit, Color.Transparent},
        { StateId.Miss, Color.Transparent},
        { StateId.CorrectRejection, Color.Transparent},
        { StateId.FalseAlarm, Color.Transparent},
        { StateId.PullPenalty, Color.Transparent},
        { StateId.EarlyResponse, Color.Transparent},
    };
}

    class RasterPointList : IPointListEdit
    {
        readonly int cap;
        readonly Scale scale;
        readonly RollingPointPairList list;
        readonly StateVisualizer owner;

        public RasterPointList(int capacity, Axis axis, StateVisualizer visualizer)
        {
            cap = capacity;
            scale = axis.Scale;
            list = new RollingPointPairList(500);
            owner = visualizer;
        }

        public PointPair this[int index]
        {
            get { return ((IPointList)this)[index]; }
            set { list[index] = value; }
        }


        PointPair IPointList.this[int index]
        {
            get
            {
                var point = list[index];
                var ordinal = (int)(point.Y - Math.Max(0, owner.ordinalCounter - cap));
                var y = scale.Transform(false, ordinal, 0);
                var x = point.X;
                return new PointPair(x, y);
            }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public void Add(PointPair point)
        {
            list.Add(point);
        }

        public void Add(double x, double y)
        {
            list.Add(x, y);
        }

        public void Clear()
        {
            list.Clear();
        }

        public object Clone()
        {
            return null;
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }
    }

    void EnsureSeries(int count)
    {
        if (barSeries == null || barSeries.Length != count)
        {
            graph.GraphPane.CurveList.Clear();
            ordinalCounter = 0;
            
            var index = graph.GraphPane.AddYAxis(null);
            graph.GraphPane.YAxisList[index].IsVisible = false;
            graph.GraphPane.YAxisList[index].Scale.IsReverse = true;
            rasterSeries = new IPointListEdit[StateValues.Length - (int)StateId.Annotation];
            for (int i = 0; i < rasterSeries.Length; i++)
            {
                var color = ColorMap.Default[StateId.Annotation + i];
                rasterSeries[i] = new RasterPointList(Capacity, graph.GraphPane.YAxis, this);
                var rasterItem = new LineItem(null, rasterSeries[i], color, SymbolType.Circle, 0);
                rasterItem.Symbol.IsAntiAlias = true;
                rasterItem.Symbol.Fill.Type = FillType.Solid;
                rasterItem.YAxisIndex = index;
                graph.GraphPane.CurveList.Add(rasterItem);
            }

            barSeries = new IPointListEdit[count];
            for (int i = 0; i < barSeries.Length; i++)
            {
                var color = ColorMap.Default[StateValues[i]];
                var values = Capacity > 0
                    ? (IPointListEdit)new RollingPointPairList(Capacity)
                    : new PointPairList();
                var barItem = new BarItem(StateLabels[i], values, color);
                barItem.Bar.Fill.Type = FillType.Solid;
                barItem.Bar.Border.IsVisible = false;
                graph.GraphPane.CurveList.Add(barItem);
                barSeries[i] = values;
            }
        }
    }

    public override void Load(IServiceProvider provider)
    {
        var context = (ITypeVisualizerContext)provider.GetService(typeof(ITypeVisualizerContext));

        graph = new GraphControl();
        graph.Dock = DockStyle.Fill;
        graph.GraphPane.BarSettings.Base = BarBase.Y;
        graph.GraphPane.BarSettings.Type = BarType.Stack;
        graph.GraphPane.XAxis.Title.IsVisible = true;
        graph.GraphPane.XAxis.Title.Text = "Time (sec)";
        var indexAxis = graph.GraphPane.YAxis;
        indexAxis.Title.IsVisible = true;
        indexAxis.Title.Text = "Trial";
        indexAxis.Type = AxisType.Text;
        indexAxis.Scale.IsReverse = true;
        indexAxis.MinorTic.IsAllTics = false;
        indexAxis.MajorTic.IsInside = false;
        indexAxis.ScaleFormatEvent += (scaleGraph, axis, value, index) =>
        {
            if (scaleGraph.CurveList.Count == 0) return null;
            var series = scaleGraph.CurveList[0];
            return index < series.NPts ? series[index].Tag as string : null;
        };

        EnsureSeries((int)StateId.Annotation);
        graph.GraphPane.AxisChangeEvent += pane =>
        {
            foreach (var axis in pane.YAxisList)
            {
                if (axis == indexAxis) continue;
                axis.Scale.Min = pane.YAxis.Scale.Transform(pane.YAxis.Scale.Min);
                axis.Scale.Max = pane.YAxis.Scale.Transform(pane.YAxis.Scale.Max);
            }
        };

        var visualizerService = (IDialogTypeVisualizerService)provider.GetService(typeof(IDialogTypeVisualizerService));
        if (visualizerService != null)
        {
            visualizerService.AddControl(graph);
        }
    }

    internal void UpdateView(DateTime time)
    {
        if ((time - updateTime) > TargetElapsedTime)
        {
            graph.Invalidate();
            updateTime = time;
        }
    }

    public override void Show(object value)
    {
        var descriptor = (StateDescriptor)value;
        AddStatistics(descriptor.Trial.ToString(), descriptor.Statistics);
        AddEvents(descriptor.Trial, descriptor.Events);
        UpdateView(DateTime.Now);
    }

    public override void Unload()
    {
        graph.Dispose();
        graph = null;
        barSeries = null;
    }
}
