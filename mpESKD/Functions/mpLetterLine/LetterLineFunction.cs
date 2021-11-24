namespace mpESKD.Functions.mpLetterLine
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
    public class LetterLineFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LinearSmartEntityGripOverrule<LetterLine>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<LetterLine>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<LetterLine>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
            SmartEntityUtils.SendStatistic<LetterLine>();

            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<LetterLine>();

                var letterLine = new LetterLine();
                var blockReference = MainFunction.CreateBlock(letterLine);

                letterLine.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                LinearEntityUtils.InsertWithJig(letterLine, blockReference);
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
        /// Команда создания буквенной линии
        /// </summary>
        [CommandMethod("ModPlus", "mpLetterLine", CommandFlags.Modal)]
        public void CreateGroundLineCommand()
        {
            SmartEntityUtils.SendStatistic<LetterLine>();
            
            CreateLetterLine();
        }

        /// <summary>
        /// Команда создания буквенной линии из полилинии
        /// </summary>
        [CommandMethod("ModPlus", "mpLetterLineFromPolyline", CommandFlags.Modal)]
        public void CreateGroundLineFromPolylineCommand()
        {
            SmartEntityUtils.SendStatistic<LetterLine>();
            
            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<LetterLine>();
            
            LinearEntityUtils.CreateFromPolyline<LetterLine>();
        }

        private void CreateLetterLine()
        {
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<LetterLine>();

                var style = StyleManager.GetCurrentStyle(typeof(LetterLine));
                var letterLine = new LetterLine();

                var blockReference = MainFunction.CreateBlock(letterLine);
                letterLine.ApplyStyle(style, true);

                LinearEntityUtils.InsertWithJig(letterLine, blockReference);
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