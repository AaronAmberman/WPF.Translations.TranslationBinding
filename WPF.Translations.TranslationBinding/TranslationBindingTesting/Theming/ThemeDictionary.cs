using System;
using System.Windows;

namespace TranslationBindingTesting.Theming
{
    internal class ThemeDictionary : ResourceDictionary
    {
        #region Fields

        private Uri _darkSource;
        private Uri _lightSource;

        #endregion

        #region Properties

        public Uri DarkSource
        {
            get => _darkSource;
            set
            {
                _darkSource = value;
            }
        }

        public Uri LightSource
        {
            get => _lightSource;
            set
            {
                _lightSource = value;
            }
        }

        #endregion

        #region Methods

        public void UpdateTheme(Theme theme)
        {
            switch (theme)
            {
                case Theme.Dark:
                    Source = _darkSource;
                    break;
                case Theme.Light:
                    Source = _lightSource;
                    break;
                default:
                    Source = _darkSource;
                    break;
            }
        }

        #endregion
    }
}
