using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using ProjectMER.Features;

namespace ProjectMER.Commands.Map;

public class Load : ICommand
{
    public string Command => "load";

    public string[] Aliases => ["l"];

    public string Description => "Loads a map";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"mpr.{Command}"))
        {
            response = $"You don't have permission to execute this command. Required permission: mpr.{Command}";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "You need to provide a map name!";
            return false;
        }

        string mapName = arguments.At(0);

        try
        {
            MapUtils.LoadMap(mapName);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("Sequence contains no matching element"))
        {
            response = $"Ошибка загрузки карты: не найден необходимый элемент.\n" +
                       $"Подробности в консоли сервера.\n" +
                       $"Возможные причины:\n" +
                       $"- Карта использует комнату, которой нет на текущей карте\n" +
                       $"- Раунд не полностью инициализирован";
            return false;
        }
        catch (FileNotFoundException)
        {
            response = $"Карта '{mapName}' не найдена!";
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"[Load] Ошибка при загрузке карты '{mapName}': {e}");
            response = $"Ошибка при загрузке карты: {e.Message}";
            return false;
        }

        response = $"{mapName} map has been loaded!";
        return true;
    }
}