namespace mpESKD.Functions.mpWeldJoint
{
    using Base.Attributes;

    /// <summary>
    /// Тип обозначения шва сварного соединения
    /// </summary>
    public enum WeldJointType
    {
        /// <summary>
        /// Стыковой заводской сплошной с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt1")]
        ButtFactorySolidVisible,

        /// <summary>
        /// Стыковой заводской сплошной с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt2")]
        ButtFactorySolidInvisible,
        
        /// <summary>
        /// Стыковой монтажный сплошной с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt3")]
        ButtMountingSolidVisible,
        
        /// <summary>
        /// Стыковой монтажный сплошной с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt4")]
        ButtMountingSolidInvisible,
        
        /// <summary>
        /// Стыковой заводской прерывистый с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt5")]
        ButtFactoryIntermittentVisible,
        
        /// <summary>
        /// Стыковой заводской прерывистый с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt6")]
        ButtFactoryIntermittentInvisible,
        
        /// <summary>
        /// Стыковой монтажный прерывистый с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt7")]
        ButtMountingIntermittentVisible,
        
        /// <summary>
        /// Стыковой монтажный прерывистый с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt8")]
        ButtMountingIntermittentInvisible,
        
        /// <summary>
        /// Угловой заводской сплошной с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt9")]
        CornerFactorySolidVisible,

        /// <summary>
        /// Угловой заводской сплошной с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt10")]
        CornerFactorySolidInvisible,
        
        /// <summary>
        /// Угловой монтажный сплошной с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt11")]
        CornerMountingSolidVisible,
        
        /// <summary>
        /// Угловой монтажный сплошной с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt12")]
        CornerMountingSolidInvisible,
        
        /// <summary>
        /// Угловой заводской прерывистый с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt13")]
        CornerFactoryIntermittentVisible,
        
        /// <summary>
        /// Угловой заводской прерывистый с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt14")]
        CornerFactoryIntermittentInvisible,
        
        /// <summary>
        /// Угловой монтажный прерывистый с видимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt15")]
        CornerMountingIntermittentVisible,
        
        /// <summary>
        /// Угловой монтажный прерывистый с невидимой стороны
        /// </summary>
        [EnumPropertyDisplayValueKey("wjt16")]
        CornerMountingIntermittentInvisible,
    }
}
