using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;

namespace Serilog.ReSharper
{
    [ActionHandler("Serilog.ReSharper.About")]
    public class AboutAction : IActionHandler
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            // return true or false to enable/disable this action
            return true;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            MessageBox.Show(
              "Serilog.ReSharper\nKristian Hellang\n\nReSharper support for Serilog",
              "About Serilog.ReSharper",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information);
        }
    }
}