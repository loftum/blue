using System.Reflection;

namespace Blue.Data;

internal static class ReflectionExtensions
{
    public static FieldInfo? GetFieldRecursive(this Type type, string name, BindingFlags bindingFlags)
    {
        FieldInfo? field = null;
        var current = type;
        
        while (field == null && current != null)
        {
            field = current.GetField(name, bindingFlags);
            if (field == null)
            {
                current = current.BaseType;
            }
        }

        return field;
    }
}