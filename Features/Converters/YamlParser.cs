using Exiled.Loader.Features.Configs;
using Exiled.Loader.Features.Configs.CustomConverters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using UnderscoredNamingConvention = Exiled.Loader.Features.Configs.UnderscoredNamingConvention;

namespace ProjectMER.Features.Converters;

public static class YamlParser
{
    private static IDeserializer? _deserializer;
    private static ISerializer? _serializer;

    public static IDeserializer Deserializer => _deserializer ??= new DeserializerBuilder()
        .WithTypeConverter(new VectorsConverter())
        .WithTypeConverter(new ColorConverter())
        .WithTypeConverter(new AttachmentIdentifiersConverter())
        .WithTypeConverter(new RoomTypeConverter())
        .WithTypeConverter(new LightShadowsConverter())
        .WithTypeConverter(new PedestalConverter())
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreFields()
        .IgnoreUnmatchedProperties()
        .Build();

    public static ISerializer Serializer => _serializer ??= new SerializerBuilder()
        .WithTypeConverter(new VectorsConverter())
        .WithTypeConverter(new ColorConverter())
        .WithTypeConverter(new AttachmentIdentifiersConverter())
        .WithTypeConverter(new RoomTypeConverter())
        .WithTypeConverter(new LightShadowsConverter())
        .WithTypeConverter(new PedestalConverter())
        .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
        .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreFields()
        .Build();
}