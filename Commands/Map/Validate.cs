using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace ProjectMER.Commands.Map;

public class Validate : ICommand
{
    public string Command => "validate";
    public string[] Aliases => ["v", "val"];
    public string Description => "Валидирует карты и схематики без лагов сервера";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("mpr.validate"))
        {
            response = "Нет прав.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "Использование:\n" +
                       ".validate all\n" +
                       ".validate schems\n" +
                       ".validate map [name]\n" +
                       ".validate status";

            return false;
        }

        string mode = arguments.At(0).ToLower();

        Task.Run(() =>
        {
            try
            {
                switch (mode)
                {
                    case "all":
                        ValidationManager.ValidateEverything();
                        sender.Respond("Валидация завершена, подробности в консоли.");
                        break;

                    case "schems":
                        ValidationManager.ValidateAllSchematics();
                        sender.Respond("Валидация завершена, подробности в консоли.");
                        break;

                    case "map":
                        if (arguments.Count < 2)
                        {
                            sender.Respond("Укажи название, .mp validate map <name>");
                            return;
                        }

                        string mapName = arguments.At(1);
                        ValidationManager.ValidateSpecificMap(mapName, out string mapResponse);
                        sender.Respond(mapResponse);
                        break;

                    case "status":
                        sender.Respond(ValidationManager.GetStatus());
                        return;

                    default:
                        sender.Respond("Неизвестный режим. Доступно: all, map <name>, status");
                        return;
                }

                Log.Info($"[VALIDATE] Завершено: {mode}");
            }
            catch (Exception e)
            {
                Log.Error($"[VALIDATE] Ошибка: {e}");
                sender.Respond("Ошибка при валидации.");
            }
        });

        response = "Используй .validate status для проверки.";
        return true;
    }
}