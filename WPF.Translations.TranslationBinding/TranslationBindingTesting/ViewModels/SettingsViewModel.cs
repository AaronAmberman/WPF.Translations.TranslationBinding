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
            new Tuple<string, string>("en", TranslationBindingOperations.GetTranslation("English")),
            new Tuple<string, string>("zh-Hans", TranslationBindingOperations.GetTranslation("Chinese")), //Chinese (Simplified)
            new Tuple<string, string>("fr", TranslationBindingOperations.GetTranslation("French")),
            new Tuple<string, string>("de", TranslationBindingOperations.GetTranslation("German")),
            new Tuple<string, string>("it", TranslationBindingOperations.GetTranslation("Italian")),
            new Tuple<string, string>("ja", TranslationBindingOperations.GetTranslation("Japanese")),
            new Tuple<string, string>("ko", TranslationBindingOperations.GetTranslation("Korean")),
            new Tuple<string, string>("no", TranslationBindingOperations.GetTranslation("Norwegian")),
            new Tuple<string, string>("pt", TranslationBindingOperations.GetTranslation("Portuguese")),
            new Tuple<string, string>("ru", TranslationBindingOperations.GetTranslation("Russian")),
            new Tuple<string, string>("es", TranslationBindingOperations.GetTranslation("Spanish"))
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
                Title = TranslationBindingOperations.GetTranslation("BrowseTitle"),
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

                //((Translation)ServiceLocator.Instance.MainWindowViewModel.Translations).Dispose();

                //ServiceLocator.Instance.MainWindowViewModel.Translations = new Translation(new ResourceDictionary
                //{
                //    Source = new Uri($"pack://application:,,,/Translations/Translations.{Settings.Default.Language}.xaml")
                //}, new ResourceDictionaryTranslationDataProvider(), false);

                /*
                 * If TranslationBindingOperations.RefreshAutomatically = true; then this method won't need to be called manually.
                 * However if it were set to false then it would need to be called manually to trigger the CultureChanged event.
                 */
                //TranslationBindingOperations.RefreshTranslations();
            }

            if (!string.IsNullOrWhiteSpace(LogPath))
            {
                if (!File.Exists(LogPath))
                {
                    string message = TranslationBindingOperations.GetTranslation("LogFileNotExistsMessage");
                    string title = TranslationBindingOperations.GetTranslation("LogFileNotExistsTitle");

                    ServiceLocator.Instance.MainWindowViewModel.ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxInternalDialogImage.CriticalError);

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
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("English"));
                    break;
                case "zh-Hans":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Chinese"));
                    break;
                case "fr":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("French"));
                    break;
                case "de":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("German"));
                    break;
                case "it":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Italian"));
                    break;
                case "ja":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Japanese"));
                    break;
                case "ko":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Korean"));
                    break;
                case "no":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Norwegian"));
                    break;
                case "pt":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Portuguese"));
                    break;
                case "ru":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Russian"));
                    break;
                case "es":
                    SelectedLanguage = new Tuple<string, string>(language, TranslationBindingOperations.GetTranslation("Spanish"));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
