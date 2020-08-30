namespace mpESKD.Base.Styles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Attributes;
    using Autodesk.AutoCAD.DatabaseServices;
    using Enums;
    using JetBrains.Annotations;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Properties;
    using Utils;

    /// <summary>
    /// Менеджер стилей
    /// </summary>
    public static class StyleManager
    {
        static StyleManager()
        {
            if (EntityStyles == null)
            {
                EntityStyles = new List<SmartEntityStyle>();
                CheckStyleFiles();
                CreateSystemStyles();
            }
        }

        /// <summary>
        /// Статическая коллекция стилей интеллектуальных примитивов
        /// </summary>
        private static readonly List<SmartEntityStyle> EntityStyles;

        /// <summary>
        /// Проверка и создание в случае отсутствия файлов хранения пользовательских стилей для всех типов примитивов
        /// </summary>
        private static void CheckStyleFiles()
        {
            var entityTypes = TypeFactory.Instance.GetEntityTypes();
            entityTypes.ForEach(et =>
            {
                bool needToCreate;
                var stylesFile = Path.Combine(MainFunction.StylesPath, $"{et.Name}Styles.xml");
                if (File.Exists(stylesFile))
                {
                    try
                    {
                        XElement.Load(stylesFile);
                        needToCreate = false;
                    }
                    catch
                    {
                        needToCreate = true;
                    }
                }
                else
                {
                    needToCreate = true;
                }

                if (needToCreate)
                {
                    var fXel = new XElement("Styles");
                    fXel.Save(stylesFile);
                }
            });
        }

        /// <summary>
        /// Создание стандартных системных стилей
        /// </summary>
        private static void CreateSystemStyles()
        {
            var entityTypes = TypeFactory.Instance.GetEntityTypes();
            entityTypes.ForEach(et =>
            {
                var systemStyle = new SmartEntityStyle(et)
                {
                    Name =
                        $"{LocalizationUtils.GetEntityLocalizationName(et)} [{Language.GetItem(Invariables.LangItem, "h12")}]",
                    Description = TypeFactory.Instance.GetSystemStyleLocalizedDescription(et),
                    Guid = "00000000-0000-0000-0000-000000000000",
                    StyleType = StyleType.System
                };
                if (!HasStyle(systemStyle))
                {
                    FillStyleDefaultProperties(systemStyle, et);

                    EntityStyles.Add(systemStyle);
                }
            });
        }

        /// <summary>
        /// Наполнение стиля свойствами со значениями по умолчанию
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public static void FillStyleDefaultProperties(this SmartEntityStyle style, Type entityType)
        {
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                if (attribute != null && attribute.Name != "Style")
                {
                    if (attribute.PropertyScope != PropertyScope.PaletteAndStyleEditor)
                    {
                        continue;
                    }

                    if (attribute.Name == "Scale")
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute, 
                            entityType,
                            AcadUtils.AnnotationScaleFromString(attribute.DefaultValue.ToString()),
                            ObjectId.Null));
                    }
                    else if (attribute.Name == "LayerName")
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute, 
                            entityType,
                            AcadUtils.Layers.Contains(attribute.DefaultValue.ToString())
                                ? attribute.DefaultValue
                                : Language.GetItem(Invariables.LangItem, "defl"),
                            ObjectId.Null));
                    }
                    else
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute,
                            entityType,
                            attribute.DefaultValue,
                            ObjectId.Null));
                    }
                }
            }
        }

        /// <summary>
        /// Чтение свойств из примитива и запись их в стиль примитива
        /// </summary>
        /// <param name="style">Стиль примитива</param>
        /// <param name="entity">Интеллектуальный примитив</param>
        /// <param name="blockReference">Вставка блока, представляющая интеллектуальный примитив в AutoCAD</param>
        public static void GetPropertiesFromEntity(this SmartEntityStyle style, SmartEntity entity, BlockReference blockReference)
        {
            var entityType = entity.GetType();
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                if (attribute != null && attribute.Name != "Style")
                {
                    if (attribute.PropertyScope != PropertyScope.PaletteAndStyleEditor)
                    {
                        continue;
                    }

                    if (attribute.Name == "LayerName")
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute, 
                            entityType,
                            blockReference.Layer,
                            ObjectId.Null));
                    }
                    else if (attribute.Name == "LineType")
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute,
                            entityType,
                            blockReference.Linetype,
                            ObjectId.Null));
                    }
                    else
                    {
                        style.Properties.Add(new SmartEntityProperty(
                            attribute,
                            entityType,
                            propertyInfo.GetValue(entity),
                            ObjectId.Null));
                    }
                }
            }
        }

        private static bool HasStyle(SmartEntityStyle style)
        {
            return EntityStyles.Any(s => s.EntityType == style.EntityType &&
                                         s.StyleType == style.StyleType &&
                                         s.Guid == style.Guid);
        }

        /// <summary>
        /// Загрузка пользовательских стилей из xml-файла для указанного типа примитива
        /// </summary>
        /// <param name="entityType">Тип примитива</param>
        private static void LoadStylesFromXmlFile(Type entityType)
        {
            var stylesFile = Path.Combine(MainFunction.StylesPath, $"{entityType.Name}Styles.xml");
            if (File.Exists(stylesFile))
            {
                for (var i = EntityStyles.Count - 1; i >= 0; i--)
                {
                    var style = EntityStyles[i];
                    if (style.StyleType == StyleType.System)
                    {
                        continue;
                    }

                    if (style.EntityType != entityType)
                    {
                        continue;
                    }

                    EntityStyles.RemoveAt(i);
                }

                var fXel = XElement.Load(stylesFile);
                foreach (var styleXel in fXel.Elements("UserStyle"))
                {
                    var style = new SmartEntityStyle(entityType)
                    {
                        StyleType = StyleType.User
                    };
                    style.Name = styleXel.Attribute(nameof(style.Name))?.Value;
                    style.Description = styleXel.Attribute(nameof(style.Description))?.Value;

                    // Guid беру, если есть атрибут. Иначе создаю новый
                    var guidAttr = styleXel.Attribute(nameof(style.Guid));
                    style.Guid = guidAttr?.Value ?? Guid.NewGuid().ToString();

                    if (HasStyle(style))
                    {
                        continue;
                    }

                    // get layer xml data
                    var layerData = styleXel.Element("LayerTableRecord");
                    style.LayerXmlData = layerData ?? null;

                    // get text style xml data
                    var textStyleData = styleXel.Element("TextStyleTableRecord");
                    style.TextStyleXmlData = textStyleData ?? null;

                    // get properties from file
                    foreach (var propertyInfo in entityType.GetProperties())
                    {
                        var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                        if (attribute != null)
                        {
                            var xmlProperty = styleXel.Elements("Property").FirstOrDefault(e => e.Attribute("Name")?.Value == attribute.Name);
                            if (xmlProperty != null)
                            {
                                var xmlValue = xmlProperty.Attribute("Value")?.Value;
                                var valueType = attribute.DefaultValue.GetType();
                                if (attribute.Name == "Scale")
                                {
                                    style.Properties.Add(new SmartEntityProperty(
                                        attribute, 
                                        entityType,
                                        AcadUtils.AnnotationScaleFromString(xmlValue),
                                        ObjectId.Null));
                                }
                                else
                                {
                                    if (valueType == typeof(string))
                                    {
                                        style.Properties.Add(new SmartEntityProperty(
                                            attribute,
                                            entityType,
                                            xmlProperty.Attribute("Value").Value,
                                            ObjectId.Null));
                                    }
                                    else if (valueType == typeof(int))
                                    {
                                        style.Properties.Add(new SmartEntityProperty(
                                            attribute, 
                                            entityType,
                                            int.TryParse(xmlValue, out var i) ? i : attribute.DefaultValue,
                                            ObjectId.Null));
                                    }
                                    else if (valueType == typeof(double))
                                    {
                                        style.Properties.Add(new SmartEntityProperty(
                                            attribute, 
                                            entityType,
                                            double.TryParse(xmlValue, out var d) ? d : attribute.DefaultValue,
                                            ObjectId.Null));
                                    }
                                    else if (valueType == typeof(bool))
                                    {
                                        style.Properties.Add(new SmartEntityProperty(
                                            attribute, 
                                            entityType,
                                            bool.TryParse(xmlValue, out var b) ? b : attribute.DefaultValue,
                                            ObjectId.Null));
                                    }
                                    else if (valueType.IsEnum)
                                    {
                                        try
                                        {
                                            style.Properties.Add(new SmartEntityProperty(
                                                attribute,
                                                entityType,
                                                Enum.Parse(attribute.DefaultValue.GetType(), xmlValue),
                                                ObjectId.Null));
                                        }
                                        catch
                                        {
                                            style.Properties.Add(new SmartEntityProperty(
                                                attribute, 
                                                entityType,
                                                attribute.DefaultValue,
                                                ObjectId.Null));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    style.CheckMissedProperties(entityType);

                    // add style
                    EntityStyles.Add(style);
                }
            }
        }

        /// <summary>
        /// Проверка стиля на наличие отсутствующих свойств. Свойства могут отсутствовать в случае,
        /// если стиль уже был сохранен, но позже вышло обновление с добавлением нового свойства
        /// </summary>
        /// <param name="style">Проверяемый стиль</param>
        /// <param name="entityType">Тип примитива</param>
        private static void CheckMissedProperties(this SmartEntityStyle style, Type entityType)
        {
            foreach (var propertyInfo in entityType.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                if (attribute != null && attribute.Name != "Style")
                {
                    if (attribute.PropertyScope != PropertyScope.PaletteAndStyleEditor)
                    {
                        continue;
                    }

                    // ReSharper disable once SimplifyLinqExpression
                    if (!style.Properties.Any(p => p.Name == attribute.Name))
                    {
                        style.Properties.Add(new SmartEntityProperty(
                        attribute,
                        entityType,
                        attribute.DefaultValue,
                        ObjectId.Null));
                    }
                }
            }
        }

        /// <summary>
        /// Сохранение стилей указанного типа
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public static void SaveStylesToXml(Type entityType)
        {
            var stylesFile = Path.Combine(MainFunction.StylesPath, $"{entityType.Name}Styles.xml");

            // Если файла нет, то создаем
            if (!File.Exists(stylesFile))
            {
                new XElement("Styles").Save(stylesFile);
            }

            try
            {
                var fXel = XElement.Load(stylesFile);
                fXel.RemoveAll();

                foreach (var style in EntityStyles.Where(s => s.EntityType == entityType))
                {
                    if (!style.CanEdit)
                    {
                        continue;
                    }

                    fXel.Add(style.ConvertToXElement());
                }

                fXel.Save(stylesFile);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Конвертировать стиль в XElement
        /// </summary>
        private static XElement ConvertToXElement(this SmartEntityStyle style)
        {
            var styleXel = new XElement("UserStyle");
            styleXel.SetAttributeValue(nameof(style.Name), style.Name);
            styleXel.SetAttributeValue(nameof(style.Description), style.Description);
            styleXel.SetAttributeValue(nameof(style.Guid), style.Guid);

            foreach (var entityProperty in style.Properties)
            {
                if (entityProperty.Name == "Scale")
                {
                    var propXel = new XElement("Property");
                    propXel.SetAttributeValue("Name", entityProperty.Name);
                    propXel.SetAttributeValue("Value", ((AnnotationScale)entityProperty.Value).Name);
                    styleXel.Add(propXel);
                }
                else
                {
                    var propXel = new XElement("Property");
                    propXel.SetAttributeValue("Name", entityProperty.Name);
                    propXel.SetAttributeValue("Value", entityProperty.Value);
                    styleXel.Add(propXel);
                }
            }

            if (LayerUtils.HasLayer(style.GetLayerNameProperty()))
            {
                styleXel.Add(LayerUtils.SetLayerXml(LayerUtils.GetLayerTableRecordByLayerName(style.GetLayerNameProperty())));
            }
            else if (style.LayerXmlData != null)
            {
                styleXel.Add(style.LayerXmlData);
            }

            // add text style
            if (TextStyleUtils.HasTextStyle(style.GetTextStyleProperty()))
            {
                styleXel.Add(TextStyleUtils.SetTextStyleTableRecordXElement(TextStyleUtils.GetTextStyleTableRecordByName(style.GetTextStyleProperty())));
            }
            else if (style.TextStyleXmlData != null)
            {
                styleXel.Add(style.TextStyleXmlData);
            }

            return styleXel;
        }

        /// <summary>
        /// Возвращает идентификатор текущего стиля для объекта указанного типа
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public static string GetCurrentStyleGuidForEntity(Type entityType)
        {
            return CurrentStyleGuid($"mp{entityType.Name}");
        }

        /// <summary>
        /// Получение идентификатора текущего стиля, сохраненного в настройках или идентификатор первого системного стиля
        /// </summary>
        /// <param name="styleType">Тип стиля</param>
        public static string GetCurrentStyleGuid(Type styleType)
        {
            var functionName = GetFunctionNameByStyleType(styleType);
            return CurrentStyleGuid(functionName);
        }

        private static string CurrentStyleGuid(string functionName)
        {
            var savedStyleGuid = UserConfigFile.GetValue(functionName, "CurrentStyleGuid");
            if (!string.IsNullOrEmpty(savedStyleGuid))
            {
                return savedStyleGuid;
            }

            const string firstSystemGuid = "00000000-0000-0000-0000-000000000000";
            UserConfigFile.SetValue(functionName, "CurrentStyleGuid", firstSystemGuid, true);
            return firstSystemGuid;
        }

        /// <summary>
        /// Сохранить стиль как Текущий в настройки пользователя
        /// </summary>
        /// <param name="style">Сохраняемый стиль</param>
        public static void SaveCurrentStyleToSettings(SmartEntityStyle style)
        {
            UserConfigFile.SetValue($"mp{style.EntityType.Name}", "CurrentStyleGuid", style.Guid, true);
        }

        /// <summary>
        /// Получить имя функции по имени типа стиля
        /// </summary>
        /// <param name="styleType">Тип стиля</param>
        /// <returns></returns>
        private static string GetFunctionNameByStyleType(Type styleType)
        {
            return $"mp{styleType.Name.Replace("Style", string.Empty)}";
        }

        /// <summary>
        /// Получить текущий стиль для типа примитива
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public static SmartEntityStyle GetCurrentStyle(Type entityType)
        {
            try
            {
                LoadStylesFromXmlFile(entityType);

                var currentStyleGuid = GetCurrentStyleGuid(entityType);

                foreach (var style in EntityStyles)
                {
                    if (style.EntityType == entityType &&
                        style.Guid == currentStyleGuid)
                    {
                        return style;
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }

            return EntityStyles.First(s => s.EntityType == entityType && s.StyleType == StyleType.System);
        }

        /// <summary>
        /// Получить все стили для типа примитива
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public static List<SmartEntityStyle> GetStyles(Type entityType)
        {
            LoadStylesFromXmlFile(entityType);

            return EntityStyles.Where(s => s.EntityType == entityType).ToList();
        }

        /// <summary>
        /// Возвращает имя стиля с указанными идентификатором для указанного типа примитива
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        /// <param name="guid">Идентификатор стиля</param>
        public static string GetStyleNameByGuid(Type entityType, string guid)
        {
            var style = EntityStyles.FirstOrDefault(s => s.EntityType == entityType && s.Guid == guid);
            if (style != null)
            {
                return style.Name;
            }

            // Стиль отсутствует в базе
            return Language.GetItem(Invariables.LangItem, "h103");
        }

        /// <summary>
        /// Возвращает стиль для указанного типа примитива по указанному имени стиля
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        /// <param name="styleName">Имя стиля</param>
        [CanBeNull]
        public static SmartEntityStyle GetStyleByName(Type entityType, string styleName)
        {
            return EntityStyles.FirstOrDefault(s => s.EntityType == entityType && s.Name == styleName);
        }

        /// <summary>
        /// Добавить стиль в список
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        public static void AddStyle(SmartEntityStyle style)
        {
            EntityStyles.Add(style);
        }

        /// <summary>
        /// Удаление стиля из списка
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        public static void RemoveStyle(SmartEntityStyle style)
        {
            EntityStyles.Remove(style);
        }

        /// <summary>
        /// Возвращает значение свойства "Имя слоя" из стиля
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        public static string GetLayerNameProperty(this SmartEntityStyle style)
        {
            foreach (var property in style.Properties)
            {
                if (property.Name == "LayerName")
                {
                    return property.Value.ToString();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Возвращает значение свойства "Тип линии" из стиля
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        /// <returns>В случае неудачи возвращает "Continuous"</returns>
        public static string GetLineTypeProperty(this SmartEntityStyle style)
        {
            foreach (var property in style.Properties)
            {
                if (property.Name == "LineType")
                {
                    return property.Value.ToString();
                }
            }

            return "Continuous";
        }

        /// <summary>
        /// Возвращает значение свойства "Текстовый стиль" из стиля
        /// </summary>
        /// <param name="style">Стиль <see cref="SmartEntityStyle"/></param>
        /// <returns>В случае неудачи возвращает "Standard"</returns>
        public static string GetTextStyleProperty(this SmartEntityStyle style)
        {
            foreach (var property in style.Properties)
            {
                if (property.Name == "TextStyle")
                {
                    return property.Value.ToString();
                }
            }

            return "Standard";
        }

        /// <summary>
        /// Применить к указанному интеллектуальному примитиву свойства из стиля
        /// </summary>
        /// <param name="entity">Экземпляр примитива</param>
        /// <param name="style">Стиль</param>
        /// <param name="isOnEntityCreation">True - применение стиля происходит при создании примитива.
        /// False - применение стиля происходит при выборе стиля в палитре</param>
        public static void ApplyStyle(this SmartEntity entity, SmartEntityStyle style, bool isOnEntityCreation)
        {
            var type = entity.GetType();
            foreach (var propertyInfo in type.GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                if (attribute != null)
                {
                    var propertyFromStyle = style.Properties.FirstOrDefault(sp => sp.Name == attribute.Name);
                    if (propertyFromStyle != null)
                    {
                        if (attribute.Name == "Scale")
                        {
                            if (isOnEntityCreation)
                            {
                                if (MainSettings.Instance.UseScaleFromStyle)
                                {
                                    propertyInfo.SetValue(entity, propertyFromStyle.Value);
                                }
                                else
                                {
                                    entity.Scale = AcadUtils.GetCurrentScale();
                                }
                            }
                            else
                            {
                                propertyInfo.SetValue(entity, propertyFromStyle.Value);
                            }
                        }
                        else if (attribute.Name == "LayerName")
                        {
                            var layerName = propertyFromStyle.Value.ToString();
                            if (string.IsNullOrEmpty(layerName))
                            {
                                layerName = Language.GetItem(Invariables.LangItem, "defl");
                            }

                            if (isOnEntityCreation)
                            {
                                if (MainSettings.Instance.UseLayerFromStyle)
                                {
                                    propertyInfo.SetValue(entity, layerName);
                                    AcadUtils.SetLayerByName(entity.BlockId, layerName, style.LayerXmlData);
                                }
                            }
                            else
                            {
                                propertyInfo.SetValue(entity, layerName);
                                AcadUtils.SetLayerByName(entity.BlockId, layerName, style.LayerXmlData);
                            }
                        }
                        else if (attribute.Name == "LineType")
                        {
                            var lineType = propertyFromStyle.Value.ToString();
                            AcadUtils.SetLineType(entity.BlockId, lineType);
                        }
                        else if (attribute.Name == "TextStyle")
                        {
                            var apply = false;
                            if (isOnEntityCreation)
                            {
                                if (MainSettings.Instance.UseTextStyleFromStyle)
                                {
                                    apply = true;
                                }
                            }
                            else
                            {
                                apply = true;
                            }

                            if (apply)
                            {
                                var textStyleName = propertyFromStyle.Value.ToString();
                                if (TextStyleUtils.HasTextStyle(textStyleName))
                                {
                                    propertyInfo.SetValue(entity, textStyleName);
                                }
                                else
                                {
                                    if (MainSettings.Instance.IfNoTextStyle == 1 &&
                                        TextStyleUtils.CreateTextStyle(style.TextStyleXmlData))
                                    {
                                        propertyInfo.SetValue(entity, textStyleName);
                                    }
                                }
                            }
                        }
                        else if (attribute.Name == "NumberSeparator")
                        {
                            switch (MainSettings.Instance.GlobalNumberSeparator)
                            {
                                case GlobalNumberSeparator.FromModPlusSettings:
                                    propertyInfo.SetValue(
                                        entity,
                                        Variables.Separator == "." ? NumberSeparator.Dot : NumberSeparator.Comma);
                                    break;
                                case GlobalNumberSeparator.FromStyle:
                                    propertyInfo.SetValue(entity, propertyFromStyle.Value);
                                    break;
                                case GlobalNumberSeparator.Dot:
                                    propertyInfo.SetValue(entity, NumberSeparator.Dot);
                                    break;
                                case GlobalNumberSeparator.Comma:
                                    propertyInfo.SetValue(entity, NumberSeparator.Comma);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            propertyInfo.SetValue(entity, propertyFromStyle.Value);
                        }
                    }
                }
                else
                {
                    if (propertyInfo.Name == "StyleGuid")
                    {
                        propertyInfo.SetValue(entity, style.Guid);
                    }
                }
            }
        }
    }
}