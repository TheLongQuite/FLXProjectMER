using Exiled.API.Enums;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ProjectMER.Features.Converters;

public class RoomTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(RoomType);

    public object? ReadYaml(IParser parser, Type type)
    {
        Scalar scalar = parser.Consume<Scalar>();
        string value = scalar.Value;
        
        return Enum.TryParse(value, true, out RoomType result) ? result : RoomType.Unknown;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        RoomType roomType = (RoomType)(value ?? RoomType.Unknown);
        emitter.Emit(new Scalar(roomType.ToString()));
    }
}