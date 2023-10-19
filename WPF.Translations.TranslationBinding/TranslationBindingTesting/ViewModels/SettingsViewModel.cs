using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TranslationBindingTesting.Properties;
using TranslationBindingTesting.Theming;
using WPF.InternalDialogs;
using WPF.Translations;
using WPF.Translations.TranslationBinding;

namespace TranslationBindingTesting.ViewModels
{
    internal class SettingsViewModel : ViewModelBase
    {
        #region Fields

        private ICommand aboutCommand;
        private Visibility aboutBoxVisibility = Visibility.Collapsed;
        private ICommand browseLogCommand;
        private ICommand cancelCommand;
        private string logFile;
        private ICommand okCommand;
        private MessageBoxResult result;
        private Tuple<string, string> selectedLanguage;
        private int theme;
        private Visibility visibility = Visibility.Collapsed;

        #endregion

        #region Properties

        public ICommand AboutCommand => aboutCommand ??= new RelayCommand(About);

        public Visibility AboutBoxVisibility
        {
            get => aboutBoxVisibility;
            set
            {
                aboutBoxVisibility = value;
                OnPropertyChanged();
            }
        }

        public ICommand BrowseLogCommand => browseLogCommand ??= new RelayCommand(BrowseLog);

        public ICommand CancelCommand => cancelCommand ??= new RelayCommand(Cancel);

        public List<Tuple<string, string>> Languages { get; set; } = new List<Tuple<string, string>>
        {
            new Tuple<string, string>("en", ServiceLocator.Instance.MainWindowViewModel.Translations.English),
            new Tuple<string, string>("zh-Hans", ServiceLocator.Instance.MainWindowViewModel.Translations.Chinese), //Chinese (Simplified)
            new Tuple<string, string>("fr", ServiceLocator.Instance.MainWindowViewModel.Translations.French),
            new Tuple<string, string>("de", ServiceLocator.Instance.MainWindowViewModel.Translations.German),
            new Tuple<string, string>("it", ServiceLocator.Instance.MainWindowViewModel.Translations.Italian),
            new Tuple<string, string>("ja", ServiceLocator.Instance.MainWindowViewModel.Translations.Japanese),
            new Tuple<string, string>("ko", ServiceLocator.Instance.MainWindowViewModel.Translations.Korean),
            new Tuple<string, string>("no", ServiceLocator.Instance.MainWindowViewModel.Translations.Norwegian),
            new Tuple<string, string>("pt", ServiceLocator.Instance.MainWindowViewModel.Translations.Portuguese),
            new Tuple<string, string>("ru", ServiceLocator.Instance.MainWindowViewModel.Translations.Russian),
            new Tuple<string, string>("es", ServiceLocator.Instance.MainWindowViewModel.Translations.Spanish)
        };

        public string LogPath
        {
            get => logFile;
            set
            {
                logFile = value;
                OnPropertyChanged();
            }
        }

        public ICommand OkCommand => okCommand ??= new RelayCommand(Ok);

        public MessageBoxResult Result
        {
            get => result;
            set
            {
                result = value;
                OnPropertyChanged();
            }
        }

        public Tuple<string, string> SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                selectedLanguage = value;
                OnPropertyChanged();
            }
        }

        public int Theme
        {
            get => theme;
            set
            {
                theme = value;
                OnPropertyChanged();
            }
        }

        public Visibility Visibility
        {
            get => visibility;
            set
            {
                visibility = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        private void About()
        {
            AboutBoxVisibility = Visibility.Visible;
        }

        private void BrowseLog()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                FileName = "TranslationBindingTesting.log",
                Filter = "Text Files(*.txt)|*.txt|Log Files(*.log)|*.log",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Multiselect = false,
                Title = ServiceLocator.Instance.MainWindowViewModel.Translations.BrowseTitle,
                ValidateNames = true
            };

            bool? result = ofd.ShowDialog();

            if (!result.HasValue) return;
            if (!result.Value) return;

            string file = ofd.FileName;

            LogPath = Path.GetDirectoryName(file);
        }

        private void Cancel()
        {
            LogPath = Settings.Default.LogPath;

            Result = MessageBoxResult.Cancel;
            Visibility = Visibility.Collapsed;
        }

        private void Ok()
        {
            if (SelectedLanguage.Item1 != Settings.Default.Language)
            {
                Settings.Default.Language = SelectedLanguage.Item1;

                Thread.CurrentThread.CurrentCulture = new CultureInfo(SelectedLanguage.Item1);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(SelectedLanguage.Item1);

                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(Settings.Default.Language);

                ((Translation)ServiceLocator.Instance.MainWindowViewModel.Translations).Dispose();

                ServiceLocator.Instance.MainWindowViewModel.Translations = new Translation(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/Translations/Translations.{Settings.Default.Language}.xaml")
                }, new ResourceDictionaryTranslationDataProvider(), false);

                //TranslationBindingOperations.RefreshTranslations();
            }

            if (!string.IsNullOrWhiteSpace(LogPath))
            {
                if (!File.Exists(LogPath))
                {
                    ServiceLocator.Instance.MainWindowViewModel.ShowMessageBox(ServiceLocator.Instance.MainWindowViewModel.Translations.LogFileNotExistsMessage,
                        ServiceLocator.Instance.MainWindowViewModel.Translations.LogFileNotExistsTitle, MessageBoxButton.OK, MessageBoxInternalDialogImage.CriticalError);

                    return;
                }

                ServiceLocator.Instance.Logger.LogFile = LogPath;
            }
            else
            {
                // null, empty or white-space, ensure our log file like we did in app startup
                try
                {
                    string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        // do not set the setting because the user did not choose the path
                        ServiceLocator.Instance.Logger.LogFile = Path.Combine(location, "TranslationBindingTesting.log");
                    }
                }
                catch
                {
                    // we cannot determine location for some reason, use desktop
                    ServiceLocator.Instance.Logger.LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TranslationBindingTesting.log");
                }
            }

            // allow the setting to take the entered path or null but not bad input
            Settings.Default.LogPath = LogPath;

            if (Theme != Settings.Default.Theme)
            {
                Settings.Default.Theme = Theme;

                ServiceLocator.Instance.ThemingService.Theme = (Theme)Theme;
            }

            Settings.Default.Save();

            Result = MessageBoxResult.OK;
            Visibility = Visibility.Collapsed;
        }

        public void SetLanguage(string language)
        {
            switch (language)
            {
                case "en":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.English);
                    break;
                case "zh-Hans":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Chinese);
                    break;
                case "fr":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.French);
                    break;
                case "de":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.German);
                    break;
                case "it":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Italian);
                    break;
                case "ja":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Japanese);
                    break;
                case "ko":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Korean);
                    break;
                case "no":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Norwegian);
                    break;
                case "pt":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Portuguese);
                    break;
                case "ru":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Russian);
                    break;
                case "es":
                    SelectedLanguage = new Tuple<string, string>(language, ServiceLocator.Instance.MainWindowViewModel.Translations.Spanish);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
