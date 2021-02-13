namespace mpESKD
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.Windows;
    using Base.Abstractions;
    using Functions.mpAxis;
    using Functions.mpBreakLine;
    using Functions.mpGroundLine;
    using Functions.mpLevelMark;
    using Functions.mpNodalLeader;
    using Functions.mpSecantNodalLeader;
    using Functions.mpWaterProofing;
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
                    foreach (var tab in ribbonControl.Tabs.Where(
                        tab => tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName)))
                    {
                        ribbonControl.Tabs.Remove(tab);
                        Application.SystemVariableChanged -= AcadApp_SystemVariableChanged;
                        break;
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
            var loaded = false;
            var ribbonControl = ComponentManager.Ribbon;
            var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
            foreach (var tab in ribbonControl.Tabs)
            {
                if (tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName))
                {
                    loaded = true;
                    break;
                }
            }

            return loaded;
        }

        private static bool IsActive()
        {
            var ribbonControl = ComponentManager.Ribbon;
            var tabName = Language.TryGetCuiLocalGroupName("ModPlus ЕСКД");
            foreach (var tab in ribbonControl.Tabs)
            {
                if (tab.Id.Equals("ModPlus_ESKD") && tab.Title.Equals(tabName))
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
                var sv = Application.GetSystemVariable("COLORTHEME").ToString();
                if (int.TryParse(sv, out var i))
                {
                    _colorTheme = i;
                }
                else
                {
                    _colorTheme = 1; // light
                }
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
            var ribbonButton = GetBigButton(Axis.GetDescriptor());
            if (ribbonButton != null)
            {
                ribRowPanel.Items.Add(ribbonButton);
            }

            if (ribRowPanel.Items.Any())
            {
                ribSourcePanel.Items.Add(ribRowPanel);
            }
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
            
            // mpNodalLeader
            var ribbonButton = GetBigButton(NodalLeader.GetDescriptor());
            if (ribbonButton != null)
            {
                ribRowPanel.Items.Add(ribbonButton);
            }
            
            // mpSecantNodalLeader
            ribbonButton = GetSmallButton(SecantNodalLeader.GetDescriptor());
            if (ribbonButton != null)
            {
                ribRowPanel.Items.Add(ribbonButton);
            }

            if (ribRowPanel.Items.Any())
            {
                ribSourcePanel.Items.Add(ribRowPanel);
            }
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
            ribRowPanel.Items.Add(GetSplitButton(LevelMark.GetDescriptor()));

            if (ribRowPanel.Items.Any())
            {
                ribSourcePanel.Items.Add(ribRowPanel);
            }
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
            ribRowPanel.Items.Add(GetSplitButton(BreakLine.GetDescriptor()));

            // mpGroundLine
            ribRowPanel.Items.Add(GetSplitButton(GroundLineDescriptor.Instance));

            // mpWaterProofing
            ribRowPanel.Items.Add(GetSplitButton(WaterProofing.GetDescriptor()));

            if (ribRowPanel.Items.Any())
            {
                ribSourcePanel.Items.Add(ribRowPanel);
            }
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
            ribRowPanel.Items.Add(GetSplitButton(Functions.mpSection.Section.GetDescriptor()));

            if (ribRowPanel.Items.Any())
            {
                ribSourcePanel.Items.Add(ribRowPanel);
            }
        }

        /// <summary>
        /// Добавить панель "Утилиты"
        /// </summary>
        private static void AddToolsPanel(RibbonTab ribbonTab)
        {
            // create the panel source
            var ribSourcePanel = new RibbonPanelSource
            {
                Title = Language.GetItem("tab9")
            };

            // now the panel
            var ribPanel = new RibbonPanel
            {
                Source = ribSourcePanel
            };
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
                    Language.GetItem("tab13"), Orientation.Vertical, string.Empty, string.Empty, "help/mpeskd"));

            // Search by values
            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                    "mpESKDSearchByValues",
                    Language.GetItem("tab12"),
                    _colorTheme == 1 // 1 - light
                        ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntitiesByValues_32x32.png"
                        : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/SearchEntitiesByValues_32x32_dark.png",
                    Language.GetItem("tab14"), Orientation.Vertical, string.Empty, string.Empty, "help/mpeskd"));

            ribSourcePanel.Items.Add(ribRowPanel);
        }

        /// <summary>
        /// Добавить панель "Настройки"
        /// </summary>
        private static void AddSettingsPanel(RibbonTab ribTab)
        {
            // create the panel source
            var ribSourcePanel = new RibbonPanelSource
            {
                Title = Language.GetItem("tab2")
            };

            // now the panel
            var ribPanel = new RibbonPanel
            {
                Source = ribSourcePanel
            };
            ribTab.Panels.Add(ribPanel);

            var ribRowPanel = new RibbonRowPanel();
            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                    "mpStyleEditor",
                    Language.GetItem("tab4"),
                    _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/StyleEditor_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/StyleEditor_32x32_dark.png",
                    Language.GetItem("tab5"), Orientation.Vertical, string.Empty, string.Empty, "help/mpeskd"));
            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                    "mpPropertiesPalette",
                    ConvertLName(Language.GetItem("tab6")),
                    _colorTheme == 1 // 1 - light
                    ? $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Properties_32x32.png"
                    : $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Properties_32x32_dark.png",
                    Language.GetItem("tab7"), Orientation.Vertical, string.Empty, string.Empty, "help/mpeskd"));
            ribSourcePanel.Items.Add(ribRowPanel);
        }

        /// <summary>
        /// Получить SplitButton (основная команда + все вложенные команды) для дескриптора функции
        /// </summary>
        /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="IIntellectualEntityDescriptor"/></param>
        /// <param name="orientation">Ориентация кнопки</param>
        /// <param name="size">Размер кнопки</param>
        private static RibbonSplitButton GetSplitButton(
            IIntellectualEntityDescriptor descriptor,
            Orientation orientation = Orientation.Vertical,
            RibbonItemSize size = RibbonItemSize.Large)
        {
            // Создаем SplitButton
            var risSplitBtn = new RibbonSplitButton
            {
                Text = "RibbonSplitButton",
                Orientation = orientation,
                Size = size,
                ShowImage = true,
                ShowText = true,
                ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                ResizeStyle = RibbonItemResizeStyles.NoResize,
                ListStyle = RibbonSplitButtonListStyle.List
            };

            // Добавляем в него первую функцию, которую делаем основной
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
        /// Получить большую кнопку по дескриптору функции. Возвращает кнопку для основной функции в дескрипторе
        /// </summary>
        /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="IIntellectualEntityDescriptor"/></param>
        /// <param name="orientation">Ориентация кнопки</param>
        private static RibbonButton GetBigButton(IIntellectualEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
        {
            return RibbonHelpers.AddBigButton(
                descriptor.Name,
                descriptor.LName,
                GetBigIconForFunction(descriptor.Name, descriptor.Name),
                descriptor.Description,
                orientation,
                descriptor.FullDescription,
                GetHelpImageForFunction(descriptor.Name, descriptor.ToolTipHelpImage),
                "help/mpeskd");
        }
        
        /// <summary>
        /// Получить маленькую кнопку по дескриптору функции. Возвращает кнопку для основной функции в дескрипторе
        /// </summary>
        /// <param name="descriptor">Дескриптор функции - класс, реализующий интерфейс <see cref="IIntellectualEntityDescriptor"/></param>
        private static RibbonButton GetSmallButton(IIntellectualEntityDescriptor descriptor)
        {
            return RibbonHelpers.AddSmallButton(
                descriptor.Name,
                descriptor.LName,
                GetSmallIconForFunction(descriptor.Name, descriptor.Name),
                descriptor.Description,
                descriptor.FullDescription,
                GetHelpImageForFunction(descriptor.Name, descriptor.ToolTipHelpImage),
                "help/mpeskd");
        }

        /// <summary>
        /// Получить список больших кнопок для вложенных команды по дескриптору
        /// </summary>
        /// <param name="descriptor">Дескриптор функции - класс, реализующий
        /// интерфейс <see cref="IIntellectualEntityDescriptor"/></param>
        /// <param name="orientation">Ориентация кнопки</param>
        private static List<RibbonButton> GetBigButtonsForSubFunctions(
            IIntellectualEntityDescriptor descriptor, Orientation orientation = Orientation.Vertical)
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
                    "help/mpeskd"));
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
    }
}
