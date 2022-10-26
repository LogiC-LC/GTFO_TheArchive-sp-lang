﻿using CellMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Utilities;
using UnityEngine;
using TheArchive.Core.Models;
using static TheArchive.Utilities.Utils;
using static TheArchive.Utilities.SColorExtensions;
using TheArchive.Interfaces;
using TheArchive.Loader;
using static TheArchive.Features.Dev.ModSettings.PageSettingsData;
using static TheArchive.Features.Dev.ModSettings.SettingsCreationHelper;
#if IL2CPP && MelonLoader
using UnhollowerBaseLib.Attributes;
#endif
#if IL2CPP && BepInEx
using Il2CppInterop.Runtime.Attributes;
#endif

namespace TheArchive.Features.Dev
{
    [EnableFeatureByDefault, HideInModSettings]
    public class ModSettings : Feature
    {
        public override string Name => "Mod Settings (this)";

        public override bool RequiresRestart => true;

        public override string Group => FeatureGroups.Dev;

        public new static IArchiveLogger FeatureLogger { get; set; }

#if MONO
        private static readonly FieldAccessor<CM_PageSettings, eSettingsSubMenuId> A_CM_PageSettings_m_currentSubMenuId = FieldAccessor<CM_PageSettings, eSettingsSubMenuId>.GetAccessor("m_currentSubMenuId");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ResetAllValueHolders = MethodAccessor<CM_PageSettings>.GetAccessor("ResetAllValueHolders");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ShowSettingsWindow = MethodAccessor<CM_PageSettings>.GetAccessor("ShowSettingsWindow");
        private static readonly FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow> A_CM_SettingsEnumDropdownButton_m_popupWindow = FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow>.GetAccessor("m_popupWindow");
        private static readonly FieldAccessor<CM_SettingsInputField, int> A_CM_SettingsInputField_m_maxLen = FieldAccessor<CM_SettingsInputField, int>.GetAccessor("m_maxLen");
        private static readonly FieldAccessor<CM_SettingsInputField, iStringInputReceiver> A_CM_SettingsInputField_m_stringReceiver = FieldAccessor<CM_SettingsInputField, iStringInputReceiver>.GetAccessor("m_stringReceiver");
        private static readonly FieldAccessor<TMPro.TMP_Text, float> A_TMP_Text_m_marginWidth = FieldAccessor<TMPro.TMP_Text, float>.GetAccessor("m_marginWidth");
        
        private static readonly FieldAccessor<CM_PageSettings, List<CM_ScrollWindow>> A_CM_PageSettings_m_allSettingsWindows = FieldAccessor<CM_PageSettings, List<CM_ScrollWindow>>.GetAccessor("m_allSettingsWindows");
#endif
        private static MethodAccessor<TMPro.TextMeshPro> A_TextMeshPro_ForceMeshUpdate;

        public static Color ORANGE_WHITE = new Color(1f, 0.85f, 0.75f, 0.8f);
        public static Color ORANGE = new Color(1f, 0.5f, 0.05f, 0.8f);
        public static Color RED = new Color(0.8f, 0.1f, 0.1f, 0.8f);
        public static Color GREEN = new Color(0.1f, 0.8f, 0.1f, 0.8f);
        public static Color DISABLED = new Color(0.3f, 0.3f, 0.3f, 0.8f);


        public class JankTextMeshProUpdaterOnce : MonoBehaviour
        {
#if IL2CPP
            public JankTextMeshProUpdaterOnce(IntPtr ptr) : base(ptr) { }
#endif
            public void Awake()
            {
                UpdateMesh(this.GetComponent<TMPro.TextMeshPro>());
                Destroy(this);
            }

            public static void UpdateMesh(TMPro.TextMeshPro textMesh)
            {
                if (!BuildInfo.Rundown.IsIncludedIn(RundownFlags.RundownFour | RundownFlags.RundownFive)) return;
                if (textMesh == null) return;

                A_TextMeshPro_ForceMeshUpdate.Invoke(textMesh);
            }

#if IL2CPP
            [HideFromIl2Cpp]
#endif
            public static JankTextMeshProUpdaterOnce Apply(TMPro.TextMeshPro tmp)
            {
                if (BuildInfo.Rundown.IsIncludedIn(RundownFlags.RundownFour | RundownFlags.RundownFive))
                {
                    return tmp.gameObject.AddComponent<JankTextMeshProUpdaterOnce>();
                }
                return null;
            }
        }

