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
        private Dictionary<Type, ISmartEntityDescriptor> _entityDescriptors;
        private Dictionary<Type, IDoubleClickEditControl> _entityEditControls;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static TypeFactory Instance => _instance ?? (_instance = new TypeFactory());

        /// <summary>
        /// Возвращает список типов интеллектуальных объектов
        /// </summary>
        public List<Type> GetEntityTypes()
        {
            return _entityTypes ?? (_entityTypes = typeof(TypeFactory).Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(SmartEntity)))
                .ToList());
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
    }
}