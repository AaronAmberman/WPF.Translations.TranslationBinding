using System;
using System.Collections.Generic;
using System.Windows;
using WPF.Translations.TranslationBinding;

namespace TranslationBindingTesting.Translations
{
    internal class TranslationProvider : ITranslationProvider
    {
        public IDictionary<string, string> GetTranslationsForCulture(string culture)
        {
            Dictionary<string, string> translations = new Dictionary<string, string>();

            try
            {
                // we have multiple translation data sources
                // we will go through the main applications translations first
                string translationXaml = $"pack://application:,,,/Translations/Translations.{culture}.xaml";

                ReadInTranslations(translationXaml, translations);
            }
            catch (Exception ex)
            {
                ServiceLocator.Instance.Logger.Error($"An error occurred attempting to load translations.{Environment.NewLine}{ex}");
            }

            try
            {
                // next get translations from satelite assemblies
                string translationXaml = $"pack://application:,,,/CustomControlTesting;component/Translations/Translations.{culture}.xaml";

                ReadInTranslations(translationXaml, translations);
            }
            catch (Exception ex)
            {
                ServiceLocator.Instance.Logger.Error($"An error occurred attempting to load translations from satelite assembly: CustomControlTesting.{Environment.NewLine}{ex}");
            }

            return translations;
        }

        private void ReadInTranslations(string resource, Dictionary<string, string> translations)
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri(resource);

            foreach (string key in resourceDictionary.Keys)
            {
                // no duplicate keys
                if (translations.ContainsKey(key)) continue;

                translations.Add(key, resourceDictionary[key].ToString());
            }
        }
    }
}