        public class CustomStringReceiver
#if MONO
            : iStringInputReceiver
        {
            public CustomStringReceiver(Func<string> getFunc, Action<string> setAction)
            {
                _getValue = getFunc;
                _setValue = setAction;
            }
#else
            : Il2CppSystem.Object
        {
            public CustomStringReceiver(IntPtr ptr) : base(ptr)
            {
            }

            public CustomStringReceiver(Func<string> getFunc, Action<string> setAction) : base(LoaderWrapper.ClassInjector.DerivedConstructorPointer<CustomStringReceiver>())
            {
                LoaderWrapper.ClassInjector.DerivedConstructorBody(this);

                _getValue = getFunc;
                _setValue = setAction;
            }
#endif

            private Func<string> _getValue;
            private Action<string> _setValue;

            string
#if MONO
                iStringInputReceiver.
#endif
                GetStringValue(eCellSettingID setting)
            {
                return _getValue?.Invoke() ?? string.Empty;
            }

            string
#if MONO
                iStringInputReceiver.
#endif
                SetStringValue(eCellSettingID setting, string value)
            {
                _setValue.Invoke(value);
                return value;
            }

        }


        public override void Init()
        {
#if IL2CPP
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<JankTextMeshProUpdaterOnce>();

            LoaderWrapper.ClassInjector.RegisterTypeInIl2CppWithInterfaces<CustomStringReceiver>(true, typeof(iStringInputReceiver));
#endif

            A_TextMeshPro_ForceMeshUpdate = MethodAccessor<TMPro.TextMeshPro>.GetAccessor("ForceMeshUpdate", Array.Empty<Type>());
        }

        public override void OnEnable()
        {
            FeatureManager.Instance.OnFeatureRestartRequestChanged += OnFeatureRestartRequestChanged;
        }

        private void OnFeatureRestartRequestChanged(Feature feature, bool restartRequested)
        {
            CM_PageSettings_SetupPatch.SetRestartInfoText(FeatureManager.Instance.AnyFeatureRequestingRestart);
        }

        public static SubMenu CreateSubMenu(string title)
        {
            var subMenu = new SubMenu(title);

            return subMenu;
        }

        public class SubMenu
        {
            public SubMenu(string title)
            {
                Title = title;
                ScrollWindow = SettingsCreationHelper.CreateScrollWindow(title);
            }

            public string Title { get; private set; }
            public bool HasBeenBuilt { get; private set; }
            public Transform WindowTransform => ScrollWindow.transform;
            public CM_ScrollWindow ScrollWindow { get; private set; }
            private readonly List<iScrollWindowContent> _content = new List<iScrollWindowContent>();

            public bool AppendContent(iScrollWindowContent item)
            {
                if (HasBeenBuilt)
                    return false;
                _content.Add(item);
                return true;
            }

            public bool Build()
            {
                if (HasBeenBuilt)
                    return false;
                ScrollWindow.SetContentItems(_content.ToIL2CPPListIfNecessary());
#if IL2CPP
                SettingsPageInstance.m_allSettingsWindows.Add(ScrollWindow);
#else
                A_CM_PageSettings_m_allSettingsWindows.Get(SettingsPageInstance).Add(ScrollWindow);
#endif
                HasBeenBuilt = true;
                return true;
            }

            public void Show()
            {
                ShowScrollWindow(ScrollWindow);
            }
        }

        public static class PageSettingsData
        {
            internal static GameObject SettingsItemPrefab { get; set; }
            internal static CM_ScrollWindow MainModSettingsScrollWindow { get; set; }
            internal static List<iScrollWindowContent> ScrollWindowContentElements { get; set; }
            internal static Transform MainScrollWindowTransform { get; set; }
            internal static CM_ScrollWindow PopupWindow { get; set; }
            internal static CM_PageSettings SettingsPageInstance { get; set; }

            public static MainMenuGuiLayer MMGuiLayer { get; internal set; }
            public static GameObject ScrollWindowPrefab { get; internal set; }
            public static RectTransform MovingContentHolder { get; internal set; }
        }

        public static void ShowMainModSettingsWindow(int _)
        {
            ShowScrollWindow(MainModSettingsScrollWindow);
        }

        public static void ShowScrollWindow(CM_ScrollWindow window)
        {
            CM_PageSettings.ToggleAudioTestLoop(false);
            SettingsPageInstance.ResetAllInputFields();
#if IL2CPP
            SettingsPageInstance.ResetAllValueHolders();
            SettingsPageInstance.m_currentSubMenuId = eSettingsSubMenuId.None;
            SettingsPageInstance.ShowSettingsWindow(window);
#else
            A_CM_PageSettings_ResetAllValueHolders.Invoke(SettingsPageInstance);
            A_CM_PageSettings_m_currentSubMenuId.Set(SettingsPageInstance, eSettingsSubMenuId.None);
            A_CM_PageSettings_ShowSettingsWindow.Invoke(SettingsPageInstance, window);
#endif
        }

        //Setup(MainMenuGuiLayer guiLayer)
        [ArchivePatch(typeof(CM_PageSettings), "Setup")]
        public class CM_PageSettings_SetupPatch
        {
            private static TMPro.TextMeshPro _restartInfoText;

            public static void SetRestartInfoText(bool value)
            {
                if(value)
                {
                    SetRestartInfoText($"<color=red><b>Restart required for some settings to apply!</b></color>");
                }
                else
                {
                    SetRestartInfoText(string.Empty);
                }
            }

            public static void SetRestartInfoText(string str)
            {
                if (_restartInfoText == null) return;
                _restartInfoText.SetText(str);
                JankTextMeshProUpdaterOnce.UpdateMesh(_restartInfoText);
            }

#if IL2CPP
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer)
            {
                try
                {
                    var m_subMenuButtonPrefab = __instance.m_subMenuButtonPrefab;
                    var m_subMenuItemOffset = __instance.m_subMenuItemOffset;
                    var m_movingContentHolder = __instance.m_movingContentHolder;
                    var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
#else
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer, GameObject ___m_subMenuButtonPrefab, ref float ___m_subMenuItemOffset, List<CM_ScrollWindow> ___m_allSettingsWindows)
            {
                var m_subMenuButtonPrefab = ___m_subMenuButtonPrefab;
                var m_subMenuItemOffset = ___m_subMenuItemOffset;
                var m_movingContentHolder = __instance.m_movingContentHolder;
                var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
                try
                {
#endif
                    SettingsPageInstance = __instance;
                    PopupWindow = __instance.m_popupWindow;
                    SettingsItemPrefab = __instance.m_settingsItemPrefab;
                    ScrollWindowPrefab = m_scrollwindowPrefab;
                    MMGuiLayer = guiLayer;
                    MovingContentHolder = m_movingContentHolder;
                    if (ScrollWindowContentElements == null)
                    {
                        ScrollWindowContentElements = new List<iScrollWindowContent>();
                    }
                    ScrollWindowContentElements.Clear();

                    var title = "Mod Settings";

                    CM_Item mainModSettingsButton = guiLayer.AddRectComp(m_subMenuButtonPrefab, GuiAnchor.TopLeft, new Vector2(70f, m_subMenuItemOffset), m_movingContentHolder).TryCastTo<CM_Item>();

                    mainModSettingsButton.SetScaleFactor(0.85f);
                    mainModSettingsButton.UpdateColliderOffset();
#if IL2CPP
                    __instance.m_subMenuItemOffset -= 80f;
#else
                    ___m_subMenuItemOffset -= 80f;
#endif
                    mainModSettingsButton.SetText(title);

                    

                    SharedUtils.ChangeColorCMItem(mainModSettingsButton, Color.magenta);

                    MainModSettingsScrollWindow = SettingsCreationHelper.CreateScrollWindow(title);

                    MainScrollWindowTransform = MainModSettingsScrollWindow.transform;


                    TMPro.TextMeshPro scrollWindowHeaderTextTMP = MainModSettingsScrollWindow.GetComponentInChildren<TMPro.TextMeshPro>();

                    _restartInfoText = GameObject.Instantiate(scrollWindowHeaderTextTMP, scrollWindowHeaderTextTMP.transform.parent);

                    _restartInfoText.name = "ModSettings_RestartInfoText";
                    _restartInfoText.transform.localPosition += new Vector3(300, 0, 0);
                    _restartInfoText.SetText("");
                    _restartInfoText.text = "";
                    _restartInfoText.enableWordWrapping = false;
                    _restartInfoText.fontSize = 16;
                    _restartInfoText.fontSizeMin = 16;

                    JankTextMeshProUpdaterOnce.UpdateMesh(_restartInfoText);


                    var odereredGroups = FeatureManager.Instance.GroupedFeatures.OrderBy(kvp => kvp.Key);

                    foreach(var kvp in odereredGroups)
                    {
                        var groupName = kvp.Key;
                        var featureSet = kvp.Value.OrderBy(fs => fs.Name);

                        if (groupName == FeatureGroups.Dev && !Feature.DevMode)
                            continue;

                        if(!Feature.DevMode && featureSet.All(f => f.IsHidden))
                            continue;

                        // Title
                        CreateHeader(groupName);

                        foreach(var feature in featureSet)
                        {
                            SetupEntriesForFeature(feature);
                        }

                        // Spacer
                        CreateSpacer();
                    }

                    IEnumerable<Feature> features;
                    if(Feature.DevMode)
                    {
                        features = FeatureManager.Instance.RegisteredFeatures;
                    }
                    else
                    {
                        features = FeatureManager.Instance.RegisteredFeatures.Where(f => !f.IsHidden);
                    }

                    features = features.Where(f => !f.BelongsToGroup);

                    features = features.OrderBy(f => f.GetType().Assembly.GetName().Name + f.Name);

                    Assembly currentAsm = null;
                    bool createSpacer = false;
                    foreach (var feature in features)
                    {
                        var featureAsm = feature.GetType().Assembly;
                        if (featureAsm != currentAsm)
                        {
                            if(createSpacer)
                            {
                                CreateSpacer();
                            }
                            createSpacer = true;
                            var headerTitle = featureAsm.GetCustomAttribute<ModDefaultFeatureGroupName>()?.DefaultGroupName ?? featureAsm.GetName().Name;
                            CreateHeader(headerTitle);
                            currentAsm = featureAsm;
                        }
                        SetupEntriesForFeature(feature);
                    }

#if IL2CPP
                    __instance.m_allSettingsWindows.Add(MainModSettingsScrollWindow);
#else
                    ___m_allSettingsWindows.Add(MainModSettingsScrollWindow);
#endif
                    mainModSettingsButton.SetCMItemEvents(ShowMainModSettingsWindow);

                    MainModSettingsScrollWindow.SetContentItems(PageSettingsData.ScrollWindowContentElements.ToIL2CPPListIfNecessary(), 5);
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }

            private static void SetupEntriesForFeature(Feature feature)
            {
                if (!Feature.DevMode && feature.IsHidden) return;

                try
                {
                    string featureName;
                    Color? col = null;
                    if (feature.IsHidden)
                    {
                        featureName = $"[H] {feature.Name}";
                        col = DISABLED;
                    }
                    else
                    {
                        featureName = feature.Name;
                    }

                    if (feature.RequiresRestart)
                    {
                        featureName = $"<color=red>[!]</color> {featureName}";
                    }

                    SubMenu subMenu = null;
                    if(feature.PlaceSettingsInSubMenu)
                    {
                        subMenu = CreateSubMenu(featureName);
                        feature.TryStore("SubMenu", subMenu);
                        featureName = $"<u>{featureName}</u>";
                    }

                    CreateSettingsItem(featureName, out var cm_settingsItem, col);

                    CM_SettingsItem sub_cm_settingsItem = null;
                    CM_SettingsItem into_sub_cm_settingsItem = null;
                    CM_SettingsItem outof_sub_cm_settingsItem = null;
                    if (subMenu != null)
                    {
                        CreateSettingsItem("<<< Back <<<", out outof_sub_cm_settingsItem, RED, subMenu);
                        CreateSpacer(subMenu);
                        CreateSettingsItem(featureName, out sub_cm_settingsItem, ORANGE, subMenu);

                        CreateSettingsItem("> Settings", out into_sub_cm_settingsItem, col);
                    }

                    
                    SetupToggleButton(cm_settingsItem, out CM_Item toggleButton_cm_item, out var toggleButtonText);

                    CM_Item sub_toggleButton_cm_item = null;
                    TMPro.TextMeshPro sub_toggleButtonText = null;
                    if (subMenu != null)
                    {
                        SetupToggleButton(sub_cm_settingsItem, out sub_toggleButton_cm_item, out sub_toggleButtonText);

                        #region back-button
                        var outButtonItem = outof_sub_cm_settingsItem.gameObject.AddComponent<CM_Item>();

                        outof_sub_cm_settingsItem.ForcePopupLayer(true);
                        GameObject.Destroy(outof_sub_cm_settingsItem);

                        outButtonItem.SetCMItemEvents((_) => {
                            ShowMainModSettingsWindow(_);
                        });
                        #endregion back-button

                        SetupToggleButton(into_sub_cm_settingsItem, out var into_sub_toggleButton_cm_item, out var into_sub_toggleButtonText);

                        SharedUtils.ChangeColorCMItem(into_sub_toggleButton_cm_item, ORANGE);

                        into_sub_toggleButton_cm_item.SetCMItemEvents(delegate (int id) {
                            FeatureLogger.Debug($"Submenu opened: \"{subMenu.Title}\"");
                            subMenu.Show();
                        });

                        into_sub_cm_settingsItem.ForcePopupLayer(true);

                        into_sub_toggleButtonText.SetText("> ENTER <");
                        JankTextMeshProUpdaterOnce.Apply(into_sub_toggleButtonText);
                    }

                    if (feature.DisableModSettingsButton)
                    {
                        toggleButton_cm_item.gameObject.SetActive(false);
                        sub_toggleButton_cm_item?.gameObject.SetActive(false);
                    }

                    if (feature.IsAutomated || feature.DisableModSettingsButton)
                    {
                        toggleButton_cm_item.SetCMItemEvents(delegate (int id) { });
                        sub_toggleButton_cm_item?.SetCMItemEvents(delegate (int id) { });
                    }
                    else
                    {
                        var del = delegate (int id)
                        {
                            FeatureManager.ToggleFeature(feature);

                            SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);
                            if(sub_toggleButton_cm_item != null)
                                SetFeatureItemTextAndColor(feature, sub_toggleButton_cm_item, sub_toggleButtonText);
                        };

                        toggleButton_cm_item.SetCMItemEvents(del);
                        sub_toggleButton_cm_item?.SetCMItemEvents(del);
                    }
                    

                    SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);
                    if(sub_toggleButton_cm_item != null)
                        SetFeatureItemTextAndColor(feature, sub_toggleButton_cm_item, sub_toggleButtonText);

                    CreateRundownInfoTextForItem(cm_settingsItem, feature.AppliesToRundowns);
                    if(sub_cm_settingsItem != null)
                        CreateRundownInfoTextForItem(sub_cm_settingsItem, feature.AppliesToRundowns);

                    cm_settingsItem.ForcePopupLayer(true);
                    sub_cm_settingsItem?.ForcePopupLayer(true);

                    if (feature.HasAdditionalSettings)
                    {
                        // Create SubMenu for additional settings
                        foreach (var settingsHelper in feature.SettingsHelpers)
                        {
                            foreach (var setting in settingsHelper.Settings)
                            {
                                if (setting.HeaderAbove != null)
                                    CreateHeader(setting.HeaderAbove.Title, setting.HeaderAbove.Color.ToUnityColor(), setting.HeaderAbove.Bold, subMenu);
                                else if (setting.SeparatorAbove)
                                    CreateHeader("------------------------------", DISABLED, false, subMenu);
                                else if (setting.SpacerAbove)
                                    CreateSpacer(subMenu: subMenu);

                                if (setting.HideInModSettings && !Feature.DevMode)
                                {
                                    continue;
                                }

                                switch (setting)
                                {
                                    case EnumListSetting els:
                                        CreateEnumListSetting(els);
                                        break;
                                    case ColorSetting cs:
                                        CreateColorSetting(cs);
                                        break;
                                    case StringSetting ss:
                                        CreateStringSetting(ss);
                                        break;
                                    case BoolSetting bs:
                                        CreateBoolSetting(bs);
                                        break;
                                    case EnumSetting es:
                                        CreateEnumSetting(es);
                                        break;
                                    default:
                                        CreateHeader(setting.DEBUG_Path, subMenu: subMenu);
                                        break;
                                }
                            }
                        }
                    }

                    subMenu?.Build();
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }

            private static void SetupToggleButton(CM_SettingsItem cm_settingsItem, out CM_Item toggleButton_cm_item, out TMPro.TextMeshPro toggleButtonText)
            {
                CM_SettingsToggleButton cm_SettingsToggleButton = GOUtil.SpawnChildAndGetComp<CM_SettingsToggleButton>(cm_settingsItem.m_toggleInputPrefab, cm_settingsItem.m_inputAlign);

                toggleButton_cm_item = cm_SettingsToggleButton.gameObject.AddComponent<CM_Item>();

                Component.Destroy(cm_SettingsToggleButton);

                toggleButtonText = toggleButton_cm_item.GetComponentInChildren<TMPro.TextMeshPro>();

                var collider = toggleButton_cm_item.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(550, 50);
                collider.offset = new Vector2(250, -25);
            }
        }

        public static class SettingsCreationHelper
        {
            public static CM_ScrollWindow CreateScrollWindow(string title)
            {
                CM_ScrollWindow scrollWindow = MMGuiLayer.AddRectComp(ScrollWindowPrefab, GuiAnchor.TopLeft, new Vector2(420f, -200f), MovingContentHolder).TryCastTo<CM_ScrollWindow>();

                scrollWindow.Setup();
                scrollWindow.SetSize(new Vector2(1020f, 900f));
                scrollWindow.SetVisible(visible: false);
                scrollWindow.SetHeader(title);

                return scrollWindow;
            }

            public static void CreateSpacer(SubMenu subMenu = null)
            {
                CreateHeader(string.Empty, subMenu: subMenu);
            }

            public static void CreateHeader(string title, Color? color = null, bool bold = true, SubMenu subMenu = null)
            {
                if (!color.HasValue)
                    color = ORANGE;

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = string.Empty;
                }
                else
                {
                    if (bold)
                        title = $"<b>{title}</b>";
                }

                CreateSettingsItem(title, out var cm_settingsItem, color.Value, subMenu);
                
                if (!string.IsNullOrWhiteSpace(title))
                {
                    cm_settingsItem.ForcePopupLayer(true);
                }
            }

            public static void CreateSettingsItem(string titleText, out CM_SettingsItem cm_settingsItem, Color? titleColor = null, SubMenu subMenu = null)
            {
                var settingsItemGameObject = GameObject.Instantiate(SettingsItemPrefab, subMenu?.WindowTransform ?? MainScrollWindowTransform);

                if(subMenu != null)
                {
                    subMenu.AppendContent(settingsItemGameObject.GetComponentInChildren<iScrollWindowContent>());
                }
                else
                {
                    ScrollWindowContentElements.Add(settingsItemGameObject.GetComponentInChildren<iScrollWindowContent>());
                }

                cm_settingsItem = settingsItemGameObject.GetComponentInChildren<CM_SettingsItem>();

                var titleTextTMP = cm_settingsItem.transform.GetChildWithExactName("Title").GetChildWithExactName("TitleText").gameObject.GetComponent<TMPro.TextMeshPro>();

                if (titleColor.HasValue)
                    titleTextTMP.color = titleColor.Value;

#if IL2CPP
                titleTextTMP.m_text = titleText;
                titleTextTMP.SetText(titleText);
#else
                titleTextTMP.SetText(titleText);
#endif

                JankTextMeshProUpdaterOnce.Apply(titleTextTMP);
            }

            public static void CreateColorSetting(ColorSetting setting)
            {
                setting.Helper.Feature.TryRetrieve<SubMenu>("SubMenu", out var subMenu);

                CreateSettingsItem(GetNameForSetting(setting), out var cm_settingsItem, subMenu: subMenu);

                CreateRundownInfoTextForItem(cm_settingsItem, setting.RundownHint);

                CM_SettingsInputField cm_settingsInputField = GOUtil.SpawnChildAndGetComp<CM_SettingsInputField>(cm_settingsItem.m_textInputPrefab, cm_settingsItem.m_inputAlign);

                StringInputSetMaxLength(cm_settingsInputField, 7);

                var bg_rt = cm_settingsInputField.m_background.GetComponent<RectTransform>();
                bg_rt.anchoredPosition = new Vector2(-175, 0);
                bg_rt.localScale = new Vector3(0.19f, 1, 1);

                var colorPreviewGO = GameObject.Instantiate(cm_settingsInputField.m_background.gameObject, cm_settingsInputField.transform, true);
                colorPreviewGO.transform.SetParent(cm_settingsItem.m_inputAlign);
                colorPreviewGO.transform.localScale = new Vector3(0.07f, 1, 1);
                colorPreviewGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-225, -25);
                var renderer = colorPreviewGO.GetComponent<SpriteRenderer>();

                var scol = (SColor)setting.GetValue();

                renderer.color = scol.ToUnityColor();

                cm_settingsInputField.m_text.SetText(scol.ToHexString());

                JankTextMeshProUpdaterOnce.Apply(cm_settingsInputField.m_text);

                var receiver = new CustomStringReceiver(new Func<string>(
                    () => {
                        FeatureLogger.Debug($"[{nameof(CustomStringReceiver)}({nameof(ColorSetting)})] Gotten value of \"{setting.DEBUG_Path}\"!");
                        SColor color = (SColor)setting.GetValue();
                        renderer.color = color.ToUnityColor();
                        return color.ToHexString();
                    }),
                    (val) => {
                        FeatureLogger.Debug($"[{nameof(CustomStringReceiver)}({nameof(ColorSetting)})] Set value of \"{setting.DEBUG_Path}\" to \"{val}\"");
                        SColor color = SColorExtensions.FromHexString(val);
                        setting.SetValue(color);
                    });

#if IL2CPP
                cm_settingsInputField.m_stringReceiver = new iStringInputReceiver(receiver.Pointer);
#else
                A_CM_SettingsInputField_m_stringReceiver.Set(cm_settingsInputField, receiver);
#endif


                cm_settingsItem.ForcePopupLayer(true);
            }

