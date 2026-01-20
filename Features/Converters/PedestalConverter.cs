using Exiled.API.Enums;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ProjectMER.Features.Converters;

public class PedestalConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(LockerType);

    public object? ReadYaml(IParser parser, Type type)
    {
        Scalar scalar = parser.Consume<Scalar>();
        string value = scalar.Value;

        if (string.Equals(value, "Pedestal", StringComparison.OrdinalIgnoreCase))
            return LockerType.ScpPedestal;

        return Enum.TryParse(value, true, out LockerType result) ? result : LockerType.Misc;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        LockerType lockerType = (LockerType)(value ?? LockerType.Misc);
        emitter.Emit(new Scalar(lockerType.ToString()));
    }
}