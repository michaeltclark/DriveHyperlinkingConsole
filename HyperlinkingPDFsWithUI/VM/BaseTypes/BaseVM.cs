using System.ComponentModel;
using PropertyChanged;

namespace HyperlinkingPDFsWithUI
{
    /// <summary>
    /// Base class for all View Models. Implements the PropertyChanged Interface
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class BaseVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
    }
}
