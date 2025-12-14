using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace BankClient.Behaviors
{
    public static class WindowClosingBehavior
    {
       
        public static readonly DependencyProperty ClosingCommandProperty =
            DependencyProperty.RegisterAttached(
                "ClosingCommand",
                typeof(ICommand),
                typeof(WindowClosingBehavior),
                new PropertyMetadata(null, OnClosingCommandChanged));

        public static ICommand GetClosingCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ClosingCommandProperty);
        }

        public static void SetClosingCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ClosingCommandProperty, value);
        }

        private static void OnClosingCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
               
                if (e.OldValue is ICommand oldCommand)
                {
                    window.Closing -= Window_Closing;
                }

               
                if (e.NewValue is ICommand newCommand)
                {
                    window.Closing += Window_Closing;
                }
            }
        }

        private static void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is Window window)
            {
                var command = GetClosingCommand(window);

              
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}
