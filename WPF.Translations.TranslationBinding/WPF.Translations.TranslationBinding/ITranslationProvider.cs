using System.Collections.Generic;

namespace WPF.Translations.TranslationBinding
{
    /// <summary>Describes an object that handles returning translations from a source.</summary>
    /// <remarks>Be sure to include a default (empty) constructor for the type.</remarks>
    public interface ITranslationProvider
    {
        /// <summary>Gets the key value pairs that make up translation.</summary>
        /// <param name="culture">The culture to get translations for.</param>
        /// <returns>A dictionary or translations.</returns>
        /// <remarks>
        /// The incoming culture should be the culture code...
        /// en
        /// en-Gb
        /// fr
        /// it
        /// ko
        /// zh-Hans
        /// zh-Hant
        /// etc.
        /// </remarks>
        IDictionary<string, string> GetTranslationsForCulture(string culture);
    }
}
