using Exiled.API.Features.Pickups;

namespace ProjectMER.Features.Extensions;

public static class ButtonExtensions
{
    /// <summary>
    /// Проверяет на пикап на то, является ли он кастомной кнопкой, если да, возвращает ключ
    /// </summary>
    /// <param name="pickup">Пикап, который проверяем</param>
    /// <param name="key">Ключ кастомной кнопки</param>
    /// <returns>true - если да, false - если нет</returns>
    public static bool IsCustomButton(this Pickup pickup, out string key)
    {
        if (EventHandlers.EventHandlers.ButtonPickups.TryGetValue(pickup.Serial, out key))
            return true;

        key = string.Empty;
        return false;
    }
}