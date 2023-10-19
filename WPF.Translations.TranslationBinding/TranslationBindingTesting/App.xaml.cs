using SimpleRollingLogger;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TranslationBindingTesting.Properties;
using TranslationBindingTesting.Services;
using TranslationBindingTesting.Theming;
using TranslationBindingTesting.ViewModels;
using WPF.Translations;
using WPF.Translations.TranslationBinding;

namespace TranslationBindingTesting
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            #region Service Initialization

            ServiceLocator.Instance.Logger = new Logger();
            ServiceLocator.Instance.ThemingService = new ThemingService();

            #endregion

            #region Log File

            try
            {
                if (!string.IsNullOrWhiteSpace(Settings.Default.LogPath))
                {
                    // the setting will be the path only (will not include the filename)
                    ServiceLocator.Instance.Logger.LogFile = Path.Combine(Settings.Default.LogPath, "TranslationBindingTesting.log");
                }
                else
                {
                    string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        ServiceLocator.Instance.Logger.LogFile = Path.Combine(location, "TranslationBindingTesting.log");
                    }
                }
            }
            catch
            {
                // we cannot determine location for some reason, use desktop
                ServiceLocator.Instance.Logger.LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TranslationBindingTesting.log");
            }

            #endregion

            #region View Models

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel
            {
                MessageBoxViewModel = new MessageBoxViewModel(),
                ProgressViewModel = new ProgressViewModel()
            };

            ServiceLocator.Instance.MainWindowViewModel = mainWindowViewModel;

            #endregion

            #region Version

            ServiceLocator.Instance.MainWindowViewModel.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            #endregion

            #region Translations

            // set settings
            if (string.IsNullOrWhiteSpace(Settings.Default.Language))
            {
                // load english if the language is missing from settings
                Settings.Default.Language = "en";
                Settings.Default.Save();
            }

            // set our culture to the current one from settings
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(Settings.Default.Language);

            TranslationBindingOperations.RefreshAutomatically = true;
            //TranslationBindingOperations.RefreshTranslations();

            // set translations
            ServiceLocator.Instance.MainWindowViewModel.Translations = new Translation(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Translations/Translations.{Settings.Default.Language}.xaml")
            }, new ResourceDictionaryTranslationDataProvider(), false);

            // need translations for view model
            mainWindowViewModel.SettingsViewModel = new SettingsViewModel();
            mainWindowViewModel.SettingsViewModel.SetLanguage(Settings.Default.Language);

            #endregion

            #region Theming

            ServiceLocator.Instance.ThemingService.Theme = (Theme)Settings.Default.Theme;

            mainWindowViewModel.SettingsViewModel.Theme = Settings.Default.Theme;

            #endregion
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                ServiceLocator.Instance.Logger.Error($"An unhandled exception occurred. Details:{Environment.NewLine}{e.Exception}");

                MessageBox.Show(ServiceLocator.Instance.MainWindowViewModel?.Translations.UnhandledErrorMessage ?? "Unhandled exception occurred. We have logged the issue.",
                    ServiceLocator.Instance.MainWindowViewModel?.Translations.UnhandledErrorTitle ?? "Unhandled Exception",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred ensuring the log file exists or an error occurred trying to write to the log file.{Environment.NewLine}{ex}");
            }

            Environment.Exit(e.Exception.HResult);
        }
    }
}
