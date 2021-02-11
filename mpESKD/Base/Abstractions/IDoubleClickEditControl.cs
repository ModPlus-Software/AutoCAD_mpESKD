namespace mpESKD.Base.Abstractions
{
    using System;

    /// <summary>
    /// Элемент управления для изменения свойств интеллектуального объекта по двойному клику
    /// </summary>
    public interface IDoubleClickEditControl
    {
        /// <summary>
        /// Тип интеллектуального объекта
        /// </summary>
        Type EntityType { get; }
        
        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="smartEntity">Экземпляр интеллектуального объекта, доступный для редактирования по двойному клику</param>
        void Initialize(IWithDoubleClickEditor smartEntity);
        
        /// <summary>
        /// Применение свойств к интеллектуальному объекту
        /// </summary>
        void OnAccept();
    }
}
