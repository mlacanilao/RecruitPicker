using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;

namespace RecruitPicker.UI
{
    public class UIController
    {
        private static Dictionary<string, string> defaultAkaToIdMap = new Dictionary<string, string>();
        private static Dictionary<string, string> cheatAkaToIdMap = new Dictionary<string, string>();
        
        public static void RegisterUI()
        {
            foreach (var obj in ModManager.ListPluginObject)
            {
                if (obj is BaseUnityPlugin plugin && plugin.Info.Metadata.GUID == ModInfo.ModOptionsGuid)
                {
                    var controller = ModOptionController.Register(guid: ModInfo.Guid, tooptipId: "mod.tooltip");
                    
                    var assemblyLocation = Path.GetDirectoryName(path: Assembly.GetExecutingAssembly().Location);
                    var xmlPath = Path.Combine(path1: assemblyLocation, path2: "RecruitPickerConfig.xml");
                    RecruitPickerConfig.InitializeXmlPath(xmlPath: xmlPath);
            
                    var xlsxPath = Path.Combine(path1: assemblyLocation, path2: "translations.xlsx");
                    RecruitPickerConfig.InitializeTranslationXlsxPath(xlsxPath: xlsxPath);
                    
                    if (File.Exists(path: RecruitPickerConfig.XmlPath))
                    {
                        using (StreamReader sr = new StreamReader(path: RecruitPickerConfig.XmlPath))
                            controller.SetPreBuildWithXml(xml: sr.ReadToEnd());
                    }
                    
                    if (File.Exists(path: RecruitPickerConfig.TranslationXlsxPath))
                    {
                        controller.SetTranslationsFromXslx(path: RecruitPickerConfig.TranslationXlsxPath);
                    }
                    
                    SetTranslations(controller: controller);
                    RegisterEvents(controller: controller);
                }
            }
        }

