namespace mpESKD.Base.Enums
{
    /// <summary>
    /// Статус выполнения <see cref="Autodesk.AutoCAD.EditorInput.EntityJig"/>
    /// </summary>
    public enum JigState
    {
        /// <summary>
        /// Запрос точки вставки
        /// </summary>
        PromptInsertPoint = 1,

        /// <summary>
        /// Запрос последующей точки
        /// </summary>
        PromptNextPoint = 2,

        /// <summary>
        /// Условная дополнительная точка
        /// </summary>
        CustomPoint = 3
    }
}
