namespace mpESKD.Base.Utils;

// https://adndevblog.typepad.com/autocad/2012/05/actual-width-and-height-of-a-text-string.html
// To get the mangled name use dumpbin.exe. For ex :
// dumpbin.exe -headers "C:\ObjectARX 2013\lib-x64\acdb19.lib" > c:\Temp\acdb19.txt
// Open the generated acdb19.txt to find the signature

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;

/// <summary>
/// todo 0
/// </summary>
internal static class TextStyleArx
{
    // ReSharper disable once AccessToStaticMemberViaDerivedType
    private static readonly Document Doc = Application.DocumentManager.MdiActiveDocument;
    private static readonly Database Db = Doc.Database;
    private static readonly Editor Ed = Doc.Editor;

    [System.Security.SuppressUnmanagedCodeSecurity]
    [DllImport("acdb24.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
    ]
    private static extern ErrorStatus fromAcDbTextStyle(System.IntPtr style, ref ObjectId id); // todo fromAcDbTextStyle ? так это для DBText? поискать для MText

    /// <summary>
    /// Для тестирования размеров текста, с учетом стиля текста
    /// </summary>
    [CommandMethod("mpTextTrueWidthMessage")]
    public static void TrueTextSizeMessage()
    {
        PromptResult prText = Ed.GetString("\nEnter a string: ");
        PromptResult prTextStyle = Ed.GetString("\nEnter a TextStyle: ");

        if (prText.Status != PromptStatus.OK && prTextStyle.Status != PromptStatus.OK)
            return;

        var textSize = GetTrueTextSize(prText.StringResult, prTextStyle.StringResult);

        Ed.WriteMessage($"\nWidth - {textSize.Item1}\nHeight - {textSize.Item2}");
    }

    /// <summary>
    /// Возвращает истинные размеры текста, с учетом стиля текста
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="textStyleName">Стиль текста</param>
    /// <returns>Кортеж (<see cref="double"/> , <see cref="double"/>):  (ширина_текста , высота_текста)</returns>
    /// <remarks>Применяется для многострочного текста <see cref="MText"/></remarks>
    internal static (double, double) GetTrueTextSize(string text, string textStyleName)
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