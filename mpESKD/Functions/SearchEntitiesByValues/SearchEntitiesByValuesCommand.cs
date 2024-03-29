namespace mpESKD.Functions.SearchEntitiesByValues;

using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

/// <summary>
/// Команда поиска интеллектуальных объектов по значениям
/// </summary>
public static class SearchEntitiesByValuesCommand
{
    private static SearchEntitiesByValuesWindow _win;

    /// <summary>
    /// Запуск команды
    /// </summary>
    [CommandMethod("ModPlus", "mpESKDSearchByValues", CommandFlags.Modal)]
    public static void Start()
    {
        if (_win == null)
        {
            var context = new SearchEntitiesByValuesViewModel();
            _win = new SearchEntitiesByValuesWindow
            {
                DataContext = context
            };

            _win.Closed += (_, _) => _win = null;

            AcApp.ShowModelessWindow(_win);
        }
        else
        {
            _win.Activate();
        }
    }
}