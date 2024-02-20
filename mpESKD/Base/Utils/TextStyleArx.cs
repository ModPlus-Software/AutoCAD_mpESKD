namespace mpESKD.Base.Utils;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Runtime.InteropServices;

// https://adndevblog.typepad.com/autocad/2012/05/actual-width-and-height-of-a-text-string.html
// To get the mangled name use dumpbin.exe. For ex :
// dumpbin.exe -headers "C:\ObjectARX 2013\lib-x64\acdb19.lib" > c:\Temp\acdb19.txt
// Использовано: dumpbin.exe /exports "...\2013\lib-x64\acdb19.lib" > "...\acdb19.txt"
// Open the generated acdb19.txt to find the signature

/// <summary>
/// Методы для работы со стилями текста с использованием импортированных библиотек ObjectARX
/// </summary>
public static class TextStyleArx
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    [DllImport(
#if A2013 
        "acdb19.dll",
#elif A2014 
        "acdb19.dll", 
#elif A2015
        "acdb20.dll",
#elif A2016
        "acdb20.dll",
#elif A2017
        "acdb21.dll",
#elif A2018
        "acdb22.dll",
#elif A2019
        "acdb23.dll",
#elif A2020
        "acdb23.dll",
#elif A2021
        "acdb24.dll",
#elif A2022
        "acdb24.dll",
#elif A2023
        "acdb24.dll",
#elif A2024
        "acdb24.dll",
#endif
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")]
    private static extern ErrorStatus fromAcDbTextStyle(System.IntPtr style, ref ObjectId id);

    /// <summary>
    /// Для тестирования размеров текста, с учетом стиля текста
    /// </summary>
    [CommandMethod("mpTextTrueWidthMessage")]
    public static void TrueTextSizeMessage()
    {
#if DEBUG
        var ed = AcadUtils.Editor;

        PromptResult prText = ed.GetString("\nEnter a String: ");
        PromptResult prTextStyle = ed.GetString("\nEnter a Text Style: ");

        if (!TextStyleUtils.HasTextStyle(prTextStyle.StringResult))
        {
            AcadUtils.WriteMessageInDebug("There is no such style!");
            return;
        }

        PromptDoubleResult prTextHeight = ed.GetDouble("\nEnter a Text Height: ");

        try
        {
            if (prText.Status != PromptStatus.OK
                && prTextStyle.Status != PromptStatus.OK
                && prTextHeight.Status != PromptStatus.OK)
                return;

            var textSize = GetTrueTextSize(prText.StringResult, prTextStyle.StringResult, prTextHeight.Value);

            AcadUtils.WriteMessageInDebug($"Text: {prText.StringResult}, " +
                                          $"Height: {prTextHeight.Value}, " +
                                          $"Style: {prTextStyle.StringResult} => " +
                                          $"Width: {textSize.Item1}, " +
                                          $"Height: {textSize.Item2}");
        }
        catch (Exception e)
        {
            AcadUtils.WriteMessageInDebug($"e.Source: {e.Source}; e.StackTrace: {e.StackTrace}");
        }
#endif
    }

    /// <summary>
    /// Возвращает актуальные размеры текста, с учетом стиля текста
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="textStyleName">Стиль текста</param>
    /// <param name="textHeight">Высота текста</param>
    /// <returns>Кортеж (<see cref="double"/>, <see cref="double"/>):  (ширина_текста, высота_текста)</returns>
    /// <remarks>Применяется для многострочного текста <see cref="MText"/></remarks>
    internal static (double, double) GetTrueTextSize(string text, string textStyleName, double textHeight)
    {
        double width = 0.0;
        double height = 0.0;
        var db = AcadUtils.Database;

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            TextStyleTable textStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            if (textStyleTable != null && textStyleTable.Has(textStyleName))
            {
                var textStyleId = textStyleTable[textStyleName];
                Autodesk.AutoCAD.GraphicsInterface.TextStyle iStyle = new Autodesk.AutoCAD.GraphicsInterface.TextStyle();

                if (fromAcDbTextStyle(iStyle.UnmanagedObject, ref textStyleId) == ErrorStatus.OK)
                {
                    iStyle.TextSize = textHeight;
                    Extents2d extents = iStyle.ExtentsBox(text, false, true, null);

                    width = extents.MaxPoint.X - extents.MinPoint.X;
                    height = extents.MaxPoint.Y - extents.MinPoint.Y;
                }
            }

            tr.Commit();
        }

        return (width, height);
    }
}