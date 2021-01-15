namespace mpESKD.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;

    /// <summary>
    /// Фабрика типов интеллектуальных примитивов
    /// </summary>
    public class TypeFactory
    {
        private static TypeFactory _instance;
        private List<Type> _entityTypes;
        private List<ISmartEntityFunction> _smartEntityFunctions;
        private Dictionary<Type, IIntellectualEntityDescriptor> _entityDescriptors;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static TypeFactory Instance => _instance ?? (_instance = new TypeFactory());

        /// <summary>
        /// Возвращает список типов примитивов
        /// </summary>
        public List<Type> GetEntityTypes()
        {
            return _entityTypes ?? (_entityTypes = typeof(TypeFactory).Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(SmartEntity)))
                .ToList());
        }

        /// <summary>
        /// Возвращает дескриптор для примитива по типу примитива
        /// </summary>
        /// <param name="entityType">Тип примитива</param>
        /// <returns></returns>
        public IIntellectualEntityDescriptor GetDescriptor(Type entityType)
        {
            if (_entityDescriptors == null)
            {
                _entityDescriptors = typeof(TypeFactory).Assembly
                    .GetTypes()
                    .Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(IIntellectualEntityDescriptor)))
                    .Select(Activator.CreateInstance)
                    .Cast<IIntellectualEntityDescriptor>()
                    .ToDictionary(d => d.EntityType, d => d);
            }

            return _entityDescriptors[entityType];
        }

        /// <summary>
        /// Возвращает список экземпляров функций, реализующих интерфейс <see cref="ISmartEntityFunction"/>
        /// </summary>
        public List<ISmartEntityFunction> GetEntityFunctionTypes()
        {
            return _smartEntityFunctions ?? (_smartEntityFunctions = typeof(TypeFactory).Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(ISmartEntityFunction)))
                .Select(Activator.CreateInstance)
                .Cast<ISmartEntityFunction>()
                .ToList());
        }

        /// <summary>
        /// Возвращает список имен команд. Имя команды - это имя типа примитива с приставкой "mp"
        /// <remarks>Используется в расширенных данных (XData) блоков</remarks>
        /// </summary>
        public List<string> GetEntityCommandNames()
        {
            return GetEntityTypes().Select(t => $"mp{t.Name}").ToList();
        }
    }
}