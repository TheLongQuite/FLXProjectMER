using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ProjectMER.Features.Converters;

public class LightShadowsConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(LightShadows);

    public object? ReadYaml(IParser parser, Type type)
    {
        Scalar scalar = parser.Consume<Scalar>();
        string value = scalar.Value;
        
        if (bool.TryParse(value, out bool boolValue))
            return boolValue ? LightShadows.Soft : LightShadows.None;
        
        if (Enum.TryParse(value, true, out LightShadows result))
            return result;

        return LightShadows.None;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        LightShadows shadows = (LightShadows)(value ?? LightShadows.None);
        emitter.Emit(new Scalar(shadows.ToString()));
    }
}