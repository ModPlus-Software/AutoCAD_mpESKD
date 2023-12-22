namespace mpESKD;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Base;
using Base.Abstractions;
using Base.Utils;
using Functions.mpAxis;
using Functions.mpBreakLine;
using Functions.mpChainLeader;
using Functions.mpFragmentMarker;
using Functions.mpGroundLine;
using Functions.mpLetterLine;
using Functions.mpLevelMark;
using Functions.mpLevelPlanMark;
using Functions.mpNodalLeader;
using Functions.mpSecantNodalLeader;
using Functions.mpViewLabel;
using Functions.mpWaterProofing;
using Functions.mpWeldJoint;
using ModPlus.Helpers;
using ModPlusAPI;
using ModPlusAPI.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

/// <summary>
/// Методы построения ленты
/// </summary>
public class RibbonBuilder
{
    private static bool _wasActive;
    private static int _colorTheme = 1;

    /// <summary>
    /// Построить вкладку ЕСКД на ленте
    /// </summary>
    public static void BuildRibbon()
    {
        if (!IsLoaded())
        {
            GetColorTheme();
            CreateRibbon();
            Application.SystemVariableChanged -= AcadApp_SystemVariableChanged;
            Application.SystemVariableChanged += AcadApp_SystemVariableChanged;
        }
    }

    /// <summary>
    /// Удалить вкладку ЕСКД с ленты
    /// </summary>
    public static void RemoveRibbon()
    {
        try
        {
            if (IsLoaded())
            {
                var ribbonControl = ComponentManager.Ribbon;
                var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
                var tab = ribbonControl.Tabs.FirstOrDefault(tab =>
                    !string.IsNullOrEmpty(tab.Id) && tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName));
                if (tab != null)
                {
                    ribbonControl.Tabs.Remove(tab);
                    Application.SystemVariableChanged -= AcadApp_SystemVariableChanged;
                }
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private static bool IsLoaded()
    {
        var ribbonControl = ComponentManager.Ribbon;
        var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
        return ribbonControl.Tabs.Any(tab => !string.IsNullOrEmpty(tab.Id) && tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName));
    }

    private static bool IsActive()
    {
        var ribbonControl = ComponentManager.Ribbon;
        var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
        foreach (var tab in ribbonControl.Tabs)
        {
            if (!string.IsNullOrEmpty(tab.Id) && tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName))
            {
                return tab.IsActive;
            }
        }

        return false;
    }

    private static void AcadApp_SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
    {
        if (e.Name.Equals("WSCURRENT"))
        {
            BuildRibbon();
        }

        if (e.Name.Equals("COLORTHEME"))
        {
            _wasActive = IsActive();
            RemoveRibbon();
            BuildRibbon();
        }
    }

    private static void GetColorTheme()
    {
        try
        {
            // 0 - dark, 1 - light
            var sv = Application.GetSystemVariable("COLORTHEME").ToString();
            _colorTheme = int.TryParse(sv, out var i) ? i : 1;
        }
        catch
        {
            _colorTheme = 1;
        }
    }

