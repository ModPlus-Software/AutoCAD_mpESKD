namespace mpESKD.Base.Utils;

using System;
using Abstractions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using View;

/// <summary>
/// Утилиты для объектов
/// </summary>
public static class EntityUtils
{
    /// <summary>
    /// Установка свойств для однострочного текста
    /// </summary>
    /// <param name="dbText">Однострочный текст</param>
    /// <param name="textStyle">имя текстового стиля</param>
    /// <param name="height">Высота текста (с учетом масштаба блока)</param>
    public static void SetProperties(this DBText dbText, string textStyle, double height)
    {
        dbText.Height = height;
        dbText.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
        dbText.Linetype = "ByBlock";
        dbText.LineWeight = LineWeight.ByBlock;
        dbText.TextStyleId = AcadUtils.GetTextStyleIdByName(textStyle);
    }

    /// <summary>
    /// Установить позицию для однострочного текста
    /// </summary>
    /// <param name="dbText">Однострочный текст</param>
    /// <param name="horizontalMode">Выравнивание по горизонтали</param>
    /// <param name="verticalMode">Выравнивание по вертикали</param>
    /// <param name="attachmentPoint">Привязка к точке вставки</param>
    public static void SetPosition(
        this DBText dbText,
        TextHorizontalMode? horizontalMode = null,
        TextVerticalMode? verticalMode = null,
        AttachmentPoint? attachmentPoint = null)
    {
        if (horizontalMode.HasValue)
            dbText.HorizontalMode = horizontalMode.Value;
        if (verticalMode.HasValue)
            dbText.VerticalMode = verticalMode.Value;
        if (attachmentPoint.HasValue)
            dbText.Justify = attachmentPoint.Value;
    }

    /// <summary>
    /// Редактирование свойств для интеллектуального объекта в специальном окне. Применяется для интеллектуальных
    /// объектов, содержащих текстовые значения
    /// </summary>
    /// <param name="blockReference">Блок, представляющий интеллектуальный объект</param>
    public static void DoubleClickEdit(BlockReference blockReference)
    {
        BeditCommandWatcher.UseBedit = false;

        var smartEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
        if (smartEntity is IWithDoubleClickEditor entityWithEditor)
        {
            smartEntity.UpdateEntities();
            var saveBack = false;

            var entityType = entityWithEditor.GetType();
            var control = TypeFactory.Instance.GetClickEditControl(entityType);
            control.Initialize(entityWithEditor);
            var sectionValueEditor = new DoubleClickEditor(
                TypeFactory.Instance.GetDescriptor(entityType).LName,
                control);

            if (sectionValueEditor.ShowDialog() == true)
            {
                saveBack = true;
            }

            if (saveBack)
            {
                smartEntity.UpdateEntities();
                smartEntity.BlockRecord.UpdateAnonymousBlocks();
                using (var resBuf = smartEntity.GetDataForXData())
                {
                    blockReference.XData = resBuf;
                }
            }

            smartEntity.Dispose();
        }
    }

    /// <summary>
    /// Возвращает геометрическую длину однострочного текста
    /// </summary>
    /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
    public static double GetLength(this DBText dbText)
    {
        return Math.Abs(dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X);
    }

