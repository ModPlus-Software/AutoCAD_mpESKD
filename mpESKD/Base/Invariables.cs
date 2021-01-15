namespace mpESKD.Base
{
    using System.Collections.Generic;

    /// <summary>
    /// Различные постоянные значения
    /// </summary>
    public static class Invariables
    {
        /// <summary>
        /// Идентификатор плагина (уникальное имя)
        /// </summary>
        public static string LangItem = "mpESKD";

        /// <summary>
        /// Допустимые буквенные значения для координационных осей согласно п.5.4, 5.5 ГОСТ 21.101-97
        /// <summary>Данные значения также актуальны для обозначений видов, разрезов и т.п.</summary>
        /// </summary>
        public static List<string> AxisRusAlphabet = new List<string>
        {
            "А", "Б", "В", "Г", "Д", "Е", "Ж", "И", "К", "Л", "М", "Н", "П", "Р", "С", "Т", "У", "Ф", "Ш", "Э", "Ю", "Я",
            "АА", "ББ", "ВВ", "ГГ", "ДД", "ЕЕ", "ЖЖ", "ИИ", "КК", "ЛЛ", "ММ", "НН", "ПП", "РР", "СС", "ТТ", "УУ", "ФФ", "ШШ", "ЭЭ", "ЮЮ", "ЯЯ"
        };

        /// <summary>
        /// Допустимые буквенные значения для координационных осей. Латиница
        /// <summary>Данные значения также актуальны для обозначений видов, разрезов и т.п.</summary>
        /// </summary>
        public static List<string> AxisEngAlphabet = new List<string>
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "AA", "BB", "CC", "DD", "EE", "FF", "GG", "HH", "II", "JJ", "KK", "LL", "MM", "NN", "OO", "PP", "QQ", "RR", "SS", "TT", "UU", "VV", "WW", "XX", "YY", "ZZ"
        };
    }
}