            public static void CreateStringSetting(StringSetting setting)
            {
                setting.Helper.Feature.TryRetrieve<SubMenu>("SubMenu", out var subMenu);

                CreateSettingsItem(GetNameForSetting(setting), out var cm_settingsItem, subMenu: subMenu);

                CreateRundownInfoTextForItem(cm_settingsItem, setting.RundownHint);

                CM_SettingsInputField cm_settingsInputField = GOUtil.SpawnChildAndGetComp<CM_SettingsInputField>(cm_settingsItem.m_textInputPrefab, cm_settingsItem.m_inputAlign);

                StringInputSetMaxLength(cm_settingsInputField, setting.MaxInputLength);

                cm_settingsInputField.m_text.SetText(setting.GetValue()?.ToString() ?? string.Empty);

                JankTextMeshProUpdaterOnce.Apply(cm_settingsInputField.m_text);

                var receiver = new CustomStringReceiver(new Func<string>(
                    () => {
                        FeatureLogger.Debug($"[{nameof(CustomStringReceiver)}] Gotten value of \"{setting.DEBUG_Path}\"!");
                        return setting.GetValue()?.ToString() ?? string.Empty;
                    }),
                    (val) => {
                        FeatureLogger.Debug($"[{nameof(CustomStringReceiver)}] Set value of \"{setting.DEBUG_Path}\" to \"{val}\"");
                        setting.SetValue(val);
                    });

#if IL2CPP
                cm_settingsInputField.m_stringReceiver = new iStringInputReceiver(receiver.Pointer);
#else
                A_CM_SettingsInputField_m_stringReceiver.Set(cm_settingsInputField, receiver);
#endif

                cm_settingsItem.ForcePopupLayer(true);
            }

