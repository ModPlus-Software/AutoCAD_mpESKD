namespace mpESKD.Base.Utils;

using System;
using System.Xml.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

/// <summary>
/// Утилиты работы с текстовыми стилями
/// </summary>
public static class TextStyleUtils
{
    /// <summary>Проверка наличия текстового стиля в текущем документе</summary>
    /// <param name="textStyleName">Имя текстового стиля</param>
    public static bool HasTextStyle(string textStyleName)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var textStyleTable = (TextStyleTable)tr.GetObject(AcadUtils.Database.TextStyleTableId, OpenMode.ForWrite);
                if (textStyleTable.Has(textStyleName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Создать текстовый стиль по данным из Xml
    /// </summary>
    /// <param name="textStyleXmlData">Xml данные текстового стиля</param>
    public static bool CreateTextStyle(XElement textStyleXmlData)
    {
        var textStyle = GetTextStyleTableRecordFromXElement(textStyleXmlData);
        var textStyleCreated = false;
        if (textStyle != null)
        {
            using (AcadUtils.Document.LockDocument())
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    using (var txtStyleTbl = tr.GetObject(AcadUtils.Database.TextStyleTableId, OpenMode.ForWrite) as TextStyleTable)
                    {
                        if (txtStyleTbl != null)
                        {
                            if (!txtStyleTbl.Has(textStyle.Name))
                            {
                                txtStyleTbl.Add(textStyle);
                                tr.AddNewlyCreatedDBObject(textStyle, true);
                                textStyleCreated = true;
                            }
                            else
                            {
                                textStyleCreated = true;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        return textStyleCreated;
    }

    public static TextStyleTableRecord GetTextStyleTableRecordByName(string textStyleName)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var textStyleTable = (TextStyleTable)tr.GetObject(AcadUtils.Database.TextStyleTableId, OpenMode.ForWrite);
                foreach (var objectId in textStyleTable)
                {
                    var textStyleTableRecord = tr.GetObject(objectId, OpenMode.ForRead) as TextStyleTableRecord;
                    if (textStyleTableRecord != null && textStyleTableRecord.Name.Equals(textStyleName))
                    {
                        return textStyleTableRecord;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>Создать текстовый стиль по данным из XElement</summary>
    /// <param name="textStyleTableRecordXElement">XElement, описывающий текстовый стиль</param>
    public static TextStyleTableRecord GetTextStyleTableRecordFromXElement(XElement textStyleTableRecordXElement)
    {
        // font
        var font = new FontDescriptor(
            textStyleTableRecordXElement.Element("Font")?.Attribute("TypeFace")?.Value,
            bool.TryParse(textStyleTableRecordXElement.Element("Font")?.Attribute("Bold")?.Value, out var b) && b,
            bool.TryParse(textStyleTableRecordXElement.Element("Font")?.Attribute("Italic")?.Value, out b) && b,
            int.TryParse(textStyleTableRecordXElement.Element("Font")?.Attribute("CharacterSet")?.Value, out var i) ? i : int.MinValue,
            int.TryParse(textStyleTableRecordXElement.Element("Font")?.Attribute("PitchAndFamily")?.Value, out i) ? i : int.MinValue);

        // textStyle
        var returnedTextStyle = new TextStyleTableRecord
        {
            Font = font,
            Name = textStyleTableRecordXElement.Attribute("Name")?.Value,
            BigFontFileName = textStyleTableRecordXElement.Attribute("BigFontFileName")?.Value,
            FileName = textStyleTableRecordXElement.Attribute("FileName")?.Value,
            IsShapeFile = bool.TryParse(textStyleTableRecordXElement.Attribute("IsShapeFile")?.Value, out b) && b,
            IsVertical = bool.TryParse(textStyleTableRecordXElement.Attribute("IsVertical")?.Value, out b) && b,
            FlagBits = byte.TryParse(textStyleTableRecordXElement.Attribute("FlagBits")?.Value, out var bt) ? bt : byte.MinValue,
            ObliquingAngle = double.TryParse(textStyleTableRecordXElement.Attribute("ObliquingAngle")?.Value, out var d) ? d : double.MinValue,
            PriorSize = double.TryParse(textStyleTableRecordXElement.Attribute("PriorSize")?.Value, out d) ? d : double.MinValue,
            TextSize = double.TryParse(textStyleTableRecordXElement.Attribute("TextSize")?.Value, out d) ? d : double.MinValue,
            XScale = double.TryParse(textStyleTableRecordXElement.Attribute("XScale")?.Value, out d) ? d : double.MinValue,
            Annotative = Enum.TryParse(textStyleTableRecordXElement.Attribute("Annotative")?.Value, out AnnotativeStates a) ? a : AnnotativeStates.False,
            HasSaveVersionOverride = bool.TryParse(textStyleTableRecordXElement.Attribute("HasSaveVersionOverride")?.Value, out b) && b
        };
        returnedTextStyle.SetPaperOrientation(bool.TryParse(textStyleTableRecordXElement.Attribute("PaperOrientation")?.Value, out b) && b);

        return returnedTextStyle;
    }

    /// <summary>Сохранить текстовый стиль в XElement</summary>
    /// <param name="textStyleTableRecord">Текстовый стиль</param>
    public static XElement SetTextStyleTableRecordXElement(TextStyleTableRecord textStyleTableRecord)
    {
        var returnedXml = new XElement("TextStyleTableRecord");
        returnedXml.SetAttributeValue("Name", textStyleTableRecord.Name); // string
        returnedXml.SetAttributeValue("BigFontFileName", textStyleTableRecord.BigFontFileName); // string
        returnedXml.SetAttributeValue("FileName", textStyleTableRecord.FileName); // string
        returnedXml.SetAttributeValue("IsShapeFile", textStyleTableRecord.IsShapeFile); // bool
        returnedXml.SetAttributeValue("IsVertical", textStyleTableRecord.IsVertical); // bool
        returnedXml.SetAttributeValue("FlagBits", textStyleTableRecord.FlagBits); // byte
        returnedXml.SetAttributeValue("ObliquingAngle", textStyleTableRecord.ObliquingAngle); // double
        returnedXml.SetAttributeValue("PriorSize", textStyleTableRecord.PriorSize); // double
        returnedXml.SetAttributeValue("TextSize", textStyleTableRecord.TextSize); // double
        returnedXml.SetAttributeValue("XScale", textStyleTableRecord.XScale); // double
        returnedXml.SetAttributeValue("Annotative", textStyleTableRecord.Annotative); // AnnotativeState
        returnedXml.SetAttributeValue("HasSaveVersionOverride", textStyleTableRecord.HasSaveVersionOverride); // bool
        returnedXml.SetAttributeValue("PaperOrientation", textStyleTableRecord.PaperOrientation); // bool

        // font
        var font = new XElement("Font");
        font.SetAttributeValue("TypeFace", textStyleTableRecord.Font.TypeFace); // string
        font.SetAttributeValue("Bold", textStyleTableRecord.Font.Bold); // bool
        font.SetAttributeValue("Italic", textStyleTableRecord.Font.Italic); // bool
        font.SetAttributeValue("CharacterSet", textStyleTableRecord.Font.CharacterSet); // int
        font.SetAttributeValue("PitchAndFamily", textStyleTableRecord.Font.PitchAndFamily); // int
        returnedXml.Add(font);
        return returnedXml;
    }
}