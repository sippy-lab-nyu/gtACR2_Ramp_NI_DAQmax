using Bonsai;
using Bonsai.Design;
using Bonsai.Design.Visualizers;
using System;
using System.Windows.Forms;
using ZedGraph;

[assembly: TypeVisualizer(typeof(JoystickVisualizer), Target = typeof(Tuple<double, double>[]))]

public class JoystickVisualizer : DialogTypeVisualizer
{
    static readonly TimeSpan TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30);
    GraphControl graph;
    IPointListEdit[] lineSeries;
    DateTimeOffset updateTime;

    internal void AddValues(Tuple<double, double>[] values)
    {
        EnsureSeries(values.Length);
        if (values.Length > 0)
        {
            for (int i = 0; i < lineSeries.Length; i++)
            {
                lineSeries[i].Add(values[i].Item1, values[i].Item2);
            }
        }
    }

    void EnsureSeries(int count)
    {
        if (lineSeries == null || lineSeries.Length != count)
        {
            graph.ResetColorCycle();
            graph.GraphPane.CurveList.Clear();
            lineSeries = new IPointListEdit[count];
            for (int i = 0; i < lineSeries.Length; i++)
            {
                var color = graph.GetNextColor();
                var values = (IPointListEdit)new PointPairList();
                var lineItem = new LineItem(
                    null,
                    values,
                    color,
                    SymbolType.None,
                    lineWidth: 1);
                lineItem.Line.IsAntiAlias = true;
                lineItem.Line.IsOptimizedDraw = true;
                lineItem.Symbol.Fill.Type = FillType.Solid;
                lineItem.Symbol.IsAntiAlias = true;
                graph.GraphPane.CurveList.Add(lineItem);
                lineSeries[i] = values;
            }
        }
    }

    public override void Load(IServiceProvider provider)
    {
        graph = new GraphControl();
        graph.Dock = DockStyle.Fill;
        graph.GraphPane.XAxis.Title.IsVisible = true;
        graph.GraphPane.YAxis.Title.IsVisible = true;
        graph.GraphPane.XAxis.Title.Text = "Time (sec)";
        graph.GraphPane.YAxis.Title.Text = "Joystick Deflection";

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
        AddValues((Tuple<double, double>[])value);
        UpdateView(DateTime.Now);
    }

    public override void Unload()
    {
        graph.Dispose();
        graph = null;
        lineSeries = null;
    }
}