            public static void StringInputSetMaxLength(CM_SettingsInputField sif, int maxLength)
            {
#if MONO
                A_CM_SettingsInputField_m_maxLen.Set(sif, maxLength);
#else
                sif.m_maxLen = maxLength;
#endif
            }

            public static void CreateBoolSetting(BoolSetting setting)
            {
                setting.Helper.Feature.TryRetrieve<SubMenu>("SubMenu", out var subMenu);

                CreateSettingsItem(GetNameForSetting(setting), out var cm_settingsItem, subMenu: subMenu);

                CreateRundownInfoTextForItem(cm_settingsItem, setting.RundownHint);

                CM_SettingsToggleButton cm_SettingsToggleButton = GOUtil.SpawnChildAndGetComp<CM_SettingsToggleButton>(cm_settingsItem.m_toggleInputPrefab, cm_settingsItem.m_inputAlign);

                var toggleButton_cm_item = cm_SettingsToggleButton.gameObject.AddComponent<CM_Item>();



                Component.Destroy(cm_SettingsToggleButton);

                var toggleButtonText = toggleButton_cm_item.GetComponentInChildren<TMPro.TextMeshPro>();

                toggleButton_cm_item.SetCMItemEvents(delegate (int id) {
                    var val = !(bool)setting.GetValue();
                    setting.SetValue(val);

                    toggleButtonText.SetText(val ? "On" : "Off");
                    var color = val ? GREEN : RED;
                    SharedUtils.ChangeColorCMItem(toggleButton_cm_item, color);
                });

                var currentValue = (bool)setting.GetValue();
                toggleButtonText.SetText(currentValue ? "On" : "Off");
                var col = currentValue ? GREEN : RED;
                SharedUtils.ChangeColorCMItem(toggleButton_cm_item, col);

                cm_settingsItem.ForcePopupLayer(true);
            }

