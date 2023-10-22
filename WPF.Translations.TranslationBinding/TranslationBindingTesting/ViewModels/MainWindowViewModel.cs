using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPF.InternalDialogs;
using WPF.Translations;

namespace TranslationBindingTesting.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private ICommand showSettingsCommand;
        //private Translation translations;
        private string version;

        #endregion

        #region Properties

        public Dispatcher Dispatcher { get; set; }

        public MessageBoxViewModel MessageBoxViewModel { get; set; }

        public ProgressViewModel ProgressViewModel { get; set; }

        public SettingsViewModel SettingsViewModel { get; set; }

        public ICommand ShowSettingsCommand => showSettingsCommand ??= new RelayCommand(ShowSettings);

        //public dynamic Translations
        //{
        //    get => translations;
        //    set
        //    {
        //        translations = value;
        //        OnPropertyChanged();

        //    }
        //}

        public string Version
        {
            get => version;
            set
            {
                version = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public void BeginInvoke(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }

        public void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }

        private void SetMessageBoxState(string message, string title, bool isModal, MessageBoxButton button, MessageBoxInternalDialogImage image, Visibility visibility)
        {
            MessageBoxViewModel.MessageBoxMessage = message;
            MessageBoxViewModel.MessageBoxTitle = title;
            MessageBoxViewModel.MessageBoxIsModal = isModal;
            MessageBoxViewModel.MessageBoxButton = button;
            MessageBoxViewModel.MessageBoxImage = image;
            MessageBoxViewModel.MessageBoxVisibility = visibility;
        }

        public void ShowMessageBox(string message)
        {
            SetMessageBoxState(message, string.Empty, true, MessageBoxButton.OK, MessageBoxInternalDialogImage.Information, Visibility.Visible);
        }

        public void ShowMessageBox(string message, string title)
        {
            SetMessageBoxState(message, title, true, MessageBoxButton.OK, MessageBoxInternalDialogImage.Information, Visibility.Visible);
        }

        public void ShowMessageBox(string message, string title, MessageBoxButton button)
        {
            SetMessageBoxState(message, title, true, button, MessageBoxInternalDialogImage.Information, Visibility.Visible);
        }

        public void ShowMessageBox(string message, string title, MessageBoxInternalDialogImage image)
        {
            SetMessageBoxState(message, title, true, MessageBoxButton.OK, image, Visibility.Visible);
        }

        public void ShowMessageBox(string message, string title, MessageBoxButton button, MessageBoxInternalDialogImage image)
        {
            SetMessageBoxState(message, title, true, button, image, Visibility.Visible);
        }

        public MessageBoxResult ShowQuestionBox(string question, string title)
        {
            MessageBoxViewModel.MessageBoxMessage = question;
            MessageBoxViewModel.MessageBoxTitle = title;
            MessageBoxViewModel.MessageBoxIsModal = true;
            MessageBoxViewModel.MessageBoxButton = MessageBoxButton.YesNo;
            MessageBoxViewModel.MessageBoxImage = MessageBoxInternalDialogImage.Help;
            MessageBoxViewModel.MessageBoxVisibility = Visibility.Visible; // this will block because of is modal

            return MessageBoxViewModel.MessageBoxResult;
        }

        private void ShowSettings()
        {
            SettingsViewModel.Visibility = Visibility.Visible;
        }

        #endregion
    }
}
