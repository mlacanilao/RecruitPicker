using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;

namespace RecruitPicker
{
    internal static class RecruitPickerConfig
    {
        private static ConfigEntry<string> _selectedRecruitsEntry;
        
        public static string XmlPath { get; private set; }
        public static string TranslationXlsxPath { get; private set; }
        
        internal static List<string> SelectedRecruits =>
            _selectedRecruitsEntry?.Value.Split(separator: ',')
                .Select(selector: id => id.Trim())
                .Where(predicate: id => !string.IsNullOrEmpty(value: id))
                .ToList() ?? new List<string>();
        
        internal static void UpdateSelectedRecruits(List<string> selectedRecruits)
        {
            if (_selectedRecruitsEntry != null)
            {
                _selectedRecruitsEntry.Value = string.Join(separator: ",", values: selectedRecruits);
            }
        }
        
        internal static void LoadConfig(ConfigFile config)
        {
            _selectedRecruitsEntry = config.Bind(
                section: ModInfo.Name,
                key: "SelectedRecruits",
                defaultValue: string.Empty,
                description: "Comma-separated list of recruit IDs to use for configuring the recruit picker.\n" +
                             "リクルートピッカーを設定するために使用するリクルートIDのカンマ区切りリストです。\n" +
                             "用于配置招募选择器的招募ID的逗号分隔列表。"
            );
        }
        
        public static void InitializeXmlPath(string xmlPath)
        {
            if (File.Exists(path: xmlPath))
            {
                XmlPath = xmlPath;
            }
            else
            {
                XmlPath = string.Empty;
            }
        }
        
        public static void InitializeTranslationXlsxPath(string xlsxPath)
        {
            if (File.Exists(path: xlsxPath))
            {
                TranslationXlsxPath = xlsxPath;
            }
            else
            {
                TranslationXlsxPath = string.Empty;
            }
        }
    }
}
