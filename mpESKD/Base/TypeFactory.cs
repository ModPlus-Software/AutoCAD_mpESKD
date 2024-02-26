namespace mpESKD.Base;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using Abstractions;

/// <summary>
/// Фабрика типов интеллектуальных примитивов
/// </summary>
public class TypeFactory
{
    private static TypeFactory _instance;
    private List<Type> _entityTypes;
    private List<ISmartEntityFunction> _smartEntityFunctions;
    private Dictionary<Type, ISmartEntityDescriptor> _entityDescriptors;
    private Dictionary<Type, IDoubleClickEditControl> _entityEditControls;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static TypeFactory Instance => _instance ??= new TypeFactory();

    /// <summary>
    /// Возвращает список типов интеллектуальных объектов
    /// </summary>
    public List<Type> GetEntityTypes()
    {
        return _entityTypes ??= typeof(TypeFactory).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(SmartEntity)))
            .ToList();
    }

    /// <summary>
    /// Возвращает дескриптор для примитива по типу интеллектуального объекта
    /// </summary>
    /// <param name="entityType">Тип интеллектуального объекта</param>
    public ISmartEntityDescriptor GetDescriptor(Type entityType)
    {
        if (_entityDescriptors == null)
        {
            _entityDescriptors = typeof(TypeFactory).Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(ISmartEntityDescriptor)))
                .Select(Activator.CreateInstance)
                .Cast<ISmartEntityDescriptor>()
                .ToDictionary(d => d.EntityType, d => d);
        }

        return _entityDescriptors[entityType];
    }

    /// <summary>
    /// Возвращает список экземпляров функций, реализующих интерфейс <see cref="ISmartEntityFunction"/>
    /// </summary>
    public List<ISmartEntityFunction> GetEntityFunctionTypes()
    {
        return _smartEntityFunctions ??= typeof(TypeFactory).Assembly
            .GetTypes()
            .Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(ISmartEntityFunction)))
            .Select(Activator.CreateInstance)
            .Cast<ISmartEntityFunction>()
            .ToList();
    }

    /// <summary>
    /// Возвращает список имен команд. Имя команды - это имя типа примитива с приставкой "mp"
    /// <remarks>Используется в расширенных данных (XData) блоков</remarks>
    /// </summary>
    public List<string> GetEntityCommandNames()
    {
        return GetEntityTypes().Select(t => $"mp{t.Name}").ToList();
    }

    /// <summary>
    /// Возвращает список имен команд, реализующих ITextValueEntity.
    /// Имя команды - это имя типа примитива с приставкой "mp"
    /// <remarks>Используется в расширенных данных (XData) блоков</remarks>
    /// </summary>
    public List<string> GetTextualEntityCommandNames()
    {
        return GetEntityTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ITextValueEntity)))
            .Select(t => $"mp{t.Name}")
            .ToList();
    }

    /// <summary>
    /// Возвращает экземпляр редактора по двойному клику для типа интеллектуального объекта
    /// </summary>
    /// <param name="entityType">Тип интеллектуального объекта</param>
    public IDoubleClickEditControl GetClickEditControl(Type entityType)
    {
        if (_entityEditControls == null)
        {
            _entityEditControls = typeof(TypeFactory).Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(IDoubleClickEditControl)))
                .Select(Activator.CreateInstance)
                .Cast<IDoubleClickEditControl>()
                .ToDictionary(c => c.EntityType, c => c);
        }

        return _entityEditControls[entityType];
    }

    /// <summary>
    /// Возвращает ресурсы изображений предварительного просмотра для редактора стилей
    /// </summary>
    public IEnumerable<ResourceDictionary> GetPreviewImageResourceDictionaries()
    {
        var resourcePaths = GetResourcePaths(Assembly.GetExecutingAssembly()).ToList();
        foreach (var commandName in GetEntityCommandNames())
        {
            if (resourcePaths.Contains($"Functions/{commandName}/{commandName.Substring(2)}.baml".ToLowerInvariant()))
            {
                yield return new ResourceDictionary
                {
                    Source = new Uri(
                        $"pack://application:,,,/mpESKD_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Functions/{commandName}/{commandName.Substring(2)}.xaml")
                };
            }
        }
    }

    private static IEnumerable<object> GetResourcePaths(Assembly assembly)
    {
        var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
        var resourceName = assembly.GetName().Name + ".g";
        var resourceManager = new ResourceManager(resourceName, assembly);

        try
        {
            var resourceSet = resourceManager.GetResourceSet(culture, true, true);

            foreach (System.Collections.DictionaryEntry resource in resourceSet)
            {
                yield return resource.Key;
            }
        }
        finally
        {
            resourceManager.ReleaseAllResources();
        }
    }
}