            public static void CreateEnumListSetting(EnumListSetting setting)
            {
                setting.Helper.Feature.TryRetrieve<SubMenu>("SubMenu", out var subMenu);

                CreateSettingsItem(GetNameForSetting(setting), out var cm_settingsItem, subMenu: subMenu);

                CreateRundownInfoTextForItem(cm_settingsItem, setting.RundownHint);

                CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton = GOUtil.SpawnChildAndGetComp<CM_SettingsEnumDropdownButton>(cm_settingsItem.m_enumDropdownInputPrefab, cm_settingsItem.m_inputAlign);

                var enumButton_cm_item = cm_settingsEnumDropdownButton.gameObject.AddComponent<CM_Item>();

                enumButton_cm_item.Setup();
                enumButton_cm_item.SetCMItemEvents(delegate (int _) {
                    CreateAndShowEnumListPopup(setting, enumButton_cm_item, cm_settingsEnumDropdownButton);
                });

                SharedUtils.ChangeColorCMItem(enumButton_cm_item, ORANGE);
                var bg = enumButton_cm_item.gameObject.transform.GetChildWithExactName("Background");
                if (bg != null)
                {
                    UnityEngine.Object.Destroy(bg.gameObject);
                }
                enumButton_cm_item.SetText(GetEnumListItemName(setting));

                var collider = enumButton_cm_item.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(550, 50);
                collider.offset = new Vector2(250, -25);

                cm_settingsItem.ForcePopupLayer(true);

                cm_settingsEnumDropdownButton.enabled = false;
                UnityEngine.Object.Destroy(cm_settingsEnumDropdownButton);

#if MONO
                A_CM_SettingsEnumDropdownButton_m_popupWindow.Set(cm_settingsEnumDropdownButton, PopupWindow);
#else
                cm_settingsEnumDropdownButton.m_popupWindow = PopupWindow;
#endif

            }

