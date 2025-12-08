using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BankServer.ViewModels
{
    // ViewModelBase implements INotifyPropertyChanged to support data binding in MVVM architecture.
    // It provides OnPropertyChanged and SetProperty methods to simplify property change notifications.
    // It's abstract, so it serves as a base class for other ViewModels in the application.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T newValue,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(backingField, newValue))
                return false;

            backingField = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
