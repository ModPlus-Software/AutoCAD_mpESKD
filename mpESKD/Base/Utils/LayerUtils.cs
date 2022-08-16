namespace mpESKD.Base.Utils;

using System;
using System.Xml.Linq;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

/// <summary>
/// Утилиты работы со слоями
/// </summary>
public static class LayerUtils
{
    /// <summary>Проверка наличия слоя в документе по имени</summary>
    /// <param name="layerName">Имя слоя</param>
    public static bool HasLayer(string layerName)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var lt = tr.GetObject(AcadUtils.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt != null)
                {
                    if (lt.Has(layerName))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Получение LayerTableRecord по имени слоя</summary>
    /// <param name="layerName">Имя слоя</param>
    public static LayerTableRecord GetLayerTableRecordByLayerName(string layerName)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var lt = tr.GetObject(AcadUtils.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt != null)
                {
                    foreach (var layerId in lt)
                    {
                        var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layer != null && layer.Name.Equals(layerName))
                        {
                            return layer;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Проверка не заблокирован ли слой
    /// </summary>
    /// <param name="layerName">Имя слоя</param>
    public static bool IsLockedLayer(string layerName)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var lt = tr.GetObject(AcadUtils.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt != null)
                {
                    foreach (var layerId in lt)
                    {
                        var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layer != null && layer.Name.Equals(layerName))
                        {
                            return layer.IsLocked;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Создание слоя в текущем документе по данным, сохраненным в файле стилей
    /// </summary>
    /// <param name="layerXElement">Xml layer data node</param>
    public static bool AddLayerFromXElement(XElement layerXElement)
    {
        var layer = GetLayerFromXml(layerXElement);
        var layerCreated = false;
        if (layer != null)
        {
            using (AcadUtils.Document.LockDocument())
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    using (var lyrTbl = tr.GetObject(AcadUtils.Database.LayerTableId, OpenMode.ForWrite) as LayerTable)
                    {
                        if (lyrTbl != null)
                        {
                            if (!lyrTbl.Has(layer.Name))
                            {
                                lyrTbl.Add(layer);
                                tr.AddNewlyCreatedDBObject(layer, true);
                                layerCreated = true;
                            }
                            else
                            {
                                layerCreated = true;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        return layerCreated;
    }

    /// <summary>
    /// save layers to xml
    /// </summary>
    /// <param name="layerTableRecord">Instance of <see cref="LayerTableRecord"/></param>
    /// <returns></returns>
    public static XElement SetLayerXml(LayerTableRecord layerTableRecord)
    {
        if (layerTableRecord == null)
        {
            return new XElement("LayerTableRecord");
        }

        using (AcadUtils.Document.LockDocument())
        {
            var layerTableRecordXml = new XElement("LayerTableRecord");
            layerTableRecordXml.SetAttributeValue("Name", layerTableRecord.Name); // string
            layerTableRecordXml.SetAttributeValue("IsFrozen", layerTableRecord.IsFrozen); // bool
            layerTableRecordXml.SetAttributeValue("IsHidden", layerTableRecord.IsHidden); // bool
            layerTableRecordXml.SetAttributeValue("Description", layerTableRecord.Description); // string
            layerTableRecordXml.SetAttributeValue("IsLocked", layerTableRecord.IsLocked); // bool
            layerTableRecordXml.SetAttributeValue("IsOff", layerTableRecord.IsOff); // bool
            layerTableRecordXml.SetAttributeValue("IsPlottable", layerTableRecord.IsPlottable); // bool
            layerTableRecordXml.SetAttributeValue("Color", layerTableRecord.Color.ColorIndex); // color
            layerTableRecordXml.SetAttributeValue("Linetype", AcadUtils.GetLineTypeName(layerTableRecord.LinetypeObjectId)); // ObjectId
            layerTableRecordXml.SetAttributeValue("LineWeight", layerTableRecord.LineWeight); // LineWeight
            layerTableRecordXml.SetAttributeValue("ViewportVisibilityDefault", layerTableRecord.ViewportVisibilityDefault); // bool

            return layerTableRecordXml;
        }
    }

    // get layers from xml
    private static LayerTableRecord GetLayerFromXml(XElement layerXElement)
    {
        var layerTblR = new LayerTableRecord();
        var nameAttr = layerXElement?.Attribute("Name");

        // string
        if (nameAttr != null)
        {
            layerTblR.Name = nameAttr.Value;
            layerTblR.Description = layerXElement.Attribute("Description")?.Value;

            // bool
            layerTblR.IsFrozen = bool.TryParse(layerXElement.Attribute("IsFrozen")?.Value, out var b) && b;
            layerTblR.IsHidden = bool.TryParse(layerXElement.Attribute("IsHidden")?.Value, out b) && b;
            layerTblR.IsLocked = bool.TryParse(layerXElement.Attribute("IsLocked")?.Value, out b) && b;
            layerTblR.IsOff = bool.TryParse(layerXElement.Attribute("IsOff")?.Value, out b) && b;
            layerTblR.IsPlottable = bool.TryParse(layerXElement.Attribute("IsPlottable")?.Value, out b) && b;
            layerTblR.ViewportVisibilityDefault =
                bool.TryParse(layerXElement.Attribute("ViewportVisibilityDefault")?.Value, out b) && b;

            // color
            layerTblR.Color = Color.FromColorIndex(
                ColorMethod.ByAci,
                short.TryParse(layerXElement.Attribute("Color")?.Value, out var sh) ? sh : short.Parse("7"));

            // linetype
            layerTblR.LinetypeObjectId = AcadUtils.GetLineTypeObjectId(layerXElement.Attribute("Linetype")?.Value);

            // LineWeight
            layerTblR.LineWeight = Enum.TryParse(layerXElement.Attribute("LineWeight")?.Value, out LineWeight lw) ? lw : LineWeight.ByLineWeightDefault;

            return layerTblR;
        }

        return null;
    }
}