    /// <summary>
    /// Возвращает геометрическую высоту однострочного текста
    /// </summary>
    /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
    public static double GetHeight(this DBText dbText)
    {
        return Math.Abs(dbText.GeometricExtents.MaxPoint.Y - dbText.GeometricExtents.MinPoint.Y);
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру текста с указанным отступом
    /// </summary>
    /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
    /// <param name="offset">Отступ</param>
    public static Wipeout GetBackgroundMask(this DBText dbText, double offset)
    {
        if (dbText == null)
            return null;
        AcadUtils.WriteMessageInDebug($"\n dbText.TextString {dbText.TextString} dbText.GeometricExtents {dbText.GeometricExtents} \n");
        return GetBackgroundMask(dbText.GeometricExtents, offset);
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру текста с указанным отступом
    /// </summary>
    /// <param name="mText">Экземпляр <see cref="DBText"/></param>
    /// <param name="offset">Отступ</param>
    public static Wipeout GetBackgroundMask(this MText mText, double offset)
    {
        if (mText == null)
            return null;

        return GetBackgroundMask(mText.GeometricExtents, offset);
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру полилини
    /// </summary>
    /// <param name="polyline">Экземпляр <see cref="Polyline"/></param>
    public static Wipeout GetBackgroundMask(this Polyline polyline)
    {
        if (polyline == null)
            return null;

        var vertexCollection = new Point2dCollection();

        for (int i = 0; i < polyline.NumberOfVertices; i++)
        {
            var vertex = polyline.GetPoint2dAt(i);
            vertexCollection.Add(vertex);
        }

        vertexCollection.Add(vertexCollection[0]);

        var wipeout = new Wipeout();
        wipeout.SetFrom(vertexCollection, Vector3d.ZAxis);
        return wipeout;
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру с указанным отступом
    /// </summary>
    /// <param name="extents3d">Крайние границы</param>
    /// <param name="offset">Отступ</param>
    private static Wipeout GetBackgroundMask(Extents3d extents3d, double offset)
    {
        try
        {
            var minPoint = extents3d.MinPoint;
            var maxPoint = extents3d.MaxPoint;
            AcadUtils.WriteMessageInDebug($"minPoint {minPoint} maxPoint {maxPoint} \n");
            var bottomLeftPoint = new Point2d(minPoint.X - offset, minPoint.Y - offset);
            var topLeftPoint = new Point2d(minPoint.X - offset, maxPoint.Y + offset);
            var topRightPoint = new Point2d(maxPoint.X + offset, maxPoint.Y + offset);
            var bottomRightPoint = new Point2d(maxPoint.X + offset, minPoint.Y - offset);
            AcadUtils.WriteMessageInDebug($"\n bottomLeftPoint {bottomLeftPoint}, topLeftPoint {topLeftPoint}, topRightPoint {topRightPoint}, bottomRightPoint {bottomRightPoint} \n");
            var wipeout = new Wipeout();
            wipeout.SetFrom(
                new Point2dCollection
                {
                    bottomLeftPoint, topLeftPoint, topRightPoint, bottomRightPoint, bottomLeftPoint
                }, Vector3d.ZAxis);
            AcadUtils.WriteMessageInDebug($"wipeout.Bounds {wipeout.Bounds} \n");
            return wipeout;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception ex)
        {
            if (ex.Message == "eNullExtents")
            {
                return null;
            }

            throw;
        }
    }

    /// <summary>
    /// Возвращает номер узла в зависимости от номера узла последнего созданного объекта
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последнего созданного объекта</param>
    /// <param name="cachedNodeNumber">Кэш номера узла для корректного обновления объекта</param>
    public static string GetNodeNumberByLastNodeNumber(string lastNodeNumber, ref string cachedNodeNumber)
    {
        var number = "1";

        if (!string.IsNullOrEmpty(lastNodeNumber))
        {
            if (int.TryParse(lastNodeNumber, out var i))
            {
                cachedNodeNumber = (i + 1).ToString();
            }
            else if (Invariables.AxisRusAlphabet.Contains(lastNodeNumber))
            {
                var index = Invariables.AxisRusAlphabet.IndexOf(lastNodeNumber);
                cachedNodeNumber = index == Invariables.AxisRusAlphabet.Count - 1
                    ? Invariables.AxisRusAlphabet[0]
                    : Invariables.AxisRusAlphabet[index + 1];
            }
            else if (Invariables.AxisEngAlphabet.Contains(lastNodeNumber))
            {
                var index = Invariables.AxisEngAlphabet.IndexOf(lastNodeNumber);
                cachedNodeNumber = index == Invariables.AxisEngAlphabet.Count - 1
                    ? Invariables.AxisEngAlphabet[0]
                    : Invariables.AxisEngAlphabet[index + 1];
            }
        }

        if (!string.IsNullOrEmpty(cachedNodeNumber))
        {
            number = cachedNodeNumber;
        }

        return number;
    }
}