            public static void CreateAndShowEnumListPopup(EnumListSetting setting, CM_Item enumButton_cm_item, CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton)
            {
#if MONO
                List<iScrollWindowContent> list = new List<iScrollWindowContent>();
#else
                Il2CppSystem.Collections.Generic.List<iScrollWindowContent> list = new Il2CppSystem.Collections.Generic.List<iScrollWindowContent>();
#endif
                var currentValues = setting.CurrentSelectedValues();

                var allCMItems = new List<CM_Item>();

                foreach (var kvp in setting.Map)
                {
                    iScrollWindowContent iScrollWindowContent = GOUtil.SpawnChildAndGetComp<iScrollWindowContent>(cm_settingsEnumDropdownButton.m_popupItemPrefab, enumButton_cm_item.transform);
                    list.Add(iScrollWindowContent);
                    string enumKey = kvp.Key;
                    object enumValue = kvp.Value;

                    CM_Item cm_Item = iScrollWindowContent.TryCastTo<CM_Item>();

                    if (cm_Item != null)
                    {
                        cm_Item.Setup();
                        cm_Item.SetText(enumKey);
                        //cm_Item.SetAnchor(GuiAnchor.TopLeft, true);
                        cm_Item.SetScaleFactor(1f);

                        cm_Item.name = enumKey;

                        SharedUtils.ChangeColorCMItem(cm_Item, currentValues.Contains(enumValue) ? ORANGE : DISABLED);

                        cm_Item.ForcePopupLayer(true);

                        allCMItems.Add(cm_Item);
                    }
                }

                foreach (var cm_Item in allCMItems)
                {
                    cm_Item.SetCMItemEvents((_) =>
                    {
                        var enumKey = cm_Item.name;
                        var value = setting.GetEnumValueFor(enumKey);
                        var enabled = setting.ToggleInList(value);

                        enumButton_cm_item.SetText(GetEnumListItemName(setting));
                        SharedUtils.ChangeColorCMItem(cm_Item, enabled ? ORANGE : DISABLED);
                    });
                }

#if MONO
                PopupWindow.SetupFromButton(cm_settingsEnumDropdownButton as iCellMenuPopupController, SettingsPageInstance);
#else
                PopupWindow.SetupFromButton(cm_settingsEnumDropdownButton.TryCast<iCellMenuPopupController>(), SettingsPageInstance);
#endif
                PopupWindow.transform.position = cm_settingsEnumDropdownButton.m_popupWindowAlign.position;
                PopupWindow.SetContentItems(list, 5f);
                PopupWindow.SetHeader(setting.DisplayName);
                PopupWindow.SetVisible(true);
            }

