using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using Bonsai;
using Bonsai.Design;
using Bonsai.Vision.Design;

[assembly: TypeVisualizer(typeof(VisualizerGrid), Target = typeof(object))]
[assembly: TypeVisualizer(typeof(ControlMashup<JoystickVisualizer>), Target = typeof(VisualizerMashup<VisualizerGrid, JoystickVisualizer>))]
[assembly: TypeVisualizer(typeof(ControlMashup<StateVisualizer>), Target = typeof(VisualizerMashup<VisualizerGrid, StateVisualizer>))]
[assembly: TypeVisualizer(typeof(ControlMashup<ResponseVisualizer>), Target = typeof(VisualizerMashup<VisualizerGrid, ResponseVisualizer>))]
[assembly: TypeVisualizer(typeof(ControlMashup<IplImageVisualizer>), Target = typeof(VisualizerMashup<VisualizerGrid, IplImageVisualizer>))]

public class VisualizerGrid : DialogMashupVisualizer
{
    internal TableLayoutPanel panel;

    public override void Load(IServiceProvider provider)
    {
        panel = new TableLayoutPanel();
        panel.Dock = DockStyle.Fill;
        panel.ColumnCount = 2;
        panel.RowCount = 2;
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.RowStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.RowStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var visualizerService = (IDialogTypeVisualizerService)provider.GetService(typeof(IDialogTypeVisualizerService));
        if (visualizerService != null)
        {
            visualizerService.AddControl(panel);
        }

        base.Load(provider);
    }

    public override void Show(object value)
    {
    }

    public override IObservable<object> Visualize(IObservable<IObservable<object>> source, IServiceProvider provider)
    {
        return Observable.Merge(Mashups.Select(xs => xs.Visualizer.Visualize(xs.Source, provider)));
    }

    public override void Unload()
    {
        base.Unload();
        panel.Dispose();
        panel = null;
    }
}

class ContainerService : IDialogTypeVisualizerService, IServiceProvider
{
    int index;
    TableLayoutPanel panel;
    IServiceProvider provider;

    public ContainerService(TableLayoutPanel panel, IServiceProvider provider)
    {
        this.panel = panel;
        this.provider = provider;
    }

    public void AddControl(Control control)
    {
        var x = index % panel.ColumnCount;
        var y = index / panel.RowCount;
        panel.Controls.Add(control, x, y);
        index++;
    }

    public object GetService(Type serviceType)
    {
        if (serviceType == typeof(IDialogTypeVisualizerService))
        {
            return this;
        }

        return provider.GetService(serviceType);
    }
}

public class ControlMashup<TVisualizer> : MashupTypeVisualizer where TVisualizer : DialogTypeVisualizer, new()
{
    TVisualizer visualizer = new TVisualizer();

    public override void Load(IServiceProvider provider)
    {
        var mashup = (VisualizerGrid)provider.GetService(typeof(DialogMashupVisualizer));
        var container = new ContainerService(mashup.panel, provider);
        visualizer.Load(container);
    }

    public override void Show(object value)
    {
        visualizer.Show(value);
    }

    public override void Unload()
    {
        visualizer.Unload();
    }

    public override IObservable<object> Visualize(IObservable<IObservable<object>> source, IServiceProvider provider)
    {
        return visualizer.Visualize(source, provider);
    }

    public override void SequenceCompleted()
    {
        visualizer.SequenceCompleted();
    }
}