namespace mpESKD;

using Autodesk.AutoCAD.ApplicationServices;
using Base.Utils;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

/// <summary>
/// Сервис слежения за командами
/// </summary>
public static class CommandsWatcher
{
    /// <summary>
    /// True - использовать редактор блоков. False - не использовать
    /// </summary>
    public static bool UseBedit { get; set; } = true;

    /// <summary>
    /// Возвращает true, если в данный момент выполняется команда MIRROR
    /// </summary>
    public static bool Mirroring { get; private set; }

    /// <summary>
    /// Возвращает true, если в данный момент выполняется команда ROTATE
    /// </summary>
    public static bool Rotation { get; private set; }

    /// <summary>
    /// Инициализация
    /// </summary>
    public static void Initialize()
    {
        AcApp.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
    }

    /// <summary>
    /// Подписаться на события документа, связанные с командами
    /// </summary>
    /// <param name="doc">Документ</param>
    public static void SubscribeDocument(Document doc)
    {
        doc.CommandWillStart -= CommandWillStart;
        doc.CommandWillStart += CommandWillStart;
        doc.CommandEnded -= CommandEnded;
        doc.CommandEnded += CommandEnded;
        doc.CommandCancelled -= CommandCancelled;
        doc.CommandCancelled += CommandCancelled;
    }

    private static void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
    {
        try
        {
            if (!UseBedit)
            {
                if (e.GlobalCommandName == "BEDIT")
                {
                    e.Veto();
                }
            }
        }
        catch (System.Exception exception)
        {
            AcadUtils.WriteMessageInDebug($"\nException {exception.Message}");
        }
    }

    private static void CommandCancelled(object sender, CommandEventArgs e)
    {
        Mirroring = false;
        Rotation = false;
    }

    private static void CommandEnded(object sender, CommandEventArgs e)
    {
        Mirroring = false;
        Rotation = false;

        if (e.GlobalCommandName is "REGEN" or "REGENALL")
        {
            SmartEntityUtils.UpdateSmartObjects(false);
        }
    }

    private static void CommandWillStart(object sender, CommandEventArgs e)
    {
        if (e.GlobalCommandName == "MIRROR")
        {
            Mirroring = true;
        }
        else if (e.GlobalCommandName == "ROTATE")
        {
            Rotation = true;
        }
    }
}