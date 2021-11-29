namespace mpESKD.Base.Properties
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Attributes;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Windows;
    using Converters;
    using Enums;
    using ModPlusAPI.Windows;
    using ModPlusStyle.Controls;
    using ModPlusStyle.Transitions;
    using Styles;
    using Utils;
    using View;
    using Visibility = System.Windows.Visibility;

    /// <summary>
    /// Палитра свойств
    /// </summary>
    public partial class PropertiesPalette
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertiesPalette"/> class.
        /// </summary>
        public PropertiesPalette()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
            ModPlusStyle.ThemeManager.ChangeTheme(
                Resources, ModPlusStyle.ThemeManager.Themes.FirstOrDefault(t => t.Name == "LightBlue"), false);

            StckMaxObjectsSelectedMessage.Visibility = Visibility.Collapsed;
            AcadUtils.Documents.DocumentCreated += Documents_DocumentCreated;
            AcadUtils.Documents.DocumentActivated += Documents_DocumentActivated;

            foreach (Document document in AcadUtils.Documents)
            {
                document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;
            }

            if (AcadUtils.Document != null)
            {
                ShowPropertiesControlsBySelection();
            }
        }

        private void Documents_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.ImpliedSelectionChanged -= Document_ImpliedSelectionChanged;
                e.Document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;
            }
        }

        private void Documents_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.ImpliedSelectionChanged -= Document_ImpliedSelectionChanged;
                e.Document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;
            }
        }

        private void Document_ImpliedSelectionChanged(object sender, EventArgs e)
        {
            ShowPropertiesControlsBySelection();
        }

        /// <summary>
        /// Добавление пользовательских элементов в палитру в зависимости от выбранных объектов
        /// </summary>
        private void ShowPropertiesControlsBySelection()
        {
            BtCollapseAll.Visibility = Visibility.Hidden;

            // Удаляем контролы свойств
            if (StackPanelProperties.Children.Count > 0)
            {
                StackPanelProperties.Children.Clear();
            }

            var psr = AcadUtils.Editor.SelectImplied();
            if (psr.Value == null || psr.Value.Count == 0)
            {
                // Очищаем панель описания
                ShowDescription(string.Empty);

                // hide message
                StckMaxObjectsSelectedMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                var maxSelectedObjects = MainSettings.Instance.MaxSelectedObjects;
                if (maxSelectedObjects == 0 || maxSelectedObjects >= psr.Value.Count)
                {
                    StckMaxObjectsSelectedMessage.Visibility = Visibility.Collapsed;

                    var objectIds = new List<ObjectId>();
                    using (AcadUtils.Document.LockDocument())
                    {
                        using (var tr = new OpenCloseTransaction())
                        {
                            foreach (SelectedObject selectedObject in psr.Value)
                            {
                                if (selectedObject.ObjectId == ObjectId.Null)
                                {
                                    continue;
                                }

                                var obj = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
                                if (obj is BlockReference blockReference &&
                                    ExtendedDataUtils.IsApplicable(blockReference))
                                {
                                    objectIds.Add(selectedObject.ObjectId);
                                }
                            }

                            tr.Commit();
                        }
                    }

                    if (objectIds.Any())
                    {
                        BtCollapseAll.Visibility = Visibility.Visible;
                        var summaryPropertyCollection = new SummaryPropertyCollection(objectIds);
                        summaryPropertyCollection.OnLockedLayerEventHandler +=
                            (sender, args) => ShowPropertiesControlsBySelection();
                        SetData(summaryPropertyCollection);
                    }
                }
                else
                {
                    StckMaxObjectsSelectedMessage.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Построение элементов в палитре по данным коллекции свойств
        /// </summary>
        /// <param name="collection"><see cref="SummaryPropertyCollection"/></param>
        public void SetData(SummaryPropertyCollection collection)
        {
            var different = $"*{ModPlusAPI.Language.GetItem("vc1")}*";

            var entityGroups = collection.Where(sp => sp.EntityPropertyDataCollection.Any())
                .GroupBy(sp => sp.EntityType);

            foreach (var entityGroup in entityGroups)
            {
                // Тип примитива может содержать атрибуты указывающие зависимость видимости свойств
                var visibilityDependencyAttributes = GetVisibilityDependencyAttributes(entityGroup.Key);

                // и атрибуты, задающие ограничение на ввод для текстовых свойств
                var regexInputRestrictionAttributes = GetRegexInputRestrictionAttributes(entityGroup.Key);

                var allEntitySummaryProperties = entityGroup.Select(g => g).ToList();

                var c = entityGroup.SelectMany(sp => sp.EntityPropertyDataCollection).Select(p => p.OwnerObjectId).Distinct().Count();
                var entityExpander = new Expander
                {
                    IsExpanded = true,
                    Header = $"{LocalizationUtils.GetEntityLocalizationName(entityGroup.Key)} [{c}]",
                    Style = Resources["EntityExpander"] as Style
                };

                var mainGrid = new Grid
                {
                    Visibility = Visibility.Collapsed,
                    Opacity = 0.0
                };
                Transitions.SetOpacity(mainGrid, new OpacityParams
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = 300,
                    TransitionOn = TransitionOn.Visibility
                });
                var categoryIndex = 0;
                var summaryPropertiesGroups = entityGroup.GroupBy(sp => sp.Category).ToList();
                summaryPropertiesGroups.Sort((sp1, sp2) => sp1.Key.CompareTo(sp2.Key));

                foreach (var summaryPropertiesGroup in summaryPropertiesGroups)
                {
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var grid = new Grid();
                    Grid.SetRow(grid, categoryIndex);

                    var headerRowDefinition = new RowDefinition { Height = GridLength.Auto };
                    var firstColumn = new ColumnDefinition { MinWidth = 50 };
                    BindingOperations.SetBinding(firstColumn, ColumnDefinition.WidthProperty, CreateBindingForColumnWidth());
                    var secondColumn = new ColumnDefinition { Width = GridLength.Auto };
                    var thirdColumn = new ColumnDefinition { MinWidth = 50 };
                    grid.RowDefinitions.Add(headerRowDefinition);
                    grid.ColumnDefinitions.Add(firstColumn);
                    grid.ColumnDefinitions.Add(secondColumn);
                    grid.ColumnDefinitions.Add(thirdColumn);

                    var categoryHeader = new TextBox
                    {
                        Text = LocalizationUtils.GetCategoryLocalizationName(summaryPropertiesGroup.Key)
                    };
                    Grid.SetRow(categoryHeader, 0);
                    Grid.SetColumn(categoryHeader, 0);
                    Grid.SetColumnSpan(categoryHeader, 3);
                    categoryHeader.Style = Resources["HeaderTextBox"] as Style;
                    grid.Children.Add(categoryHeader);

                    // sort
                    var j = 1;
                    foreach (var summaryProperty in summaryPropertiesGroup.OrderBy(sp => sp.OrderIndex))
                    {
                        if (summaryProperty.PropertyScope == PropertyScope.Hidden)
                        {
                            continue;
                        }

                        var rowDefinition = new RowDefinition { Height = GridLength.Auto };
                        grid.RowDefinitions.Add(rowDefinition);

                        // property name
                        var propertyDescription = GetPropertyDescription(summaryProperty);
                        var propertyHeader = new TextBox
                        {
                            Text = GetPropertyDisplayName(summaryProperty),
                            Style = Resources["PropertyNameTextBox"] as Style
                        };
                        SetDescription(propertyHeader, propertyDescription);
                        SetVisibilityDependency(visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, propertyHeader);
                        Grid.SetColumn(propertyHeader, 0);
                        Grid.SetRow(propertyHeader, j);
                        grid.Children.Add(propertyHeader);

                        var entityProperty = summaryProperty.EntityPropertyDataCollection.FirstOrDefault();

                        if (entityProperty != null)
                        {
                            if (summaryProperty.PropertyName == "Style")
                            {
                                try
                                {
                                    var cb = new ComboBox();
                                    Grid.SetColumn(cb, 2);
                                    Grid.SetRow(cb, j);
                                    cb.ItemsSource = StyleManager.GetStyles(entityProperty.EntityType).Select(s => s.Name);
                                    cb.Style = Resources["PropertyValueComboBox"] as Style;
                                    SetDescription(cb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, cb);
                                    SetForegroundBinding(cb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        cb, ComboBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    grid.Children.Add(cb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (summaryProperty.PropertyName == "LayerName")
                            {
                                try
                                {
                                    var cb = new ComboBox();
                                    Grid.SetColumn(cb, 2);
                                    Grid.SetRow(cb, j);
                                    cb.ItemsSource = AcadUtils.Layers;
                                    cb.Style = Resources["PropertyValueComboBox"] as Style;
                                    SetDescription(cb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, cb);
                                    SetForegroundBinding(cb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        cb, ComboBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    grid.Children.Add(cb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (summaryProperty.PropertyName == "Scale")
                            {
                                try
                                {
                                    var cb = new ComboBox();
                                    Grid.SetColumn(cb, 2);
                                    Grid.SetRow(cb, j);
                                    cb.ItemsSource = AcadUtils.Scales;
                                    cb.Style = Resources["PropertyValueComboBox"] as Style;
                                    SetDescription(cb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, cb);
                                    SetForegroundBinding(cb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        cb, ComboBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    grid.Children.Add(cb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (summaryProperty.PropertyName == "LineType")
                            {
                                try
                                {
                                    var tb = new TextBox();
                                    Grid.SetColumn(tb, 2);
                                    Grid.SetRow(tb, j);
                                    tb.Style = Resources["PropertyValueTextBoxClickable"] as Style;
                                    tb.PreviewMouseDown += LineType_OnPreviewMouseDown;
                                    SetDescription(tb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, tb);
                                    SetForegroundBinding(tb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        tb, TextBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    grid.Children.Add(tb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (summaryProperty.PropertyName.Contains("TextStyle"))
                            {
                                try
                                {
                                    var cb = new ComboBox();
                                    Grid.SetColumn(cb, 2);
                                    Grid.SetRow(cb, j);
                                    cb.ItemsSource = AcadUtils.TextStyles;
                                    cb.Style = Resources["PropertyValueComboBox"] as Style;
                                    SetDescription(cb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, cb);
                                    SetForegroundBinding(cb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        cb, ComboBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    grid.Children.Add(cb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (entityProperty.Value is Enum)
                            {
                                try
                                {
                                    var cb = new ComboBox();
                                    Grid.SetColumn(cb, 2);
                                    Grid.SetRow(cb, j);
                                    cb.Style = Resources["PropertyValueComboBox"] as Style;
                                    var type = entityProperty.Value.GetType();
                                    SetDescription(cb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, cb);
                                    cb.ItemsSource = LocalizationUtils.GetEnumPropertyLocalizationFields(type);
                                    cb.IsEnabled = !entityProperty.IsReadOnly;
                                    SetForegroundBinding(cb, summaryProperty);

                                    BindingOperations.SetBinding(cb, ComboBox.TextProperty,
                                        CreateTwoWayBindingForProperty(summaryProperty, new EnumPropertyValueConverter()));

                                    grid.Children.Add(cb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (entityProperty.Value is int)
                            {
                                try
                                {
                                    if (entityProperty.IsReadOnly)
                                    {
                                        var tb = new TextBox
                                        {
                                            Style = Resources["PropertyValueReadOnlyTextBox"] as Style
                                        };
                                        Grid.SetColumn(tb, 2);
                                        Grid.SetRow(tb, j);

                                        SetDescription(tb, propertyDescription);
                                        SetVisibilityDependency(visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, tb);
                                        if (summaryProperty.IntValue.HasValue)
                                        {
                                            BindingOperations.SetBinding(
                                                tb,
                                                TextBox.TextProperty,
                                                CreateOneWayBindingForPropertyForNumericValue(summaryProperty, true));
                                        }
                                        else
                                        {
                                            tb.Text = different;
                                        }

                                        grid.Children.Add(tb);
                                    }
                                    else
                                    {
                                        var numericBox = new NumericBox();
                                        Grid.SetColumn(numericBox, 2);
                                        Grid.SetRow(numericBox, j);
                                        numericBox.Minimum = summaryProperty.EntityPropertyDataCollection
                                            .Select(p => Convert.ToInt32(p.Minimum)).Max();
                                        numericBox.Maximum = summaryProperty.EntityPropertyDataCollection
                                            .Select(p => Convert.ToInt32(p.Maximum)).Min();
                                        numericBox.Interval = 1.0;
                                        numericBox.NumericInputMode = NumericInput.Numbers;
                                        numericBox.Style = Resources["PropertyValueNumericTextBox"] as Style;
                                        HintAssist.SetHint(numericBox, different);
                                        SetDescription(numericBox, propertyDescription);
                                        SetVisibilityDependency(
                                            visibilityDependencyAttributes,
                                            allEntitySummaryProperties,
                                            summaryProperty.PropertyName,
                                            numericBox);

                                        BindingOperations.SetBinding(
                                            numericBox,
                                            NumericBox.ValueProperty,
                                            CreateTwoWayBindingForPropertyForNumericValue(summaryProperty, true));

                                        grid.Children.Add(numericBox);
                                    }
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (entityProperty.Value is double)
                            {
                                try
                                {
                                    if (entityProperty.IsReadOnly)
                                    {
                                        var tb = new TextBox
                                        {
                                            Style = Resources["PropertyValueReadOnlyTextBox"] as Style
                                        };
                                        Grid.SetColumn(tb, 2);
                                        Grid.SetRow(tb, j);
                                        SetDescription(tb, propertyDescription);
                                        SetVisibilityDependency(
                                            visibilityDependencyAttributes,
                                            allEntitySummaryProperties,
                                            summaryProperty.PropertyName,
                                            tb);
                                        if (summaryProperty.DoubleValue.HasValue)
                                        {
                                            BindingOperations.SetBinding(
                                               tb,
                                               TextBox.TextProperty,
                                               CreateOneWayBindingForPropertyForNumericValue(summaryProperty, false));
                                        }
                                        else
                                        {
                                            tb.Text = different;
                                        }

                                        grid.Children.Add(tb);
                                    }
                                    else
                                    {
                                        var numericBox = new NumericBox();
                                        Grid.SetColumn(numericBox, 2);
                                        Grid.SetRow(numericBox, j);
                                        numericBox.Minimum = summaryProperty.EntityPropertyDataCollection
                                            .Select(p => Convert.ToDouble(p.Minimum)).Max();
                                        numericBox.Maximum = summaryProperty.EntityPropertyDataCollection
                                            .Select(p => Convert.ToDouble(p.Maximum)).Min();
                                        numericBox.NumericInputMode = NumericInput.Decimal;
                                        numericBox.Speedup = true;
                                        numericBox.Interval = 0.1;
                                        numericBox.Style = Resources["PropertyValueNumericTextBox"] as Style;
                                        HintAssist.SetHint(numericBox, different);
                                        SetDescription(numericBox, propertyDescription);
                                        SetVisibilityDependency(
                                            visibilityDependencyAttributes,
                                            allEntitySummaryProperties,
                                            summaryProperty.PropertyName,
                                            numericBox);
                                        BindingOperations.SetBinding(
                                            numericBox,
                                            NumericBox.ValueProperty,
                                            CreateTwoWayBindingForPropertyForNumericValue(summaryProperty, false));

                                        grid.Children.Add(numericBox);
                                    }
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (entityProperty.Value is bool)
                            {
                                try
                                {
                                    var chb = new CheckBox
                                    {
                                        Style = Resources["PropertyValueCheckBox"] as Style
                                    };
                                    SetDescription(chb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes,
                                        allEntitySummaryProperties,
                                        summaryProperty.PropertyName,
                                        chb);
                                    chb.IsEnabled = !entityProperty.IsReadOnly;
                                    BindingOperations.SetBinding(
                                        chb,
                                        ToggleButton.IsCheckedProperty,
                                        CreateTwoWayBindingForProperty(summaryProperty));

                                    var outerBorder = new Border
                                    {
                                        Style = Resources["BorderForValueCheckBox"] as Style
                                    };
                                    Grid.SetColumn(outerBorder, 2);
                                    Grid.SetRow(outerBorder, j);

                                    outerBorder.Child = chb;
                                    grid.Children.Add(outerBorder);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }
                            else if (entityProperty.Value is string)
                            {
                                try
                                {
                                    var tb = new TextBox();
                                    Grid.SetColumn(tb, 2);
                                    Grid.SetRow(tb, j);
                                    tb.Style = Resources["PropertyValueTextBox"] as Style;
                                    SetDescription(tb, propertyDescription);
                                    SetVisibilityDependency(
                                        visibilityDependencyAttributes, allEntitySummaryProperties, summaryProperty.PropertyName, tb);
                                    SetForegroundBinding(tb, summaryProperty);
                                    BindingOperations.SetBinding(
                                        tb, TextBox.TextProperty, CreateTwoWayBindingForProperty(summaryProperty));
                                    tb.IsReadOnly = entityProperty.IsReadOnly;

                                    if (regexInputRestrictionAttributes.ContainsKey(summaryProperty.PropertyName))
                                        SetInputRestriction(tb, regexInputRestrictionAttributes[summaryProperty.PropertyName]);

                                    grid.Children.Add(tb);
                                }
                                catch (Exception exception)
                                {
                                    ExceptionBox.Show(exception);
                                }
                            }

                            j++;
                        }
                    }

                    grid.Children.Add(CreateGridSplitter(j));

                    mainGrid.Children.Add(grid);

                    categoryIndex++;
                }

                entityExpander.Content = mainGrid;

                StackPanelProperties.Children.Add(entityExpander);

                mainGrid.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Добавление описания свойства в тэг элемента и подписывание на события
        /// </summary>
        /// <param name="e">Элемент</param>
        /// <param name="description">Описание свойства</param>
        private void SetDescription(FrameworkElement e, string description)
        {
            e.Tag = description;
            e.GotFocus += PropertyControl_OnGotFocus;
            e.LostFocus += PropertyControl_OnLostFocus;
        }

        /// <summary>
        /// Установка зависимости видимости в случае, если имеется специальный атрибут
        /// </summary>
        /// <param name="visibilityDependencyAttributes">Список атрибутов зависимостей видимости для читаемого типа примитива</param>
        /// <param name="allEntitySummaryProperties">Список всех свойств примитива</param>
        /// <param name="propertyName">Имя свойства, которое отображается текущим элементом палитры (заголовок или значение)</param>
        /// <param name="element">Элемент палитры</param>
        private static void SetVisibilityDependency(
            Dictionary<string, PropertyVisibilityDependencyAttribute> visibilityDependencyAttributes,
            IEnumerable<SummaryProperty> allEntitySummaryProperties,
            string propertyName,
            DependencyObject element)
        {
            try
            {
                // direct visibility
                var attribute = visibilityDependencyAttributes
                    .FirstOrDefault(a => a.Value.VisibleDependencyProperties.Contains(propertyName));
                if (attribute.Key != null)
                {
                    var binding = new Binding
                    {
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Source = allEntitySummaryProperties.FirstOrDefault(sp => sp.PropertyName == attribute.Key),
                        Path = new PropertyPath("SummaryValue"),
                        Converter = new ModPlusAPI.Converters.BooleanToVisibilityConverter()
                    };
                    BindingOperations.SetBinding(element, VisibilityProperty, binding);
                }
                else
                {
                    // inverse visibility
                    attribute = visibilityDependencyAttributes
                        .FirstOrDefault(a => a.Value.HiddenDependencyProperties != null &&
                                             a.Value.HiddenDependencyProperties.Contains(propertyName));
                    if (attribute.Key != null)
                    {
                        var binding = new Binding
                        {
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            Source = allEntitySummaryProperties.FirstOrDefault(sp => sp.PropertyName == attribute.Key),
                            Path = new PropertyPath("SummaryValue"),
                            Converter = new ModPlusAPI.Converters.BooleanInverseConverter()
                        };
                        BindingOperations.SetBinding(element, VisibilityProperty, binding);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private Dictionary<string, PropertyVisibilityDependencyAttribute> GetVisibilityDependencyAttributes(Type entityType)
        {
            var dictionary = new Dictionary<string, PropertyVisibilityDependencyAttribute>();
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<PropertyVisibilityDependencyAttribute>();
                if (attribute != null)
                {
                    dictionary.Add(propertyInfo.Name, attribute);
                }
            }

            return dictionary;
        }

        private Dictionary<string, RegexInputRestrictionAttribute> GetRegexInputRestrictionAttributes(Type entityType)
        {
            var dictionary = new Dictionary<string, RegexInputRestrictionAttribute>();
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<RegexInputRestrictionAttribute>();
                if (attribute != null)
                {
                    dictionary.Add(propertyInfo.Name, attribute);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Отображение описания свойства при получении элементом фокуса
        /// </summary>
        private void PropertyControl_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                ShowDescription(element.Tag.ToString());
            }
        }

        /// <summary>
        /// Очистка поля вывода описания свойства при пропадании фокуса с элемента
        /// </summary>
        private void PropertyControl_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ShowDescription(string.Empty);
        }

        /// <summary>
        /// Получение локализованного (отображаемого) имени свойства
        /// </summary>
        /// <param name="summaryProperty">Суммарное свойство</param>
        private static string GetPropertyDisplayName(SummaryProperty summaryProperty)
        {
            try
            {
                var displayName = ModPlusAPI.Language.GetItem(summaryProperty.DisplayNameLocalizationKey);
                if (!string.IsNullOrEmpty(displayName))
                {
                    if (!displayName.EndsWith(":"))
                        displayName = $"{displayName}:";

                    return displayName;
                }
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }

        /// <summary>
        /// Получение локализованного описания свойства
        /// </summary>
        /// <param name="summaryProperty">Суммарное свойство</param>
        private static string GetPropertyDescription(SummaryProperty summaryProperty)
        {
            try
            {
                var description = ModPlusAPI.Language.GetItem(summaryProperty.DescriptionLocalizationKey);
                if (!string.IsNullOrEmpty(description))
                {
                    return description;
                }
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }

        /// <summary>
        /// Добавление в Grid разделителя GridSplitter
        /// </summary>
        /// <param name="rowSpan">Количество пересекаемых строк</param>
        private static GridSplitter CreateGridSplitter(int rowSpan)
        {
            var gridSplitter = new GridSplitter
            {
                BorderThickness = new Thickness(2, 0, 0, 0),
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#FF696969"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetColumn(gridSplitter, 1);
            Grid.SetRow(gridSplitter, 1);
            Grid.SetRowSpan(gridSplitter, rowSpan);
            return gridSplitter;
        }

        /// <summary>
        /// Создание двусторонней привязки к суммарному свойству
        /// </summary>
        /// <param name="summaryProperty">Суммарное свойство</param>
        /// <param name="converter">Конвертер (при необходимости)</param>
        /// <returns>Объект типа <see cref="Binding"/></returns>
        private static Binding CreateTwoWayBindingForProperty(
            SummaryProperty summaryProperty, IValueConverter converter = null)
        {
            var binding = new Binding
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = summaryProperty,
                Path = new PropertyPath("SummaryValue")
            };
            if (converter != null)
            {
                binding.Converter = converter;
            }

            return binding;
        }

        /// <summary>
        /// Создание двусторонней привязки для использования в элементе <see cref="NumericBox"/>
        /// По какой-то причине при привязке к типу object не работает. В связи с этим делаю такой вот
        /// лайфхак - добавляю в класс <see cref="SmartEntityProperty"/> два свойства конкретного типа.
        /// Это нужно, чтобы решить эту специфическую проблему в данном проекте и не менять из-за этого
        /// библиотеку оформления
        /// </summary>
        /// <param name="summaryProperty">Суммарное свойство</param>
        /// <param name="isInteger">True - целое число. False - дробное число</param>
        private Binding CreateTwoWayBindingForPropertyForNumericValue(SummaryProperty summaryProperty, bool isInteger)
        {
            return new Binding
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = summaryProperty,
                Path = isInteger ? new PropertyPath("IntValue") : new PropertyPath("DoubleValue")
            };
        }

        /// <summary>
        /// Создание односторонней привязки числовых свойств. Используется, когда свойство только для чтения
        /// По какой-то причине при привязке к типу object не работает. В связи с этим делаю такой вот
        /// лайфхак - добавляю в класс <see cref="SmartEntityProperty"/> два свойства конкретного типа.
        /// Это нужно, чтобы решить эту специфическую проблему в данном проекте и не менять из-за этого
        /// библиотеку оформления
        /// </summary>
        /// <param name="summaryProperty">Суммарное свойство</param>
        /// <param name="isInteger">True - целое число. False - дробное число</param>
        private Binding CreateOneWayBindingForPropertyForNumericValue(SummaryProperty summaryProperty, bool isInteger)
        {
            return new Binding
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = summaryProperty,
                Path = isInteger ? new PropertyPath("IntValue") : new PropertyPath("DoubleValue")
            };
        }

        /// <summary>
        /// Установка привязки цвета текста элемента к свойству объекта <see cref="SummaryProperty"/>
        /// </summary>
        /// <param name="element">Визуальный элемент</param>
        /// <param name="summaryProperty">Суммарное свойство</param>
        private void SetForegroundBinding(DependencyObject element, SummaryProperty summaryProperty)
        {
            BindingOperations.SetBinding(element, ForegroundProperty,
                new Binding
                {
                    Source = summaryProperty,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Path = new PropertyPath("Foreground")
                });
        }

        /// <summary>
        /// Создание привязки для первой колонки в Grid. Позволяет менять ширину сразу всех колонок в текущем UserControl
        /// </summary>
        /// <returns>Объект типа <see cref="Binding"/></returns>
        private static Binding CreateBindingForColumnWidth()
        {
            var b = new Binding
            {
                Source = mpESKD.Properties.Settings.Default,
                Path = new PropertyPath("GridColumnWidth"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new ColumnWidthConverter()
            };
            return b;
        }

        /// <summary>
        /// Отобразить описание свойства в специальном поле палитры
        /// </summary>
        /// <param name="description">Описание свойства</param>
        public void ShowDescription(string description)
        {
            TbDescription.Text = description;
        }

        /// <summary>
        /// Отображение диалогового окна AutoCAD с выбором типа линии
        /// </summary>
        private static void LineType_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            using (AcadUtils.Document.LockDocument())
            {
                var ltd = new LinetypeDialog { IncludeByBlockByLayer = false };
                if (ltd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!ltd.Linetype.IsNull)
                    {
                        using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
                        {
                            using (var ltr = tr.GetObject(ltd.Linetype, OpenMode.ForRead) as LinetypeTableRecord)
                            {
                                if (ltr != null)
                                {
                                    ((TextBox)sender).Text = ltr.Name;
                                }
                            }

                            tr.Commit();
                        }
                    }
                }
            }
        }

        private void LmSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var lmSetting = new PaletteSettings();
            lmSetting.ShowDialog();

            if (MainSettings.Instance.AddToMpPalette)
                MainFunction.AddToMpPalette();
            else
                MainFunction.RemoveFromMpPalette(true);
        }

        // open settings
        private void OpenSettings_OnClick(object sender, RoutedEventArgs e)
        {
            if (AcadUtils.Document != null)
            {
                AcadUtils.Document.SendStringToExecute("mpStyleEditor ", true, false, false);
            }
        }

        private void BtCollapseAll_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (UIElement child in StackPanelProperties.Children)
            {
                if (child is Expander expander)
                    expander.IsExpanded = false;
            }
        }

        private void SetInputRestriction(TextBox tb, RegexInputRestrictionAttribute regexInputRestrictionAttribute)
        {
            tb.SetValue(TextBoxAttachedProperties.InputRestrictionRegexPatternProperty, regexInputRestrictionAttribute.Pattern);
            tb.PreviewTextInput += TbOnPreviewTextInput;
            DataObject.AddPastingHandler(tb, TbOnPasting);
        }

        private void TbOnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.GetValue(TextBoxAttachedProperties.InputRestrictionRegexPatternProperty) is string pattern)
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    var text = (string)e.DataObject.GetData(typeof(string));
                    if (!IsAllowedText(text, pattern))
                        e.CancelCommand();
                }
                else
                {
                    e.CancelCommand();
                }
            }
        }

        private void TbOnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.GetValue(TextBoxAttachedProperties.InputRestrictionRegexPatternProperty) is string pattern)
            {
                e.Handled = !IsAllowedText(e.Text, pattern);
            }
        }

        private bool IsAllowedText(string text, string pattern)
        {
            return new Regex(pattern).IsMatch(text);
        }
    }
}
