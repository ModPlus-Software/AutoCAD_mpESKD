namespace mpESKD.Functions.SearchEntitiesByValues
{
    using Autodesk.AutoCAD.Runtime;

    /// <summary>
    /// Команда поиска интеллектуальных объектов по значениям
    /// </summary>
    public static class SearchEntitiesByValuesCommand
    {
        /// <summary>
        /// Запуск команды
        /// </summary>
        [CommandMethod("ModPlus", "mpESKDSearchByValues", CommandFlags.Modal)]
        public static void Start()
        {
            var context = new SearchEntitiesByValuesViewModel();
            var win = new SearchEntitiesByValuesWindow
            {
                DataContext = context
            };

            win.Show();
        }
    }
}
