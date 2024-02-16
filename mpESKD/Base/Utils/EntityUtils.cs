namespace mpESKD.Base.Utils;

using System.Linq;
using Abstractions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Enums;
using System;
using View;

/// <summary>
/// Утилиты для объектов
/// </summary>
public static class EntityUtils
{
    /// <summary>
    /// Удалить объект
    /// </summary>
    /// <param name="objectId">Идентификатор объекта</param>
    public static void Erase(ObjectId objectId)
    {
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
            {
                var obj = tr.GetObject(objectId, OpenMode.ForWrite, true, true);
                obj.Erase(true);
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Установка свойств для однострочного текста
    /// </summary>
    /// <param name="dbText">Однострочный текст</param>
    /// <param name="textStyleName">Имя текстового стиля</param>
    /// <param name="height">Высота текста (с учетом масштаба блока)</param>
    public static void SetProperties(this DBText dbText, string textStyleName, double height)
    {
        dbText.Height = height;
        dbText.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
        dbText.Linetype = "ByBlock";
        dbText.LineWeight = LineWeight.ByBlock;
        var textStyle = AcadUtils.GetTextStyleByName(textStyleName);
        if (textStyle != null)
        {
            AcadUtils.WriteMessageInDebug($"\n SetProperties for dbText: {dbText}; TextStyle: {textStyleName} \n");
            dbText.TextStyleId = textStyle.Id;

            // https://adn-cis.org/forum/index.php?topic=8236.0
            dbText.Oblique = textStyle.ObliquingAngle;
            dbText.WidthFactor = textStyle.XScale;
        }
    }

    /// <summary>
    /// Установка свойств для многострочного текста
    /// </summary>
    /// <param name="mText">Многострочный текст</param>
    /// <param name="textStyleName">Имя текстового стиля</param>
    /// <param name="height">Высота текста (с учетом масштаба блока)</param>
    public static void SetProperties(this MText mText, string textStyleName, double height)
    {
        mText.Height = height;
        mText.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
        mText.Linetype = "ByBlock";
        mText.LineWeight = LineWeight.ByBlock;
        var textStyle = AcadUtils.GetTextStyleByName(textStyleName);
        if (textStyle != null)
        {
            mText.TextStyleId = textStyle.Id;
        }
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
    /// Возвращает вектор сдвига текста при выравнивании
    /// </summary>
    /// <param name="valueHorizontalAlignment">Значение выравнивания по горизонтали</param>
    /// <param name="isRight">Направление полки</param>
    /// <param name="textHalfMovementHorV">Половина сдвига по горизонтали</param>
    /// <param name="scaleFactorX">Значение зеркальности</param>
    /// <returns></returns>
    public static Vector3d GetMovementPositionVector(
        TextHorizontalAlignment valueHorizontalAlignment, 
        bool isRight, 
        Vector3d textHalfMovementHorV, 
        double scaleFactorX)
    {
        if ((isRight && valueHorizontalAlignment == TextHorizontalAlignment.Right) ||
            (!isRight && valueHorizontalAlignment == TextHorizontalAlignment.Left))
        {
            if (scaleFactorX > 0)
                return textHalfMovementHorV;
            return -textHalfMovementHorV;
        }

        if ((!isRight && valueHorizontalAlignment == TextHorizontalAlignment.Right) ||
            (isRight && valueHorizontalAlignment == TextHorizontalAlignment.Left))
        {
            if (scaleFactorX < 0)
                return textHalfMovementHorV;
            return -textHalfMovementHorV;
        }

        return default;
    }

    /// <summary>
    /// Редактирование свойств для интеллектуального объекта в специальном окне. Применяется для интеллектуальных
    /// объектов, содержащих текстовые значения
    /// </summary>
    /// <param name="blockReference">Блок, представляющий интеллектуальный объект</param>
    public static void DoubleClickEdit(BlockReference blockReference)
    {
        CommandsWatcher.UseBedit = false;

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
    /// <param name="center">Средняя точка прямоугольной маскировки</param>
    public static Wipeout GetBackgroundMask(this DBText dbText, double offset, Point3d center)
    {
        if (dbText == null)
            return null;

        var framePoints = GetOverallDimensionsPoints<DBText>(dbText, offset, center);

        return GetBackgroundMask(framePoints);
    } // Использует NodalLeader

    /// <summary>
    /// <inheritdoc cref="GetBackgroundMask"/>
    /// </summary>
    /// <param name="mText"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static Wipeout GetBackgroundMask(this MText mText, double offset)
    {
        if (mText == null)
            return null;

        var center = GeometryUtils.GetMiddlePoint3d(
            mText.GeometricExtents.MinPoint, 
            mText.GeometricExtents.MaxPoint);

        return GetBackgroundMask(mText, offset, center);
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру текста с указанным отступом
    /// </summary>
    /// <param name="mText">Экземпляр <see cref="DBText"/></param>
    /// <param name="offset">Отступ</param>
    /// <param name="center"></param>
    public static Wipeout GetBackgroundMask(this MText mText, double offset, Point3d center)
    {
        if (mText == null)
            return null;

        var framePoints = GetOverallDimensionsPoints<MText>(mText, offset, center);
        return GetBackgroundMask(framePoints);
    }

    /// <summary>
    /// Возвращает маскировку, созданную по контуру полилинии
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

        var wipeout = GetBackgroundMask(vertexCollection);
        return wipeout;
    }

    private static Wipeout GetBackgroundMask(Point2dCollection points)
    {
        try
        {
            if (points == null)
                return null;

            var pointsWipeout = points.ToArray().ToList();
            pointsWipeout.Add(pointsWipeout.First());

            var points2dWipeout = new Point2dCollection();
            foreach (var point in pointsWipeout)
            {
                points2dWipeout.Add(point);
            }

            var wipeout = new Wipeout();
            wipeout.SetFrom(points2dWipeout, Vector3d.ZAxis);

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
    /// Возвращает точки габаритного контура с заданными отступом
    /// </summary>
    /// <param name="textObject">Объект текста</param>
    /// <param name="offset">Отступ</param>
    /// <param name="center"></param>
    /// <returns>коллекция точек контура: слева внизу, справа внизу, справа вверху, слева вверху</returns>
    private static Point2dCollection GetOverallDimensionsPoints<T>(this T textObject, double offset, Point3d center) 
        where T : Entity 
    {
        if (textObject == null || typeof(T) != typeof(DBText) & typeof(T) != typeof(MText))
        {
            return null;
        }

        var textSize = (0d, 0d);
        if (textObject is DBText dbText)
        {
            textSize = TextStyleArx.GetTrueTextSize(dbText.TextString, dbText.TextStyleName);
        }
        else if (textObject is MText mText)
        {
            textSize = TextStyleArx.GetTrueTextSize(mText.Text, mText.TextStyleName);
        }

        if (textSize.Item1 != 0 && textSize.Item2 != 0)
        {
            var halfWidth = textSize.Item1 / 2;
            var halfHeight = textSize.Item2 / 2;

            var bottomLeftPoint = new Point2d(center.X - halfWidth, center.Y - halfHeight);
            var topLeftPoint = new Point2d(center.X - halfWidth, center.Y + halfHeight);
            var topRightPoint = new Point2d(center.X + halfWidth, center.Y + halfHeight);
            var bottomRightPoint = new Point2d(center.X + halfWidth, center.Y - halfHeight);

            return new Point2dCollection
            {
                bottomLeftPoint, topLeftPoint, topRightPoint, bottomRightPoint
            };
        }

        return null;
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