namespace mpESKD.Base.Utils;

using ModPlusAPI.IO;

// https://adndevblog.typepad.com/autocad/2012/05/actual-width-and-height-of-a-text-string.html
// To get the mangled name use dumpbin.exe. For ex :
// dumpbin.exe -headers "C:\ObjectARX 2013\lib-x64\acdb19.lib" > c:\Temp\acdb19.txt
// Использовано: dumpbin.exe /exports "...\2013\lib-x64\acdb19.lib" > "...\acdb19.txt"
// Open the generated acdb19.txt to find the signature

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;

/// <summary>
/// Методы для работы со стилями текста с использованием импортированных библиотек ObjectARX
/// </summary>
internal static class TextStyleArx
{
    // ReSharper disable once AccessToStaticMemberViaDerivedType
    private static readonly Document Doc = Application.DocumentManager.MdiActiveDocument;
    private static readonly Database Db = Doc.Database;
    private static readonly Editor Ed = Doc.Editor;

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
        @"acdb24.dll",
#elif A2023
        "acdb24.dll",
#elif A2024
        "acdb24.dll",
#endif
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Cdecl,
#if A2013
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2014
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2015
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2016
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2017
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2018
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2019
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2020
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2021
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2022      
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2023
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#elif A2024
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
#endif
    ]
    private static extern ErrorStatus fromAcDbTextStyle(System.IntPtr style, ref ObjectId id); 

    /// <summary>
    /// Для тестирования размеров текста, с учетом стиля текста
    /// </summary>
    [CommandMethod("mpTextTrueWidthMessage")]
    public static void TrueTextSizeMessage()
    {
        PromptResult prText = Ed.GetString("\nEnter a String: ");
        PromptResult prTextStyle = Ed.GetString("\nEnter a Text Style: ");
        PromptResult prTextHeight = Ed.GetDouble("\nEnter a Text Height: ");

        if (prText.Status != PromptStatus.OK 
            && prTextStyle.Status != PromptStatus.OK 
            && prTextHeight.Status != PromptStatus.OK)
            return;

        var textSize = GetTrueTextSize(prText.StringResult, prTextStyle.StringResult, prTextHeight.StringResult.ToDouble());

        Ed.WriteMessage($"\nWidth - {textSize.Item1}\nHeight - {textSize.Item2}");
    }

    /// <summary>
    /// Возвращает актуальные размеры текста, с учетом стиля текста
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="textStyleName">Стиль текста</param>
    /// <param name="textHeight">Высота текста</param>
    /// <returns>Кортеж (<see cref="double"/> , <see cref="double"/>):  (ширина_текста , высота_текста)</returns>
    /// <remarks>Применяется для многострочного текста <see cref="MText"/></remarks>
    internal static (double, double) GetTrueTextSize(string text, string textStyleName, double textHeight)
    {
        double width = 0.0;
        double height = 0.0;

        using (Transaction tr = Db.TransactionManager.StartTransaction())
        {
            TextStyleTable textStyleTable = tr.GetObject(Db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

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