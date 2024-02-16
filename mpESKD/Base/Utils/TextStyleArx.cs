namespace mpESKD.Base.Utils;

// https://adndevblog.typepad.com/autocad/2012/05/actual-width-and-height-of-a-text-string.html
// To get the mangled name use dumpbin.exe. For ex :
// dumpbin.exe -headers "C:\ObjectARX 2013\lib-x64\acdb19.lib" > c:\Temp\acdb19.txt
// Open the generated acdb19.txt to find the signature

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Runtime.InteropServices;
using System;
using Autodesk.AutoCAD.ApplicationServices;

/// <summary>
/// todo 0
/// </summary>
internal static class TextStyleArx
{
    private static Document _doc = Application.DocumentManager.MdiActiveDocument;
    private static Database _db = _doc.Database;
    private static Editor _ed = _doc.Editor;

    [System.Security.SuppressUnmanagedCodeSecurity]
    [DllImport("acdb24.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "?fromAcDbTextStyle@@YA?AW4ErrorStatus@Acad@@AEAVAcGiTextStyle@@AEBVAcDbObjectId@@@Z")
    ]
    private static extern ErrorStatus fromAcDbTextStyle(System.IntPtr style, ref ObjectId id);

    [CommandMethod("mpTextTrueWidthMessage")]
    public static void TrueTextSizeMessage()
    {
        PromptResult prText = _ed.GetString("\nEnter a string: ");
        PromptResult prTextStyle = _ed.GetString("\nEnter a TextStyle: ");

        if (prText.Status != PromptStatus.OK && prTextStyle.Status != PromptStatus.OK)
            return;

        var textSize = GetTrueTextSize(prText.StringResult, prTextStyle.StringResult);

        _ed.WriteMessage($"\nWidth - {textSize.Item1}\nHeight - {textSize.Item2}");
    }

    /// <summary>
    /// todo 1
    /// </summary>
    /// <param name="text"></param>
    /// <param name="textStyleName"></param>
    /// <returns>Кортеж (ширина_текста, высота_текста)</returns>
    internal static (double, double) GetTrueTextSize(string text, string textStyleName)

    {
        ObjectId textStyleId = ObjectId.Null;
        double width = 0.0;
        double height = 0.0;

        using (Transaction tr = _db.TransactionManager.StartTransaction())
        {
            TextStyleTable textStyleTable = tr.GetObject
            (
                _db.TextStyleTableId,
                OpenMode.ForRead
            ) as TextStyleTable;

            if (textStyleTable.Has(textStyleName))
            {
                textStyleId = textStyleTable[textStyleName];

                Autodesk.AutoCAD.GraphicsInterface.TextStyle iStyle
                    = new Autodesk.AutoCAD.GraphicsInterface.TextStyle();

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