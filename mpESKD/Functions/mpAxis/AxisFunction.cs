namespace mpESKD.Functions.mpAxis;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Enums;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class AxisFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new AxisGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<Axis>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<Axis>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<Axis>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<Axis>();

            var axisLastHorizontalValue = string.Empty;
            var axisLastVerticalValue = string.Empty;
            FindLastAxisValues(ref axisLastHorizontalValue, ref axisLastVerticalValue);
            var axis = new Axis(axisLastHorizontalValue, axisLastVerticalValue);

            var blockReference = MainFunction.CreateBlock(axis);

            axis.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            // Отключаю видимость кружков направления
            axis.TopOrientMarkerVisible = false;
            axis.BottomOrientMarkerVisible = false;

            InsertAxisWithJig(axis, blockReference);
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }

    /// <summary>
    /// Команда создания прямой оси
    /// </summary>
    [CommandMethod("ModPlus", "mpAxis", CommandFlags.Modal)]
    public void CreateAxisCommand()
    {
        CreateAxis();
    }

    private static void CreateAxis()
    {
        SmartEntityUtils.SendStatistic<Axis>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<Axis>();

            var style = StyleManager.GetCurrentStyle(typeof(Axis));

            var axisLastHorizontalValue = string.Empty;
            var axisLastVerticalValue = string.Empty;
            FindLastAxisValues(ref axisLastHorizontalValue, ref axisLastVerticalValue);
            var axis = new Axis(axisLastHorizontalValue, axisLastVerticalValue);

            var blockReference = MainFunction.CreateBlock(axis);
            axis.ApplyStyle(style, true);

            InsertAxisWithJig(axis, blockReference);
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }

    private static void InsertAxisWithJig(Axis axis, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            axis,
            blockReference,
            new Point3d(0, -1, 0));
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (entityJig.JigState == JigState.PromptInsertPoint)
                {
                    entityJig.JigState = JigState.PromptNextPoint;
                }
                else
                {
                    break;
                }
            }
            else
            {
                using (AcadUtils.Document.LockDocument())
                {
                    using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                    {
                        var obj = (BlockReference)tr.GetObject(blockReference.Id, OpenMode.ForWrite, true, true);
                        obj.Erase(true);
                        tr.Commit();
                    }
                }

                break;
            }
        }
        while (true);

        if (!axis.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(axis.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = axis.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Поиск последних цифровых и буквенных значений осей на текущем виде
    /// </summary>
    /// <param name="axisLastHorizontalValue">Последнее значение для горизонтальной оси</param>
    /// <param name="axisLastVerticalValue">Последнее значение для вертикальной оси</param>
    private static void FindLastAxisValues(ref string axisLastHorizontalValue, ref string axisLastVerticalValue)
    {
        if (!MainSettings.Instance.AxisSaveLastTextAndContinueNew)
            return;

        var allIntegerValues = new List<int>();
        var allLetterValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<Axis>(typeof(Axis)).ForEach(a =>
        {
            var s = a.FirstText;
            if (int.TryParse(s, out var i))
            {
                allIntegerValues.Add(i);
            }
            else
            {
                allLetterValues.Add(s);
            }
        });

        if (allIntegerValues.Any())
        {
            allIntegerValues.Sort();
            axisLastVerticalValue = allIntegerValues.Last().ToString();
        }

        if (allLetterValues.Any())
        {
            allLetterValues.Sort();
            axisLastHorizontalValue = allLetterValues.Last();
        }
    }
}