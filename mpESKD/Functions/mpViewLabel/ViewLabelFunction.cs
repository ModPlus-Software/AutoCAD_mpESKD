namespace mpESKD.Functions.mpViewLabel
{
    using System;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
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
    public class ViewLabelFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ViewLabelGripPointOverrule(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<ViewLabel>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<ViewLabel>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(Section.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(ViewLabel.GetDescriptor());

                var viewLabelLastLetterValue = string.Empty;
                var viewLabelLastIntegerValue = string.Empty;
                FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
                var viewLabel = new ViewLabel(viewLabelLastIntegerValue, viewLabelLastLetterValue);

                var blockReference = MainFunction.CreateBlock(viewLabel);

                viewLabel.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);
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
        /// Команда создания обозначения разреза
        /// </summary>
        [CommandMethod("ModPlus", "mpViewLabel", CommandFlags.Modal)]
        public void CreateViewLabelCommand()
        {
            CreateViewLabel(true);
        }

    
        private static void CreateViewLabel(bool isSimple)
        {
#if !DEBUG
            Statistic.SendCommandStarting(Section.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(ViewLabel.GetDescriptor());

                var style = StyleManager.GetCurrentStyle(typeof(ViewLabel));
                var viewLabelLastLetterValue = string.Empty;
                var viewLabelLastIntegerValue = string.Empty;
                FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
                var viewLabel = new ViewLabel(viewLabelLastIntegerValue, viewLabelLastLetterValue);

                var blockReference = MainFunction.CreateBlock(viewLabel);
                viewLabel.ApplyStyle(style, true);
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
        /// Поиск последних цифровых и буквенных значений разрезов на текущем виде
        /// </summary>
        private static void FindLastViewLabelValues(ref string sectionLastLetterValue, ref string sectionLastIntegerValue)
        {
            if (MainSettings.Instance.SectionSaveLastTextAndContinueNew)
            {
                var sections = AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<ViewLabel>(typeof(Section));
                if (sections.Any())
                {
                    sections.Sort((s1, s2) => string.Compare(s1.BlockRecord.Name, s2.BlockRecord.Name, StringComparison.Ordinal));
                    var v = sections.Last().Designation;
                    if (int.TryParse(v, out var i))
                    {
                        sectionLastIntegerValue = i.ToString();
                    }
                    else
                    {
                        sectionLastLetterValue = v;
                    }
                }
            }
        }
    }
}
