using System.Threading.Tasks;
using System.Windows.Input;

namespace HyperlinkingPDFsWithUI
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}
