using System;
using System.Globalization;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public interface ILocalizationService
{
    /// <summary>
    /// Gets the current UI culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the localized string for the given resource key.
    /// Falls back to the key itself when the key is missing.
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    /// Gets the localized string for the given key, applying format placeholders.
    /// </summary>
    string Format(string key, params object[] args);

    /// <summary>
    /// Switches the application language and notifies subscribers.
    /// </summary>
    void ChangeLanguage(AppLanguage language);

    /// <summary>
    /// Raised when the application language changes.
    /// </summary>
    event EventHandler? LanguageChanged;
}