            public static string GetEnumListItemName(EnumListSetting setting)
            {
                var str = string.Join(", ", setting.CurrentSelectedValues());

                if (string.IsNullOrWhiteSpace(str))
                {
                    return "[None]";
                }

                if (str.Length > 36)
                {
                    return str.Substring(0, 36) + " ...";
                }

                return str;
            }

            public static void CreateEnumSetting(EnumSetting setting)
            {
                setting.Helper.Feature.TryRetrieve<SubMenu>("SubMenu", out var subMenu);

                CreateSettingsItem(GetNameForSetting(setting), out var cm_settingsItem, subMenu: subMenu);

                CreateRundownInfoTextForItem(cm_settingsItem, setting.RundownHint);

                CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton = GOUtil.SpawnChildAndGetComp<CM_SettingsEnumDropdownButton>(cm_settingsItem.m_enumDropdownInputPrefab, cm_settingsItem.m_inputAlign);

                var enumButton_cm_item = cm_settingsEnumDropdownButton.gameObject.AddComponent<CM_Item>();

                enumButton_cm_item.Setup();
                enumButton_cm_item.SetCMItemEvents(delegate (int _) {
                    CreateAndShowEnumPopup(setting, enumButton_cm_item, cm_settingsEnumDropdownButton);
                });

                SharedUtils.ChangeColorCMItem(enumButton_cm_item, ORANGE);
                var bg = enumButton_cm_item.gameObject.transform.GetChildWithExactName("Background");
                if (bg != null)
                {
                    UnityEngine.Object.Destroy(bg.gameObject);
                }
                enumButton_cm_item.SetText(setting.GetCurrentEnumKey());

                var collider = enumButton_cm_item.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(550, 50);
                collider.offset = new Vector2(250, -25);

                cm_settingsItem.ForcePopupLayer(true);

                cm_settingsEnumDropdownButton.enabled = false;
                UnityEngine.Object.Destroy(cm_settingsEnumDropdownButton);

#if MONO
                A_CM_SettingsEnumDropdownButton_m_popupWindow.Set(cm_settingsEnumDropdownButton, PopupWindow);
#else
                cm_settingsEnumDropdownButton.m_popupWindow = PopupWindow;
#endif

            }

