using System.Collections.Generic;

namespace WPF.Translations.TranslationBinding
{
    /// <summary>Describes an object that handles returning translations from a source.</summary>
    public interface ITranslationProvider
    {
        IDictionary<string, string> GetTranslationsForCulture(string culture);
    }
}
