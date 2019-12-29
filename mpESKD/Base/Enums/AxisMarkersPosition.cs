﻿namespace mpESKD.Base.Enums
{
    using Attributes;

    public enum AxisMarkersPosition
    {
        /// <summary>С обеих сторон</summary>
        [EnumPropertyDisplayValueKey("amt1")]
        Both,

        /// <summary>Сверху</summary>
        [EnumPropertyDisplayValueKey("amt2")]
        Top,

        /// <summary>Снизу</summary>
        [EnumPropertyDisplayValueKey("amt3")]
        Bottom
    }
}