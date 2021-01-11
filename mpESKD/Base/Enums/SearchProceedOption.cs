namespace mpESKD.Base.Enums
{
    /// <summary>
    /// Вариант обработки примитивов командой "Найти"
    /// </summary>
    public enum SearchProceedOption
    {
        /// <summary>
        /// Обновить графику
        /// </summary>
        Update = 0,
        
        /// <summary>
        /// Выбрать
        /// </summary>
        Select = 1,

        /// <summary>
        /// Удалить расширенные данные
        /// </summary>
        RemoveData = 2,

        /// <summary>
        /// Взорвать
        /// </summary>
        Explode = 3,

        /// <summary>
        /// Удалить
        /// </summary>
        Delete = 4
    }
}
