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

- *CultureInfo.DefaultThreadCurrentCulture* or *CultureInfo.DefaultThreadCurrentUICulture* are used to track whether or not the culture changes. Use *CultureInfo.DefaultThreadCurrentCulture* if ***TranslationBindingOperations.UseUICulture*** = false and use *CultureInfo.DefaultThreadCurrentUICulture* if ***TranslationBindingOperations.UseUICulture*** = true. The developer is responsible for setting the CultureInfo property important to them, or both.
- ***ITranslationProvider*** is required to be implemented by the developer. The interface implementation should be in the WPF application that has the need for translations. It should not be in satelite assemblies. There should be only one, meaning satelite assemblies should not all implement ***ITranslationProvider***...even if they have their own translation needs. That being said satelite assemblies can have their own translations. More in the examples section. Additional note, the API will throw an InvalidOperationException if a ***ITranslationProvider*** is not set on the ***TranslationBindingOperations.TranslationProvider*** property before the first translation request is made.

# Notes About Usage
- Clean up of old translation bindings happens when the ***TranslationBindingOperation.CultureInfo*** event is fired. This cannot be changed, modified or managed differently. Call ***TranslationBindingOperations.RefreshTranslations***, even if the culture did not change and the API will clean up old references.
  - For example, if you open a Window that has TranslationBindings in it and then close that window, those TranslationBindings will sit in memory until clean up occurs.
- **A TranslationBinding cannot be used in a Setter of a Style in XAML.** So for example...

```XAML
<Style x:Key="TextBlockPropertyUsage" TargetType="TextBlock">
    <Setter Property="Text" Value="{tb:TranslationBinding TranslationKey=Test, FallbackValue=Test fallback}" />
</Style>
```

- An error reading something like; "TranslationBindingExtension is not valid for Setter.Value. The only supported MarkupExtension types are DynamicResourceExtension and BindingBase or derived types."
  - A TranslationBinding can ***only*** be set on a DependencyObject. So set it on the instance of the TextBlock using that style. That being said, a TranslationBinding can be used in a **ControlTemplate** that is used for a Template in a Setter. See example project.
  - If you need this kind of functionality then I suggest checking out my other translation API mentioned above.
- Translations are a runtime thing not a design time thing. So if you want to see something in the designer then enter a ***FallbackValue***.
- If the developer has the need to use a translation before the XAML processor/renderer processes the first TranslationBinding then the developer will have to call ***TranslationBindingOperations.ReadInTranslationsForCulture()*** manually for the API to read the translation. Call this after setting *CultureInfo.DefaultThreadCurrentCulture* or *CultureInfo.DefaultThreadCurrentUICulture* or both. The API will read in translations when the XAML processor/renderer processes the first instance of a TranslationBinding.

It all sounds kind of complicated but it is really not that complicated.

# Parameters
The API supports up to 3 parameters for translation bindings. Set the ***Parameter***, ***Parameter2*** or ***Parameter3*** properties as needed. The Parameter properties have to be a binding in XAML. 

```XAML
<TextBlock VerticalAlignment="Top"
           Text="{tb:TranslationBinding TranslationKey=Testing, FallbackValue=Testing parameters, 
                                        Parameter={Binding Version}}" />
```

It is suggested to not listen to the TextChanged, or similar, on controls using a TranslationBinding that has a Parameter property assigned. The bound property will change multiple times while evaluating Parameter bindings.

If something more complex is needed then it is up to the developer to retrieve the string manually and perform the custom format work then.

# From Code
Translations can be pulled in code, after they are read in, by calling ***TranslationBindingOperations.GetTranslation(string key)***. If the key is not found then **null** is returned.

# Examples
Be sure to check out the testing WPF application project in the repository for code examples. We will cover some examples at a high level.

### Implementing ITranslationProvider
Make a simple translation provider that reads XAML resource dictionaries for translations...

```C#
using WPF.Translations.TranslationBinding;

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
```

As you can see from the simple example, we are reading translations from our main entry assembly/application and from satelite assemblies. This is because my XAML resouce dictionaries are marked as "Resource" under properties and so I can use pack application strings to access them. Please look up WPF pack URIs if you don't know and would like to know more.

***This is amazing because now all those de, en, fr, it, zh-Hans, zh-Hant directories that have to get deployed with your applicaton can die!!!!!!!!!!! Translations can now be included natively in your application.*** This makes it so customers can't ruin your software by deleting the "de" directory and wonder why when they select German for the application it defaults back to English. :| Seriously... ... ...

### Application StartUp Event in App.xaml.cs
Subscribe to the **StartUp** event in ***App.xaml*** and set your TranslationProvider...

```C#
TranslationBindingOperations.TranslationProvider = new TranslationProvider();
```

Next deciding on whether or not you want to set the ***CultureInfo.DefaultThreadCurrentCulture*** or the ***CultureInfo.DefaultThreadCurrentUICulture*** or both. If you are going to set both then this point is moot, but if you are not setting both then this point matters. If you want to manage the UI thread culture then set ***TranslationBindingOperations.UseUICulture = true*** and set ***CultureInfo.DefaultThreadCurrentUICulture***. If you want to manage the thread culture then set ***CultureInfo.DefaultThreadCurrentCulture*** and leave the default false value for ***TranslationBindingOperations.UseUICulture***.

After that you also need to decide to handle the ***TranslationBindingOperations.CultureChange*** call yourself by calling ***TranslationBindingOperations.RefreshTranslations()*** or by setting ***TranslationBindingOperations.RefreshAutomatically*** to true. If using the automatic route, there is a timer that fires every half a second and if a culture changed is detected then the ***TranslationBindingOperations.CultureChange*** event is fired. So there may be a small delay when changing the culture with the automatic timer.

Reminder that if you need to potentially show translations before any XAML is processed then remember to call ***TranslationBindingOperations.ReadInTranslationsForCulture()*** after setting *CultureInfo.DefaultThreadCurrentCulture* or *CultureInfo.DefaultThreadCurrentUICulture* or both.
