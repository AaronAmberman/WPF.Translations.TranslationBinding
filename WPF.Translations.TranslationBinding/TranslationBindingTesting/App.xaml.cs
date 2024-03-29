﻿using SimpleRollingLogger;
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
using TranslationBindingTesting.Translations;
using TranslationBindingTesting.ViewModels;
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

            TranslationBindingOperations.TranslationProvider = new TranslationProvider();
            //TranslationBindingOperations.UseUICulture = true;

            /*
             * TranslationBindingOperations.ReadInTranslationsForCulture() needs to be called because the SettingsViewModel
             * needs access to the names of the cultures for the drop down in settings. 
             * 
             * The building of the drop down list could be moved to a loaded event and the need to manually load the translations
             * would go away. This is another option if the developer did not want to "preload" the translations. This method 
             * would not need to be called in the loaded event...only here. This is because by the time the loaded event occurred
             * the XAML processor/renderer would have loaded the first TranslationBinding instance...which would automatically
             * load the translations in.
             */
            TranslationBindingOperations.ReadInTranslationsForCulture();
            TranslationBindingOperations.RefreshAutomatically = true;

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

                string message = TranslationBindingOperations.GetTranslation("UnhandledErrorMessage");
                string title = TranslationBindingOperations.GetTranslation("UnhandledErrorTitle");

                MessageBox.Show(message ?? "Unhandled exception occurred. We have logged the issue.",
                    title ?? "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred ensuring the log file exists or an error occurred trying to write to the log file.{Environment.NewLine}{ex}");
            }

            Environment.Exit(e.Exception.HResult);
        }
    }
}
