using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Janggi.AI;

namespace Janggi.Core
{
    /// <summary>
    /// 지원 언어 목록
    /// </summary>
    public enum Language
    {
        Korean,
        English
    }

    /// <summary>
    /// Unity 공식 Localization 패키지(com.unity.localization) 및 CSV 기반의 다국어 매니저.
    /// JanggiStringTable (CSV) 원본 데이터를 실시간으로 조회하고 언어 변경 이벤트를 전달합니다.
    /// </summary>
    public static class LocalizationManager
    {
        public const string TableName = "JanggiStringTable";
        private const string LanguagePrefKey = "Janggi_Language_Setting";

        public static event Action OnLanguageChanged;

        /// <summary>
        /// 현재 선택된 언어를 반환합니다.
        /// </summary>
        public static Language CurrentLanguage
        {
            get
            {
                var selectedLocale = LocalizationSettings.SelectedLocale;
                if (selectedLocale != null && selectedLocale.Identifier.Code.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
                {
                    return Language.Korean;
                }
                return Language.English;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInit()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            ApplyInitialLocale();
        }

        private static void OnSelectedLocaleChanged(Locale locale)
        {
            OnLanguageChanged?.Invoke();
        }

        private static void ApplyInitialLocale()
        {
            // 1. PlayerPrefs에 저장된 언어 확인
            if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                string savedLang = PlayerPrefs.GetString(LanguagePrefKey);
                if (Enum.TryParse<Language>(savedLang, out var lang))
                {
                    SetLanguage(lang, savePref: false);
                    return;
                }
            }

            // 2. 시스템 기본 언어 감지
            if (Application.systemLanguage == SystemLanguage.Korean)
            {
                SetLanguage(Language.Korean, savePref: false);
            }
            else
            {
                SetLanguage(Language.English, savePref: false);
            }
        }

        public static void SetLanguage(Language lang, bool savePref = true)
        {
            if (savePref)
            {
                PlayerPrefs.SetString(LanguagePrefKey, lang.ToString());
                PlayerPrefs.Save();
            }

            string prefix = lang == Language.Korean ? "ko" : "en";
            var availableLocales = LocalizationSettings.AvailableLocales;
            if (availableLocales != null && availableLocales.Locales != null)
            {
                var targetLocale = availableLocales.Locales.Find(
                    l => l != null && l.Identifier.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                if (targetLocale != null && LocalizationSettings.SelectedLocale != targetLocale)
                {
                    LocalizationSettings.SelectedLocale = targetLocale;
                }
            }

            OnLanguageChanged?.Invoke();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == Language.Korean ? Language.English : Language.Korean);
        }

        public static string GetLanguageToggleLabel()
        {
            return CurrentLanguage == Language.Korean ? "🌐 언어: 한국어 (KO)" : "🌐 Language: English (EN)";
        }

        public static string GetHeaderLanguageToggleLabel()
        {
            return CurrentLanguage == Language.Korean ? "🌐 한국어" : "🌐 English";
        }

        /// <summary>
        /// JanggiStringTable (CSV 데이터)에서 현재 언어에 해당하는 텍스트를 조회합니다.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            try
            {
                if (LocalizationSettings.StringDatabase != null)
                {
                    var localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
                    if (!string.IsNullOrEmpty(localizedString) && !localizedString.StartsWith("No translation found"))
                    {
                        return localizedString;
                    }
                }
            }
            catch
            {
                // 로딩 지연 또는 미초기화 시 key 반환
            }

            return key;
        }

        /// <summary>
        /// JanggiStringTable (CSV 데이터)에서 포맷 인자를 적용하여 텍스트를 조회합니다.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string raw = Get(key);
            if (args == null || args.Length == 0) return raw;

            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }

        public static string GetPieceName(PieceType type, PlayerSide side = PlayerSide.Cho)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return side == PlayerSide.Han ? Get("Piece_Pawn_Han") : Get("Piece_Pawn");
                case PieceType.Horse:
                    return Get("Piece_Horse");
                case PieceType.Elephant:
                    return Get("Piece_Elephant");
                case PieceType.Cannon:
                    return Get("Piece_Cannon");
                case PieceType.Chariot:
                    return Get("Piece_Chariot");
                case PieceType.King:
                    return Get("Piece_King");
                case PieceType.Advisor:
                    return Get("Piece_Advisor");
                default:
                    return type.ToString();
            }
        }

        public static string GetDifficultyName(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return Get("Diff_Easy_Title");
                case AIDifficulty.Normal:
                    return Get("Diff_Normal_Title");
                case AIDifficulty.Hard:
                    return Get("Diff_Hard_Title");
                case AIDifficulty.Hell:
                    return Get("Diff_Hell_Title");
                default:
                    return difficulty.ToString();
            }
        }
    }
}
