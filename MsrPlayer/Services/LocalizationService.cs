using System;
using System.Globalization;
using System.Resources;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _manager =
        new ResourceManager("MsrPlayer.Resources.Strings", typeof(LocalizationService).Assembly);

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    public event EventHandler? LanguageChanged;

    public string this[string key]
    {
        get
        {
            var value = _manager.GetString(key, CurrentCulture);
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CurrentCulture, this[key], args);
    }

    public void ChangeLanguage(AppLanguage language)
    {
        CurrentCulture = language switch
        {
            AppLanguage.ChineseSimplified => CultureInfo.GetCultureInfo("zh-CN"),
            _ => CultureInfo.GetCultureInfo("en-US")
        };

        CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