            public static void CreateAndShowEnumPopup(EnumSetting setting, CM_Item enumButton_cm_item, CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton)
            {
                var list = SharedUtils.NewListForGame<iScrollWindowContent>();

                var currentKey = setting.GetCurrentEnumKey();

                foreach (var kvp in setting.Map)
                {
                    iScrollWindowContent iScrollWindowContent = GOUtil.SpawnChildAndGetComp<iScrollWindowContent>(cm_settingsEnumDropdownButton.m_popupItemPrefab, enumButton_cm_item.transform);
                    list.Add(iScrollWindowContent);
                    string enumKey = kvp.Key;

                    CM_Item cm_Item = iScrollWindowContent.TryCastTo<CM_Item>();

                    if (cm_Item != null)
                    {
                        cm_Item.Setup();
                        cm_Item.SetText(enumKey);

                        cm_Item.SetScaleFactor(1f);

                        SharedUtils.ChangeColorCMItem(cm_Item, currentKey == enumKey ? ORANGE : DISABLED);

                        cm_Item.ForcePopupLayer(true);

                        cm_Item.SetCMItemEvents((_) => {
                            setting.SetValue(kvp.Value);

                            enumButton_cm_item.SetText(enumKey);

                            SetPopupVisible(false);
                        });
                    }
                }

                PopupWindow.SetupFromButton(cm_settingsEnumDropdownButton.CastTo<iCellMenuPopupController>(), SettingsPageInstance);

                PopupWindow.transform.position = cm_settingsEnumDropdownButton.m_popupWindowAlign.position;
                PopupWindow.SetContentItems(list, 5f);
                PopupWindow.SetHeader(setting.DisplayName);
                PopupWindow.SetVisible(true);
            }

            public static void CreateRundownInfoTextForItem(CM_SettingsItem cm_settingsItem, RundownFlags rundowns)
            {
                var rundownInfoButton = GOUtil.SpawnChildAndGetComp<CM_SettingsToggleButton>(cm_settingsItem.m_toggleInputPrefab, cm_settingsItem.m_inputAlign);
                var rundownInfoTMP = rundownInfoButton.GetComponentInChildren<TMPro.TextMeshPro>();
                JankTextMeshProUpdaterOnce.Apply(rundownInfoTMP);
                UnityEngine.Object.Destroy(rundownInfoButton);
                UnityEngine.Object.Destroy(rundownInfoButton.GetComponent<BoxCollider2D>());


                rundownInfoTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 50);

                if (rundowns == RundownFlags.None)
                {
                    rundownInfoTMP.SetText(string.Empty);
                }
                else
                {
                    Enum.TryParse<RundownID>(rundowns.LowestRundownFlag().ToString(), out var lowestId);
                    Enum.TryParse<RundownID>(rundowns.HighestRundownFlag().ToString(), out var highestId);


                    if (lowestId == highestId)
                    {
                        rundownInfoTMP.SetText($"<align=right>R{(int)lowestId}</align>");
                    }
                    else
                    {
                        rundownInfoTMP.SetText($"<align=right>R{(int)lowestId}-R{(int)highestId}</align>");
                    }
                }

                rundownInfoTMP.color = ORANGE;
            }

            public static void SetFeatureItemTextAndColor(Feature feature, CM_Item buttonItem, TMPro.TextMeshPro text)
            {
                if (feature.IsAutomated)
                {
                    text.SetText("Automated");
                    SharedUtils.ChangeColorCMItem(buttonItem, DISABLED);
                    return;
                }
                bool enabled = (feature.AppliesToThisGameBuild && !feature.RequiresRestart) ? feature.Enabled : FeatureManager.IsEnabledInConfig(feature);
                text.SetText(enabled ? "Enabled" : "Disabled");

                SetFeatureItemColor(feature, buttonItem);

                JankTextMeshProUpdaterOnce.UpdateMesh(text);
            }

            public static void SetFeatureItemColor(Feature feature, CM_Item item)
            {
                if (!feature.AppliesToThisGameBuild)
                {
                    SharedUtils.ChangeColorCMItem(item, DISABLED);
                    return;
                }

                Color col;

                if (feature.RequiresRestart)
                {
                    col = FeatureManager.IsEnabledInConfig(feature) ? GREEN : RED;
                    SharedUtils.ChangeColorCMItem(item, col);
                    return;
                }

                col = feature.Enabled ? GREEN : RED;
                SharedUtils.ChangeColorCMItem(item, col);
            }

            public static void ShowPopupWindow(string header, Vector2 pos)
            {
                PopupWindow.SetHeader(header);
                PopupWindow.transform.position = pos;
                PopupWindow.SetVisible(true);
            }

            public static void SetPopupVisible(bool visible)
            {
                PopupWindow.SetVisible(visible);
            }

            public static string GetNameForSetting(FeatureSetting setting)
            {
                if (setting.HideInModSettings)
                    return $"<{DISABLED.ToSColor().ToHexString()}>[H] {setting.DisplayName}</color>";

                return setting.DisplayName;
            }
        }
    }
}
