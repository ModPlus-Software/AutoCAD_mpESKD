namespace mpESKD.Base.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Attributes;
using Enums;

/// <summary>
/// Утилиты локализации
/// </summary>
public static class LocalizationUtils
{
    static LocalizationUtils()
    {
        if (EnumPropertiesLocalizationValues == null)
        {
            EnumPropertiesLocalizationValues = new Dictionary<Type, List<string>>();
        }

        if (CategoryLocalizationNames == null)
        {
            CategoryLocalizationNames = new Dictionary<string, string>();
        }

        if (EntityLocalizationNames == null)
        {
            EntityLocalizationNames = new Dictionary<Type, string>();
        }
    }

    /// <summary>
    /// Словарь хранения локализованных значений для свойств типа Enum чтобы не читать их из атрибутов много раз
    /// </summary>
    private static readonly Dictionary<Type, List<string>> EnumPropertiesLocalizationValues;

    /// <summary>
    /// Словарь хранения локализованных значений имени категории
    /// </summary>
    private static readonly Dictionary<string, string> CategoryLocalizationNames;

    /// <summary>
    /// Словарь хранения локализованных значений имени интеллектуального примитива
    /// </summary>
    private static readonly Dictionary<Type, string> EntityLocalizationNames;

    /// <summary>
    /// Получение локализованных значений полей перечислителя, имеющих специальный атрибут
    /// </summary>
    /// <param name="enumType">Тип перечислителя</param>
    /// <returns>Список локализованных значений или список полей в случае неудачи</returns>
    public static List<string> GetEnumPropertyLocalizationFields(Type enumType)
    {
        if (EnumPropertiesLocalizationValues.ContainsKey(enumType))
        {
            return EnumPropertiesLocalizationValues[enumType];
        }

        var enumPropertyLocalizationValues = new List<string>();
        foreach (var fieldInfo in enumType.GetFields()
                     .Where(f => f.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>() != null))
        {
            var attribute = fieldInfo.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>();
            if (attribute != null)
            {
                try
                {
                    enumPropertyLocalizationValues.Add(
                        ModPlusAPI.Language.GetItem(attribute.LocalizationKey));
                }
                catch
                {
                    // ignore
                }
            }
        }

        if (enumPropertyLocalizationValues.Any())
        {
            EnumPropertiesLocalizationValues.Add(enumType, enumPropertyLocalizationValues);
            return enumPropertyLocalizationValues;
        }

        return Enum.GetNames(enumType).ToList();
    }

    /// <summary>
    /// Получение локализованного значения имени примитива путем чтения атрибута
    /// </summary>
    /// <param name="entityType">Тип примитива</param>
    /// <returns>Локализованное значение или имя типа в случае неудачи</returns>
    public static string GetEntityLocalizationName(Type entityType)
    {
        if (EntityLocalizationNames.ContainsKey(entityType))
        {
            return EntityLocalizationNames[entityType];
        }

        var attribute = entityType.GetCustomAttribute<SmartEntityDisplayNameKeyAttribute>();
        if (attribute != null)
        {
            try
            {
                var localName = ModPlusAPI.Language.GetItem(attribute.LocalizationKey);
                if (!EntityLocalizationNames.ContainsKey(entityType))
                {
                    EntityLocalizationNames.Add(entityType, localName);
                }

                return localName;
            }
            catch
            {
                // ignore
            }
        }

        return entityType.Name;
    }

    /// <summary>
    /// Получение локализованного имени категории свойств
    /// </summary>
    /// <param name="category">Категория</param>
    /// <returns>Локализованное значение или имя категории в случае неудачи</returns>
    public static string GetCategoryLocalizationName(PropertiesCategory category)
    {
        if (CategoryLocalizationNames.ContainsKey(category.ToString()))
        {
            return CategoryLocalizationNames[category.ToString()];
        }

        var type = category.GetType();
        foreach (var fieldInfo in type.GetFields()
                     .Where(f => f.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>() != null))
        {
            if (fieldInfo.Name != category.ToString())
            {
                continue;
            }

            var attribute = fieldInfo.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>();
            if (attribute != null)
            {
                try
                {
                    var localName = ModPlusAPI.Language.GetItem(attribute.LocalizationKey);
                    if (!CategoryLocalizationNames.ContainsKey(category.ToString()))
                    {
                        CategoryLocalizationNames.Add(category.ToString(), localName);
                    }

                    return localName;
                }
                catch
                {
                    // ignore
                }
            }
        }

        return category.ToString();
    }
}