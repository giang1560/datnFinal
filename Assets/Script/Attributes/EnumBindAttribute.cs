using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EnumBindAttribute : PropertyAttribute
{
    public Type EnumType { get; private set; }

    public EnumBindAttribute(Type enumType)
    {
        EnumType = enumType;
    }
}