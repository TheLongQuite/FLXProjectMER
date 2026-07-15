using System.Reflection;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MapGeneration;
using Mirror;

namespace ProjectMER.Patches;

public static class MirrorPatches
{
    public static void RegisterRoomIdentifierWriter()
    {
        try
        {
            _ = MirrorExtensions.WriterExtensions;

            FieldInfo? writerExtensionsValueField = typeof(MirrorExtensions).GetField("WriterExtensionsValue", BindingFlags.NonPublic | BindingFlags.Static);
            if (writerExtensionsValueField == null)
            {
                Log.Error("Could not find WriterExtensionsValue field in MirrorExtensions.");
                return;
            }

            Dictionary<Type, MethodInfo>? dict = (Dictionary<Type, MethodInfo>)writerExtensionsValueField.GetValue(null);
            
            if (dict.ContainsKey(typeof(RoomIdentifier)))
                return;

            MethodInfo writeMethod = null;
            foreach (Assembly? assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        try
                        {
                            MethodInfo? method = type.GetMethod("WriteRoomIdentifier", BindingFlags.Public | BindingFlags.Static);
                            if (method != null)
                            {
                                ParameterInfo[] parameters = method.GetParameters();
                                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(NetworkWriter) && parameters[1].ParameterType == typeof(RoomIdentifier))
                                {
                                    writeMethod = method;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                if (writeMethod != null) break;
            }

            if (writeMethod != null)
            {
                dict[typeof(RoomIdentifier)] = writeMethod;
                Log.Debug($"Successfully registered WriteRoomIdentifier from {writeMethod.DeclaringType.Name} in MirrorExtensions!");
            }
            else
            {
                Log.Error("Could not find WriteRoomIdentifier method anywhere!");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register RoomIdentifier writer: {ex}");
        }
    }
}