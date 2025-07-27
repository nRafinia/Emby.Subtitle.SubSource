using System;
using MediaBrowser.Model.Globalization;

namespace Emby.Subtitle.SubSource.Helpers
{
    public static class LanguageExt
    {
        public static string GetIsoLanguage(this ILocalizationManager localizationManager, string language)
        {
            if (!string.IsNullOrWhiteSpace(language))
            {
                return language;
            }

            var culture = localizationManager?.FindLanguageInfo(language.AsSpan());
            return culture != null
                ? culture.ThreeLetterISOLanguageName
                : language;
        }
        
        public static string MapFromEmbyLanguage(this string embyLanguage)
        {
            switch (embyLanguage.ToLower())
            {
                case "fa":
                case "per":
                    embyLanguage = "farsi_persian";
                    break;
                case "ar":
                case "ara":
                    embyLanguage = "arabic";
                    break;
                case "en":
                case "eng":
                    embyLanguage = "english";
                    break;
                case "my":
                case "bur":
                    embyLanguage = "burmese";
                    break;
                case "da":
                case "dan":
                    embyLanguage = "danish";
                    break;
                case "nl":
                case "dut":
                    embyLanguage = "dutch";
                    break;
                case "he":
                case "heb":
                    embyLanguage = "hebrew";
                    break;
                case "id":
                case "ind":
                    embyLanguage = "indonesian";
                    break;
                case "ko":
                case "kor":
                    embyLanguage = "korean";
                    break;
                case "ms":
                case "may":
                    embyLanguage = "malay";
                    break;
                case "es":
                case "spa":
                    embyLanguage = "spanish";
                    break;
                case "vi":
                case "vie":
                    embyLanguage = "vietnamese";
                    break;
                case "tr":
                case "tur":
                    embyLanguage = "turkish";
                    break;
                case "bn":
                case "ben":
                    embyLanguage = "bengali";
                    break;
                case "bg":
                case "bul":
                    embyLanguage = "bulgarian";
                    break;
                case "hr":
                case "hrv":
                    embyLanguage = "croatian";
                    break;
                case "fi":
                case "fin":
                    embyLanguage = "finnish";
                    break;
                case "fr":
                case "fre":
                    embyLanguage = "french";
                    break;
                case "de":
                case "ger":
                    embyLanguage = "german";
                    break;
                case "el":
                case "gre":
                    embyLanguage = "greek";
                    break;
                case "hu":
                case "hun":
                    embyLanguage = "hungarian";
                    break;
                case "it":
                case "ita":
                    embyLanguage = "italian";
                    break;
                case "ku":
                case "kur":
                    embyLanguage = "kurdish";
                    break;
                case "mk":
                case "mac":
                    embyLanguage = "macedonian";
                    break;
                case "ml":
                case "mal":
                    embyLanguage = "malayalam";
                    break;
                case "nn":
                case "nno":
                case "nb":
                case "nob":
                case "no":
                case "nor":
                    embyLanguage = "norwegian";
                    break;
                case "pt":
                case "por":
                    embyLanguage = "portuguese";
                    break;
                case "ru":
                case "rus":
                    embyLanguage = "russian";
                    break;
                case "sr":
                case "srp":
                    embyLanguage = "serbian";
                    break;
                case "si":
                case "sin":
                    embyLanguage = "sinhala";
                    break;
                case "sl":
                case "slv":
                    embyLanguage = "slovenian";
                    break;
                case "sv":
                case "swe":
                    embyLanguage = "swedish";
                    break;
                case "th":
                case "tha":
                    embyLanguage = "thai";
                    break;
                case "ur":
                case "urd":
                    embyLanguage = "urdu";
                    break;
                case "pt-br":
                case "pob":
                    embyLanguage = "brazillian-portuguese";
                    break;
            }

            return embyLanguage;
        }
    }
}