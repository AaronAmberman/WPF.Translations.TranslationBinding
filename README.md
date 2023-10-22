# WPF.Translations.TranslationBinding
A {TranslationBinding} for XAML.

# Do Not Mix
My other translations API [WPF.Translations](https://github.com/AaronAmberman/WPF.Translations), Nuget package [here](https://www.nuget.org/packages/WPF.Translations/), and this API should not be used together. You should choose one or the other to manage your translations. Both will meet your needs whatever your situation but are used differently.

# Basic Usage
This is not a MS package natively included in WPF so after you add the Nuget package reference to your project/solution, you'll need to add a namespace declaration at the top of your XAML file...

```XAML
xmlns:tb="clr-namespace:WPF.Translations.TranslationBinding;assembly=WPF.Translations.TranslationBinding"
```

Usage will be very similar to a normal binding...

```XAML
{tb:TranslationBinding TranslationKey=MyKey, FallbackValue=Fallback value for the translation binding.}
```

Parameters are supported, more on that below.

# Requirements For Usage
There are a few things to note about the functionality of the API...

- *CultureInfo.DefaultThreadCurrentCulture* or *CultureInfo.DefaultThreadCurrentUICulture* are used to track whether or not the culture changes. Use *CultureInfo.DefaultThreadCurrentCulture* if ***UseUICulture*** = false and use *CultureInfo.DefaultThreadCurrentUICulture* if ***UseUICulture*** = true. The developer is responsible for setting the CultureInfo property important to them, or both.
- ***ITranslationProvider*** is required to be implemented by the developer. The interface implementation should be in the WPF application that has the need for translations. It should not be in satelite assemblies. There should be only one, meaning satelite assemblies should not all implement ***ITranslationProvider***...even if they have their own translation needs. That being said satelite assemblies can have their own translations. More in the examples section. Additional note, the API will throw an InvalidOperationException if a ***ITranslationProvider*** is not set on the ***TranslationBindingOperations.TranslationProvider*** property before the first translation request is made.

# Notes About Usage
- Clean up of old translation bindings happens when the TranslationBindingOperation.CultureInfo is fired. This cannot be changed, modified or managed differently. Call TranslationBindingOperations.RefreshTranslations, even if the culture did not change and the API will clean up old references.
  - For example, if you open a Window that has TranslationBindings in it and then close that window, those TranslationBindings will sit in memory until clean up occurs.
- A TranslationBinding cannot be used in a Setter of a Style in XAML. So for example...
```XAML
<Style x:Key="TextBlockPropertyUsage" TargetType="TextBlock">
    <Setter Property="Text" Value="{tb:TranslationBinding TranslationKey=Test, FallbackValue=Test fallback}" />
</Style>
```
- An error reading something like; "TranslationBindingExtension is not valid for Setter.Value. The only supported MarkupExtension types are DynamicResourceExtension and BindingBase or derived types."
  - A TranslationBinding can ***only*** be set on a DependencyObject. So set it on the instance of the TextBlock using that style.
  - If you need this kind of functionality then I suggest checking out my other translation API mentioned above.
- Translations are a runtime thing not a design time thing. So if you want to see something in the designer then enter a ***FallbackValue***.
- If the developer has the need to use a translation before the XAML processor/renderer processes the first TranslationBinding then the developer will have to call ***TranslationBindingOperations.ReadInTranslationsForCulture()*** manually for the API to read the translation. Call this after setting *CultureInfo.DefaultThreadCurrentCulture* or *CultureInfo.DefaultThreadCurrentUICulture* or both. The API will read in translations when the XAML processor/renderer processes the first instance of a TranslationBinding.
