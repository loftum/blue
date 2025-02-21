using System.Reflection;
using Blue.Core;
using LiteDB;

namespace Blue.Data;

public class MyMapper : BsonMapper
{
    public MyMapper()
    {
        ResolveMember = DoResolveMember;
    }

    private void DoResolveMember(Type type, MemberInfo memberInfo, MemberMapper memberMapper)
    {
        if (memberInfo is PropertyInfo property && memberMapper.Setter == null)
        {
            var setter = property.GetSetMethod(true);
            if (setter != null)
            {
                memberMapper.Setter = (target, value) => setter.Invoke(target, [value]);
                return;
            }

            var field = type.GetFieldRecursive($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                memberMapper.Setter = (target, value) => SetValue(field, target, value);
            }
        }
    }

    private static void SetValue(FieldInfo field, object target, object value)
    {
        field.SetValue(target, value);
    }

    protected override IEnumerable<MemberInfo> GetTypeMembers(Type type)
    {
        var members = base.GetTypeMembers(type);
        return members;
    }

    public override object ToObject(Type type, BsonDocument doc)
    {
        var maybejson = doc.ToString();
        return base.ToObject(type, doc);
    }

    public override T ToObject<T>(BsonDocument doc)
    {
        return base.ToObject<T>(doc);
    }
}