    private static void CreateRibbon()
    {
        try
        {
            var ribbonControl = ComponentManager.Ribbon;

            // add the tab
            var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
            var ribbonTab = new RibbonTab { Title = tabName, Id = "ModPlus_ESKD" };
            ribbonControl.Tabs.Add(ribbonTab);

            // add content
            AddAxisPanel(ribbonTab);
            AddLeadersPanel(ribbonTab);
            AddLevelMarksPanel(ribbonTab);
            AddMarksPanel(ribbonTab);
            AddLinesPanel(ribbonTab);
            AddViewsPanel(ribbonTab);

            // tools 
            AddToolsPanel(ribbonTab);

            // add settings panel
            AddSettingsPanel(ribbonTab);

            ribbonControl.UpdateLayout();
            if (_wasActive)
            {
                ribbonTab.IsActive = true;
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    /// <summary>
    /// Панель "Оси"
    /// </summary>
    private static void AddAxisPanel(RibbonTab ribbonTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab3") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);
        var ribRowPanel = new RibbonRowPanel();

        // mpAxis
        ribRowPanel.Items.Add(GetBigButton(GetDescriptor<Axis>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Панель "Выноски"
    /// </summary>
    private static void AddLeadersPanel(RibbonTab ribbonTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab15") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);
        var ribRowPanel = new RibbonRowPanel();

        // mpChainLeader
        ribRowPanel.Items.Add(GetBigButton(GetDescriptor<ChainLeader>()));

        // mpNodalLeader
        ribRowPanel.Items.Add(GetBigButton(GetDescriptor<NodalLeader>()));

        // mpSecantNodalLeader
        ribRowPanel.Items.Add(GetSmallButton(GetDescriptor<SecantNodalLeader>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Панель "Отметки уровня"
    /// </summary>
    private static void AddLevelMarksPanel(RibbonTab ribbonTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab11") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);
        var ribRowPanel = new RibbonRowPanel();

        // mpLevelMark
        ribRowPanel.Items.Add(GetBigSplitButton(GetDescriptor<LevelMark>()));

        // mpLevelPlanMark
        ribRowPanel.Items.Add(GetBigButton(GetDescriptor<LevelPlanMark>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Панель "Линии"
    /// </summary>
    private static void AddLinesPanel(RibbonTab ribbonTab)
    {
        // create the panel source
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab1") };

        // now the panel
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);

        var ribRowPanel = new RibbonRowPanel();

        // mpBreakLine
        ribRowPanel.Items.Add(GetBigSplitButton(GetDescriptor<BreakLine>()));

        // mpWeldJoint
        ribRowPanel.Items.Add(GetBigSplitButton(GetDescriptor<WeldJoint>()));

        // mpLetterLine
        ribRowPanel.Items.Add(GetBigSplitButton(GetDescriptor<LetterLine>()));

        ribSourcePanel.Items.Add(ribRowPanel);

        ribRowPanel = new RibbonRowPanel();

        // mpGroundLine
        ribRowPanel.Items.Add(GetSmallSplitButton(GetDescriptor<GroundLine>()));

        ribRowPanel.Items.Add(new RibbonRowBreak());

        // mpWaterProofing
        ribRowPanel.Items.Add(GetSmallSplitButton(GetDescriptor<WaterProofing>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Панель "Виды, разрезы"
    /// </summary>
    private static void AddViewsPanel(RibbonTab ribbonTab)
    {
        // create the panel source
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab8") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);

        var ribRowPanel = new RibbonRowPanel();

        // mpSection
        ribRowPanel.Items.Add(GetBigSplitButton(GetDescriptor<Functions.mpSection.Section>()));
            
        ribSourcePanel.Items.Add(ribRowPanel);

        ribRowPanel = new RibbonRowPanel();

        // mpViewLabel
        ribRowPanel.Items.Add(GetSmallButton(GetDescriptor<ViewLabel>()));

        ribRowPanel.Items.Add(new RibbonRowBreak());

        // mpSectionLabel
        ribRowPanel.Items.Add(GetSmallButton(GetDescriptor<ViewLabel>(), 0));

        ribRowPanel.Items.Add(new RibbonRowBreak());

        // mpView
        ribRowPanel.Items.Add(GetSmallButton(GetDescriptor<Functions.mpView.View>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Панель "Обозначения
    /// </summary>
    private static void AddMarksPanel(RibbonTab ribbonTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab16") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);
        var ribRowPanel = new RibbonRowPanel();

        // mpFragmentMarker
        ribRowPanel.Items.Add(GetBigButton(GetDescriptor<FragmentMarker>()));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Добавить панель "Утилиты"
    /// </summary>
    private static void AddToolsPanel(RibbonTab ribbonTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab9") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribbonTab.Panels.Add(ribPanel);

        var ribRowPanel = new RibbonRowPanel();

        // Search by types
        ribRowPanel.Items.Add(
            RibbonHelpers.AddBigButton(
                "mpESKDSearch",
                Language.GetItem("tab10"),
                _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntities_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntities_32x32_dark.png",
                Language.GetItem("tab13"), Orientation.Vertical, string.Empty, string.Empty, "autocadplugins/mpeskd"));

        // Search by values
        ribRowPanel.Items.Add(
            RibbonHelpers.AddBigButton(
                "mpESKDSearchByValues",
                Language.GetItem("tab12"),
                _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntitiesByValues_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntitiesByValues_32x32_dark.png",
                Language.GetItem("tab14"), Orientation.Vertical, string.Empty, string.Empty, "autocadplugins/mpeskd"));

        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Добавить панель "Настройки"
    /// </summary>
    private static void AddSettingsPanel(RibbonTab ribTab)
    {
        var ribSourcePanel = new RibbonPanelSource { Title = Language.GetItem("tab2") };
        var ribPanel = new RibbonPanel { Source = ribSourcePanel };
        ribTab.Panels.Add(ribPanel);

        var ribRowPanel = new RibbonRowPanel();
        ribRowPanel.Items.Add(
            RibbonHelpers.AddBigButton(
                "mpStyleEditor",
                Language.GetItem("tab4"),
                _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/StyleEditor_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/StyleEditor_32x32_dark.png",
                Language.GetItem("tab5"), Orientation.Vertical, string.Empty, string.Empty, "autocadplugins/mpeskd"));
        ribRowPanel.Items.Add(
            RibbonHelpers.AddBigButton(
                "mpPropertiesPalette",
                ConvertLName(Language.GetItem("tab6")),
                _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Properties_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Properties_32x32_dark.png",
                Language.GetItem("tab7"), Orientation.Vertical, string.Empty, string.Empty, "autocadplugins/mpeskd"));
        ribSourcePanel.Items.Add(ribRowPanel);
    }

    /// <summary>
    /// Получить SplitButton (основная команда + все вложенные команды) для дескриптора функции
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static RibbonSplitButton GetBigSplitButton(
        ISmartEntityDescriptor descriptor,
        Orientation orientation = Orientation.Vertical)
    {
        // Создаем SplitButton
        var risSplitBtn = new RibbonSplitButton
        {
            Text = "RibbonSplitButton",
            Orientation = orientation,
            Size = RibbonItemSize.Large,
            ShowImage = true,
            ShowText = true,
            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
            ResizeStyle = RibbonItemResizeStyles.NoResize,
            ListStyle = RibbonSplitButtonListStyle.List
        };

        var ribBtn = GetBigButton(descriptor, orientation);
        if (ribBtn != null)
        {
            risSplitBtn.Items.Add(ribBtn);
            risSplitBtn.Current = ribBtn;
        }

        // Вложенные команды
        GetBigButtonsForSubFunctions(descriptor, orientation).ForEach(b => risSplitBtn.Items.Add(b));

        return risSplitBtn;
    }

    /// <summary>
    /// Получить SplitButton (основная команда + все вложенные команды) для дескриптора функции
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static RibbonSplitButton GetSmallSplitButton(
        ISmartEntityDescriptor descriptor,
        Orientation orientation = Orientation.Vertical)
    {
        // Создаем SplitButton
        var risSplitBtn = new RibbonSplitButton
        {
            Text = "RibbonSplitButton",
            Orientation = orientation,
            Size = RibbonItemSize.Standard,
            ShowImage = true,
            ShowText = false,
            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
            ResizeStyle = RibbonItemResizeStyles.NoResize,
            ListStyle = RibbonSplitButtonListStyle.List
        };

        var ribBtn = GetButton(descriptor, orientation);
        if (ribBtn != null)
        {
            risSplitBtn.Items.Add(ribBtn);
            risSplitBtn.Current = ribBtn;
        }

        // Вложенные команды
        GetButtonsForSubFunctions(descriptor, orientation).ForEach(b => risSplitBtn.Items.Add(b));

        return risSplitBtn;
    }

    /// <summary>
    /// Получить кнопку по дескриптору функции. Возвращает кнопку для основной функции в дескрипторе
    /// </summary>
    /// <remarks>Для команды должно быть две иконки (16х16 и 32х32) в ресурсах!</remarks>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static RibbonButton GetButton(ISmartEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
    {
        return RibbonHelpers.AddButton(
            descriptor.Name,
            descriptor.LName,
            GetSmallIconForFunction(descriptor.Name, descriptor.Name),
            GetBigIconForFunction(descriptor.Name, descriptor.Name),
            descriptor.Description,
            orientation,
            descriptor.FullDescription,
            GetHelpImageForFunction(descriptor.Name, descriptor.ToolTipHelpImage),
            "autocadplugins/mpeskd");
    }

    /// <summary>
    /// Получить большую кнопку по дескриптору функции. Возвращает кнопку для основной функции в дескрипторе
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static RibbonButton GetBigButton(ISmartEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
    {
        return RibbonHelpers.AddBigButton(
            descriptor.Name,
            descriptor.LName,
            GetBigIconForFunction(descriptor.Name, descriptor.Name),
            descriptor.Description,
            orientation,
            descriptor.FullDescription,
            GetHelpImageForFunction(descriptor.Name, descriptor.ToolTipHelpImage),
            "autocadplugins/mpeskd");
    }

    /// <summary>
    /// Получить маленькую кнопку по дескриптору функции. Возвращает кнопку для основной функции в дескрипторе
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    private static RibbonButton GetSmallButton(ISmartEntityDescriptor descriptor)
    {
        return RibbonHelpers.AddSmallButton(
            descriptor.Name,
            descriptor.LName,
            GetSmallIconForFunction(descriptor.Name, descriptor.Name),
            descriptor.Description,
            descriptor.FullDescription,
            GetHelpImageForFunction(descriptor.Name, descriptor.ToolTipHelpImage),
            "autocadplugins/mpeskd");
    }

    /// <summary>
    /// Получить маленькую кнопку по дескриптору функции для вложенной функции
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="subFunctionIndex">Индекс вложенной функции</param>
    private static RibbonButton GetSmallButton(ISmartEntityDescriptor descriptor, int subFunctionIndex)
    {
        return RibbonHelpers.AddSmallButton(
            descriptor.SubFunctionsNames[subFunctionIndex],
            descriptor.SubFunctionsLNames[subFunctionIndex],
            GetSmallIconForFunction(descriptor.Name, descriptor.SubFunctionsNames[subFunctionIndex]),
            descriptor.SubDescriptions[subFunctionIndex],
            descriptor.SubFullDescriptions[subFunctionIndex],
            GetHelpImageForFunction(descriptor.Name, descriptor.SubHelpImages[subFunctionIndex]),
            "autocadplugins/mpeskd");
    }

    /// <summary>
    /// Получить список кнопок для вложенных команды по дескриптору
    /// </summary>
    /// <remarks>Для всех команд должно быть две иконки (16х16 и 32х32) в ресурсах!</remarks>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий
    /// интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static List<RibbonButton> GetButtonsForSubFunctions(
        ISmartEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
    {
        var buttons = new List<RibbonButton>();

        for (var i = 0; i < descriptor.SubFunctionsNames.Count; i++)
        {
            buttons.Add(RibbonHelpers.AddButton(
                descriptor.SubFunctionsNames[i],
                descriptor.SubFunctionsLNames[i],
                GetSmallIconForFunction(descriptor.Name, descriptor.SubFunctionsNames[i]),
                GetBigIconForFunction(descriptor.Name, descriptor.SubFunctionsNames[i]),
                descriptor.SubDescriptions[i],
                orientation,
                descriptor.SubFullDescriptions[i],
                GetHelpImageForFunction(descriptor.Name, descriptor.SubHelpImages[i]),
                "autocadplugins/mpeskd"));
        }

        return buttons;
    }

    /// <summary>
    /// Получить список больших кнопок для вложенных команды по дескриптору
    /// </summary>
    /// <param name="descriptor">Дескриптор функции - класс, реализующий
    /// интерфейс <see cref="ISmartEntityDescriptor"/></param>
    /// <param name="orientation">Ориентация кнопки</param>
    private static List<RibbonButton> GetBigButtonsForSubFunctions(
        ISmartEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
    {
        var buttons = new List<RibbonButton>();

        for (var i = 0; i < descriptor.SubFunctionsNames.Count; i++)
        {
            buttons.Add(RibbonHelpers.AddBigButton(
                descriptor.SubFunctionsNames[i],
                descriptor.SubFunctionsLNames[i],
                GetBigIconForFunction(descriptor.Name, descriptor.SubFunctionsNames[i]),
                descriptor.SubDescriptions[i],
                orientation,
                descriptor.SubFullDescriptions[i],
                GetHelpImageForFunction(descriptor.Name, descriptor.SubHelpImages[i]),
                "autocadplugins/mpeskd"));
        }

        return buttons;
    }

    private static string GetBigIconForFunction(string functionName, string subFunctionName)
    {
        return _colorTheme == 1
            ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{functionName}/Icons/{subFunctionName}_32x32.png"
            : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{functionName}/Icons/{subFunctionName}_32x32_dark.png";
    }

    private static string GetSmallIconForFunction(string functionName, string subFunctionName)
    {
        return _colorTheme == 1
            ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{functionName}/Icons/{subFunctionName}_16x16.png"
            : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{functionName}/Icons/{subFunctionName}_16x16_dark.png";
    }

    private static string GetHelpImageForFunction(string functionName, string imgName)
    {
        return
            $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{functionName}/Help/{imgName}";
    }

    /// <summary>
    /// Вспомогательный метод для добавления символа перехода на новую строку в именах функций на палитре
    /// </summary>
    private static string ConvertLName(string lName)
    {
        if (!lName.Contains(" "))
        {
            return lName;
        }

        if (lName.Length <= 8)
        {
            return lName;
        }

        if (lName.Count(x => x == ' ') == 1)
        {
            return lName.Split(' ')[0] + Environment.NewLine + lName.Split(' ')[1];
        }

        var center = lName.Length * 0.5;
        var nearestDelta = lName.Select((c, i) => new { index = i, value = c }).Where(w => w.value == ' ')
            .OrderBy(x => Math.Abs(x.index - center)).First().index;
        return lName.Substring(0, nearestDelta) + Environment.NewLine + lName.Substring(nearestDelta + 1);
    }

    private static ISmartEntityDescriptor GetDescriptor<T>()
        where T : SmartEntity
    {
        return SmartEntityUtils.GetDescriptor<T>();
    }
}