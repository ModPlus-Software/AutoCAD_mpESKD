namespace mpESKD.Functions.mpThickArrow;

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
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class ThickArrowFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ThickArrowGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<ThickArrow>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<ThickArrow>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ThickArrow>();

            var thickArrow = new ThickArrow();
            var blockReference = MainFunction.CreateBlock(thickArrow);

            thickArrow.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);
            InsertThickArrowWithJig(true, thickArrow, blockReference);
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }

    /// <summary>
    /// Команда создания толстой стрелки
    /// </summary>
    [CommandMethod("ModPlus", "mpThickArrow", CommandFlags.Modal)]
    public void CreateViewCommand()
    {
        CreateThickArrow(true);
    }

    private static void CreateThickArrow(bool isSimple)
    {
        SmartEntityUtils.SendStatistic<ThickArrow>();
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ThickArrow>();

            var style = StyleManager.GetCurrentStyle(typeof(ThickArrow));
            var thickArrow = new ThickArrow();
            var blockReference = MainFunction.CreateBlock(thickArrow);

            thickArrow.ApplyStyle(style, true);
            InsertThickArrowWithJig(isSimple, thickArrow, blockReference);
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }

    private static void InsertThickArrowWithJig(bool isSimple, ThickArrow thickArrow, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            thickArrow,
            blockReference,
            new Point3d(20, 0, 0));

        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (isSimple)
                {
                    if (entityJig.JigState == JigState.PromptInsertPoint)
                    {
                        entityJig.JigState = JigState.PromptNextPoint;
                        entityJig.PreviousPoint = thickArrow.InsertionPoint;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                EntityUtils.Erase(blockReference.Id);
                break;
            }
        }
        while (true);

        if (!thickArrow.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(thickArrow.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = thickArrow.GetDataForXData();
                tr.Commit();
            }
        }
    }
}