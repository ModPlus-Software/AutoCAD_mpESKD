namespace mpESKD.Functions.mpGroundLine
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Abstractions;
    using Base.Overrules;
    using Base.Styles;
    using Base.Utils;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    public class GroundLineFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LinearSmartEntityGripOverrule<GroundLine>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<GroundLine>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<GroundLine>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                GroundLine.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif

            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(GroundLine.GetDescriptor());

                var groundLine = new GroundLine();
                var blockReference = MainFunction.CreateBlock(groundLine);

                groundLine.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                LinearEntityUtils.InsertWithJig(groundLine, blockReference);
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
        /// Команда создания линии грунта
        /// </summary>
        [CommandMethod("ModPlus", "mpGroundLine", CommandFlags.Modal)]
        public void CreateGroundLineCommand()
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                GroundLine.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            CreateGroundLine();
        }

        /// <summary>
        /// Команда создания линия грунта из полилинии
        /// </summary>
        [CommandMethod("ModPlus", "mpGroundLineFromPolyline", CommandFlags.Modal)]
        public void CreateGroundLineFromPolylineCommand()
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                "mpGroundLineFromPolyline", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            
            ExtendedDataUtils.AddRegAppTableRecord(GroundLine.GetDescriptor());
            LinearEntityUtils.CreateFromPolyline<GroundLine>();
        }

        private void CreateGroundLine()
        {
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(GroundLine.GetDescriptor());

                var style = StyleManager.GetCurrentStyle(typeof(GroundLine));
                var groundLine = new GroundLine();

                var blockReference = MainFunction.CreateBlock(groundLine);
                groundLine.ApplyStyle(style, true);

                LinearEntityUtils.InsertWithJig(groundLine, blockReference);
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
    }
}