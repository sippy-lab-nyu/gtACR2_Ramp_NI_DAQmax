using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Drawing;
using Bonsai.Expressions;
using System.Linq.Expressions;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Source)]
public class WindowMemory : ZeroArgumentExpressionBuilder
{
    Rectangle bounds;

    public Rectangle Bounds
    {
        get
        {
            var mainForm = (Form)Application.OpenForms.Cast<Form>().FirstOrDefault();
            if (mainForm != null)
            {
                return bounds = mainForm.DesktopBounds;
            }
            else return bounds;
        }
        set
        {
            bounds = value;
            var mainForm = (Form)Application.OpenForms.Cast<Form>().FirstOrDefault();
            if (mainForm != null)
            {
                mainForm.DesktopBounds = value;
            }
        }
    }

    public override Expression Build(IEnumerable<Expression> arguments)
    {
        var mainForm = (Form)Form.ActiveForm;
        if (mainForm != null)
        {
            mainForm.DesktopBounds = bounds;
        }
        return Expression.Call(typeof(WindowMemory), "Process", null);
    }

    static IObservable<int> Process()
    {
        return Observable.Return(0);
    }
}