        private static void SetTranslations(ModOptionController controller)
        {
            controller.OnBuildUI += builder =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    string textId = $"text{i:D2}";
            
                    var text = builder.GetPreBuild<OptLabel>(id: textId);
                    if (text != null)
                    {
                        text.Align = TextAnchor.UpperLeft;
                    }
                }
            };
        }

        private static void RegisterEvents(ModOptionController controller)
        {
            controller.OnBuildUI += builder =>
            {
                var dropdown01 = PopulateDropdownWithSelectedRecruits(builder: builder, dropdownId: "dropdown01");
                var button01 = builder.GetPreBuild<OptButton>(id: "button01");
                button01.OnClicked += () =>
                {
                    RecruitPickerConfig.UpdateSelectedRecruits(selectedRecruits: new List<string>());

                    if (dropdown01 != null)
                    {
                        dropdown01.Base.options.Clear();
                        dropdown01.Base.RefreshShownValue();
                    }
                };
                
                var dropdown02 = PopulateDropdown(
                    builder: builder,
                    dropdownId: "dropdown02",
                    spawnListId: "c_neutral",
                    targetMap: defaultAkaToIdMap);
                
                var button02 = builder.GetPreBuild<OptButton>(id: "button02");
                ConfigureButton(builder: builder, button: button02, dropdown: dropdown02, targetMap: defaultAkaToIdMap);
                
                var dropdown03 = PopulateDropdown(
                    builder: builder,
                    dropdownId: "dropdown03",
                    targetMap: cheatAkaToIdMap);
                
                var button03 = builder.GetPreBuild<OptButton>(id: "button03");
                ConfigureButton(builder: builder, button: button03, dropdown: dropdown03, targetMap: cheatAkaToIdMap);
            };
        }
        
        private static OptDropdown PopulateDropdownWithSelectedRecruits(OptionUIBuilder builder, string dropdownId)
        {
            try
            {
                var dropdown = builder.GetPreBuild<OptDropdown>(id: dropdownId);

                if (dropdown == null)
                {
                    RecruitPicker.Log(payload: $"Dropdown with ID '{dropdownId}' not found.");
                    return null;
                }

                var selectedRecruits = RecruitPickerConfig.SelectedRecruits;

                dropdown.Base.options.Clear();

                foreach (var recruitId in selectedRecruits)
                {
                    dropdown.Base.options.Add(item: new UnityEngine.UI.Dropdown.OptionData(text: recruitId));
                }

                dropdown.Base.RefreshShownValue();
                return dropdown;
            }
            catch (Exception ex)
            {
                RecruitPicker.Log(payload: $"Error populating dropdown '{dropdownId}': {ex.Message}");
                return null;
            }
        }
        
        private static OptDropdown PopulateDropdown(OptionUIBuilder builder, string dropdownId, 
            Dictionary<string, string> targetMap, string spawnListId = null)
        {
            try
            {
                var dropdown = builder.GetPreBuild<OptDropdown>(id: dropdownId);

                if (dropdown == null)
                {
                    RecruitPicker.Log(payload: $"Dropdown with ID '{dropdownId}' not found.");
                    return null;
                }

                List<SourceChara.Row> characterRows = null;
                if (string.IsNullOrEmpty(value: spawnListId))
                {
                    characterRows = EClass.sources?.charas?.rows;
                    if (characterRows == null || characterRows.Count == 0)
                    {
                        RecruitPicker.Log(payload: "No character rows found in EClass.sources.charas.rows.");
                        return null;
                    }
                }
                else
                {
                    SpawnList spawnList = SpawnList.Get(id: spawnListId);
                    characterRows = spawnList?.rows?.OfType<SourceChara.Row>().ToList();
                    if (characterRows == null || characterRows.Count == 0)
                    {
                        RecruitPicker.Log(payload: $"No character rows found in spawn list '{spawnListId}'.");
                        return null;
                    }
                }
                
                foreach (var row in characterRows)
                {
                    string localizedText = GetLocalizedText(row: row);
                    string characterId = row.id;

                    if (!string.IsNullOrEmpty(value: localizedText) && !string.IsNullOrEmpty(value: characterId))
                    {
                        targetMap[key: localizedText] = characterId;
                    }
                }

                dropdown.Base.options.Clear();
                
                foreach (var aka in targetMap.Keys)
                {
                    dropdown.Base.options.Add(item: new UnityEngine.UI.Dropdown.OptionData(text: aka));
                }
                
                dropdown.Base.RefreshShownValue();
                return dropdown;
            }
            catch (Exception ex)
            {
                RecruitPicker.Log(payload: $"Error populating dropdown '{dropdownId}' with spawn list '{spawnListId}': {ex.Message}");
                return null;
            }
        }
        
        private static string GetLocalizedText(SourceChara.Row row)
        {
            if (row == null)
            {
                RecruitPicker.Log(payload: "Row is null; returning null for localized text.");
                return null;
            }

            string name;
            string aka;

            switch (Lang.langCode)
            {
                case "JP":
                    name = string.IsNullOrEmpty(value: row.name_JP) || row.name_JP == "*r" ? string.Empty : row.name_JP;
                    aka = row.aka_JP ?? row.aka;
                    break;

                case "CN":
                    name = string.IsNullOrEmpty(value: row.name) || row.name == "*r" ? string.Empty : row.GetText(id: "name", returnNull: false);
                    aka = row.GetText(id: "aka", returnNull: false);
                    break;

                case "EN":
                default:
                    name = string.IsNullOrEmpty(value: row.name) || row.name == "*r" ? string.Empty : row.name;
                    aka = row.aka;
                    if (Lang.langCode != "EN")
                    {
                        RecruitPicker.Log(payload: $"Unsupported language '{Lang.langCode}'; defaulting to English.");
                    }
                    break;
            }
            
            string namePart = !string.IsNullOrEmpty(name) ? $"「{name}」" : string.Empty;
            string akaPart = aka ?? string.Empty;
            string idPart = $"({row.id})";
            string result = $"{akaPart} {namePart} {idPart}".Trim();
            return result;
        }
        
        private static void ConfigureButton(OptionUIBuilder builder, OptButton button, OptDropdown dropdown,
            Dictionary<string, string> targetMap)
        {
            if (button == null || dropdown == null)
            {
                return;
            }

            button.OnClicked += () =>
            {
                if (dropdown.Base.options.Count == 0)
                {
                    return;
                }

                int selectedIndex = dropdown.Base.value;
                string selectedAka = dropdown.Base.options[index: selectedIndex].text;

                if (targetMap.TryGetValue(key: selectedAka, value: out string selectedId))
                {
                    List<string> selectedRecruits = RecruitPickerConfig.SelectedRecruits;
                    if (!selectedRecruits.Contains(item: selectedId))
                    {
                        selectedRecruits.Add(item: selectedId);
                        RecruitPickerConfig.UpdateSelectedRecruits(selectedRecruits: selectedRecruits);
                    }
                }
                else
                {
                    RecruitPicker.Log(payload: $"Failed to find ID for aka '{selectedAka}'.");
                }

                PopulateDropdownWithSelectedRecruits(builder: builder, dropdownId: "dropdown01");
            };
        }
    }
}