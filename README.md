# WPF.Translations.TranslationBinding
A {TranslationBinding} for XAML.

# Do Not Mix
My other translations API [WPF.Translations](https://github.com/AaronAmberman/WPF.Translations), Nuget package [here](https://www.nuget.org/packages/WPF.Translations/), and this API should not be used together. You should choose one or the other to manage your translations. Both will meet your needs whatever your situation but are used differently.

# Basic Usage
This is not a MS package natively included in WPF so after you add the Nuget package reference to it, you'll need to add a namespace declaration at the top of your XAML file...

```XML
xmlns:tb="clr-namespace:WPF.Translations.TranslationBinding;assembly=WPF.Translations.TranslationBinding"
```

Usage will be very similar to a normal binding...

```XML
{tb:TranslationBinding TranslationKey=MyKey, FallbackValue=Fallback value for the translation binding.}
```
