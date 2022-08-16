namespace mpESKD.Base;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Объект привязки объекта сериализации
/// </summary>
public class Binder : SerializationBinder
{
    /// <inheritdoc/>
    public override Type BindToType(string assemblyName, string typeName)
    {
        return Type.GetType($"{typeName}, {assemblyName}");
    }
}