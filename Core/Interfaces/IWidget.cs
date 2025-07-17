using System.Threading.Tasks;
using System.Windows.Controls;

namespace gtt_sidebar.Core.Interfaces
{
    public interface IWidget
    {
        string Name { get; }
        UserControl GetControl();
        Task InitializeAsync();
        void Dispose();
    }
}