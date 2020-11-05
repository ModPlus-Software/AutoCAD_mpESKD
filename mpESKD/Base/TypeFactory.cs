namespace mpESKD.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Functions.mpAxis;
    using Functions.mpBreakLine;
    using Functions.mpGroundLine;
    using Functions.mpLevelMark;
    using Functions.mpSection;
    using Functions.mpWaterProofing;

    /// <summary>
    /// Фабрика типов интеллектуальных примитивов
    /// </summary>
    public class TypeFactory
    {
        private static TypeFactory _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static TypeFactory Instance => _instance ?? (_instance = new TypeFactory());

        /// <summary>
        /// Возвращает список типов примитивов
        /// </summary>
        public List<Type> GetEntityTypes()
        {
            return new List<Type>
            {
                typeof(BreakLine),
                typeof(Axis),
                typeof(GroundLine),
                typeof(Section),
                typeof(LevelMark),
                typeof(WaterProofing)
            };
        }

        /// <summary>
        /// Возвращает дескриптор для примитива по типу примитива
        /// </summary>
        /// <param name="entityType">Тип примитива</param>
        /// <returns></returns>
        public IIntellectualEntityDescriptor GetDescriptor(Type entityType)
        {
            if (entityType == typeof(BreakLine))
                return BreakLineDescriptor.Instance;

            if (entityType == typeof(Axis))
                return AxisDescriptor.Instance;

            if (entityType == typeof(GroundLine))
                return GroundLineDescriptor.Instance;

            if (entityType == typeof(Section))
                return SectionDescriptor.Instance;

            if (entityType == typeof(LevelMark))
                return LevelMarkDescriptor.Instance;

            if (entityType == typeof(WaterProofing))
                return WaterProofingDescriptor.Instance;

            return null;
        }

        /// <summary>
        /// Возвращает список экземпляров функций, реализующих интерфейс <see cref="ISmartEntityFunction"/>
        /// </summary>
        public List<ISmartEntityFunction> GetEntityFunctionTypes()
        {
            return new List<ISmartEntityFunction>
            {
                new BreakLineFunction(),
                new AxisFunction(),
                new GroundLineFunction(),
                new SectionFunction(),
                new LevelMarkFunction(),
                new WaterProofingFunction()
            };
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
        /// Возвращает локализованное описание базового стиля
        /// </summary>
        /// <param name="entityType">Тип интеллектуального примитива</param>
        public string GetSystemStyleLocalizedDescription(Type entityType)
        {
            switch (entityType.Name)
            {
                case nameof(BreakLine):
                    return TryGetLocalizationValue("h53");
                case nameof(Axis):
                    return TryGetLocalizationValue("h68");
                case nameof(GroundLine):
                    return TryGetLocalizationValue("h78");
                case nameof(Section):
                    return TryGetLocalizationValue("h96");
                case nameof(LevelMark):
                    return TryGetLocalizationValue("h108");
                case nameof(WaterProofing):
                    return TryGetLocalizationValue("h114");
            }

            return string.Empty;
        }

        private static string TryGetLocalizationValue(string key)
        {
            try
            {
                return ModPlusAPI.Language.GetItem(Invariables.LangItem, key);
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }
    }
}
