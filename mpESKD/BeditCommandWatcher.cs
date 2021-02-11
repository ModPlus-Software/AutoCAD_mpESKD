namespace mpESKD
{
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Base.Utils;

    /// <summary>
    /// Слежение за командой AutoCAD "редактор блоков"
    /// </summary>
    public class BeditCommandWatcher
    {
        /// <summary>
        /// True - использовать редактор блоков. False - не использовать
        /// </summary>
        public static bool UseBedit = true;

        /// <summary>
        /// Инициализация
        /// </summary>
        public static void Initialize()
        {
            Application.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
        }

        private static void DocumentManager_DocumentLockModeChanged(
            object sender, Autodesk.AutoCAD.ApplicationServices.DocumentLockModeChangedEventArgs e)
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
    }
}