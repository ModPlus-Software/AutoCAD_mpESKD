namespace mpESKD.Base.Abstractions
{
    using System.Collections.Generic;

    /// <summary>
    /// Интерфейс дескриптора функций. Класс, реализующий интерфейс, содержит описание функций.
    /// Используется при построении вкладки на ленте (Ribbon)
    /// </summary>
    public interface IIntellectualEntityDescriptor
    {
        /// <summary>
        /// Имя функции. Должно быть как имя типа примитива с приставкой "mp". Например, mpAxis, mpGroundLine и т.п.
        /// <remarks>Используется в расширенных данных (XData) для идентификации примитива и соответствует команде AutoCAD</remarks>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Локализованное (отображаемое) имя функции
        /// </summary>
        string LName { get; }

        /// <summary>
        /// Краткое описание функции (локализованное)
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Полное описание функции (локализованное)
        /// </summary>
        string FullDescription { get; }

        /// <summary>
        /// Имя файла изображения для подсказки на ленте. Изображение должно иметь расширение .png и
        /// располагаться в папке Help подпапки функции (Например, /Functions/mpBreakLine/Help)
        /// </summary>
        string ToolTipHelpImage { get; }

        /// <summary>
        /// Список имен подфункции. Аналогично <see cref="Name"/>
        /// </summary>
        List<string> SubFunctionsNames { get; }

        /// <summary>
        /// Список локализованных имен подфункций. Аналогично <see cref="LName"/>
        /// <remarks>Список обязательно должен содержать такое-же количество элементов как в списке <see cref="SubFunctionsNames"/></remarks>
        /// </summary>
        List<string> SubFunctionsLNames { get; }

        /// <summary>
        /// Список кратких описаний подфункций. Аналогично <see cref="Description"/>
        /// <remarks>Список обязательно должен содержать такое-же количество элементов как в списке <see cref="SubFunctionsNames"/></remarks>
        /// </summary>
        List<string> SubDescriptions { get; }

        /// <summary>
        /// Список полных описаний подфункций. Аналогично <see cref="FullDescription"/>
        /// <remarks>Список обязательно должен содержать такое-же количество элементов как в списке <see cref="SubFunctionsNames"/></remarks>
        /// </summary>
        List<string> SubFullDescriptions { get; }

        /// <summary>
        /// Список имен файлов изображений для подсказки в ленте. Аналогично <see cref="ToolTipHelpImage"/>
        /// <remarks>Список обязательно должен содержать такое-же количество элементов как в списке <see cref="SubFunctionsNames"/></remarks>
        /// </summary>
        List<string> SubHelpImages { get; }
    }
}
