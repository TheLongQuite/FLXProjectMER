using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using NorthwoodLib.Pools;
using ProjectMER.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.ToolGun;
using UnityEngine;

namespace ProjectMER.Commands.Modifying;

public class Modify : ICommand
{
    public string Command => "modify";
    public string[] Aliases { get; } = ["mod"];
    public string Description => "Allows modifying properties of the selected object.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"mpr.{Command}"))
        {
            response = $"У вас нет прав для выполнения этой команды. Требуется право: mpr.{Command}";
            return false;
        }

        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = "Эту команду нельзя выполнить из консоли сервера.";
            return false;
        }

        if (!ToolGunHandler.TryGetSelectedMapObject(player, out MapEditorObject mapEditorObject))
        {
            response = "Вы не выбрали ни одного объекта! Используйте команду select или ToolGun в режиме SELECT.";
            return false;
        }

        object instance;
        try
        {
            FieldInfo? baseField = mapEditorObject.GetType().GetField("Base");
            if (baseField == null)
            {
                Log.Error($"[Modify] Поле 'Base' не найдено в типе {mapEditorObject.GetType().Name}");
                response = "Внутренняя ошибка: поле Base не найдено.";
                return false;
            }

            instance = baseField.GetValue(mapEditorObject);
            if (instance == null)
            {
                Log.Error($"[Modify] Значение поля 'Base' равно null для объекта {mapEditorObject.Id}");
                response = "Внутренняя ошибка: Base равен null.";
                return false;
            }

            Log.Debug($"[Modify] Получен instance типа {instance.GetType().Name} для объекта {mapEditorObject.Id}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify] Не удалось получить Base у объекта {mapEditorObject.GetType().Name}: {ex}");
            response = "Внутренняя ошибка: не удалось получить данные объекта.";
            return false;
        }

        List<PropertyInfo> properties = instance.GetType().GetModifiableProperties().ToList();

        if (arguments.Count == 0)
            return ShowProperties(mapEditorObject, instance, properties, out response);

        string propertyName = arguments.At(0).ToUpperInvariant();

        if (propertyName.Contains("MAP"))
            return HandleMap(arguments, mapEditorObject, out response);

        if (propertyName == "ID")
            return HandleId(arguments, mapEditorObject, out response);

        PropertyInfo? foundProperty = properties.FirstOrDefault(x => x.Name.ToUpperInvariant().Contains(propertyName));
        if (foundProperty == null)
        {
            response = $"Свойство \"{arguments.At(0)}\" не найдено!\n" +
                       $"Доступные свойства: {string.Join(", ", properties.Select(p => p.Name))}";

            return false;
        }

        bool isReadOnlyOperation = false;

        if (IsComplexCollection(foundProperty.PropertyType))
        {
            if (arguments.Count >= 2 && arguments.At(1).ToLower() == "list")
                isReadOnlyOperation = true;

            if (!HandleComplexCollection(arguments, foundProperty, instance, out response))
                return false;
        }
        else if (typeof(ICollection).IsAssignableFrom(foundProperty.PropertyType))
        {
            if (!HandleSimpleCollection(arguments, foundProperty, instance, out response))
                return false;
        }
        else if (foundProperty.PropertyType == typeof(Vector3) || foundProperty.PropertyType == typeof(Vector2))
        {
            if (!HandleVector(arguments, foundProperty, instance, out response))
                return false;
        }
        else if (IsComplexType(foundProperty.PropertyType))
        {
            if (!HandleComplexType(arguments, foundProperty, instance, out response))
                return false;
        }
        else if (foundProperty.PropertyType == typeof(string))
        {
            if (!HandleString(arguments, foundProperty, instance, out response))
                return false;
        }
        else
        {
            if (!HandleSimpleType(arguments, foundProperty, instance, out response))
                return false;
        }

        if (isReadOnlyOperation)
            return true;

        try
        {
            mapEditorObject.UpdateObjectAndCopies();
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify] Ошибка при обновлении объекта {mapEditorObject.Id}: {ex}");
            response = "Значение изменено, но произошла ошибка при обновлении объекта.";
            return false;
        }

        if (string.IsNullOrEmpty(response))
            response = "Объект успешно изменён!";

        return true;
    }

    private static bool ShowProperties(MapEditorObject mapEditorObject, object instance,
        List<PropertyInfo> properties, out string response)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine();
        sb.AppendLine("Свойства объекта:");
        sb.AppendLine();
        sb.AppendLine($"MapName: {MapUtils.GetColoredMapName(mapEditorObject.MapName)}");
        sb.AppendLine($"ID: {MapUtils.GetColoredString(mapEditorObject.Id)}");

        foreach (string property in properties.GetColoredProperties(instance))
            sb.AppendLine(property);

        response = StringBuilderPool.Shared.ToStringReturn(sb);
        return true;
    }

    private static bool HandleVector(ArraySegment<string> arguments, PropertyInfo property, object instance, out string response)
    {
        bool isVector3 = property.PropertyType == typeof(Vector3);
        object currentRaw = property.GetValue(instance);
        
        float x = isVector3 ? ((Vector3)currentRaw).x : ((Vector2)currentRaw).x;
        float y = isVector3 ? ((Vector3)currentRaw).y : ((Vector2)currentRaw).y;
        float z = isVector3 ? ((Vector3)currentRaw).z : 0;

        if (arguments.Count < 2)
        {
            response = $"Использование для {property.Name} ({property.PropertyType.Name}):\n" +
                       $"  mod {property.Name} <x> <y> [z]\n" +
                       $"  mod {property.Name} <x,y,z>\n" +
                       $"  mod {property.Name} x=10 y=20\n" +
                       $"Текущее значение: {currentRaw}";
            return false;
        }

        // Собираем все аргументы после имени свойства в одну строку и массив
        string[] vectorArgs = arguments.Skip(1).ToArray();
        string fullString = string.Join(" ", vectorArgs);

        try
        {
            // Случай 1: Формат x=10 y=20
            if (fullString.Contains('='))
            {
                foreach (string arg in vectorArgs)
                {
                    string[] parts = arg.Split('=');
                    if (parts.Length != 2) continue;
                    
                    string axis = parts[0].ToLower();
                    float val = float.Parse(parts[1]);

                    if (axis == "x") x = val;
                    else if (axis == "y") y = val;
                    else if (axis == "z" && isVector3) z = val;
                }
            }
            // Случай 2: Формат 10,20,30
            else if (fullString.Contains(','))
            {
                string[] parts = fullString.Split(',');
                if (parts.Length >= 1) x = float.Parse(parts[0]);
                if (parts.Length >= 2) y = float.Parse(parts[1]);
                if (parts.Length >= 3 && isVector3) z = float.Parse(parts[2]);
            }
            // Случай 3: Формат 10 20 30 (пробелы)
            else
            {
                if (vectorArgs.Length >= 1) x = float.Parse(vectorArgs[0]);
                if (vectorArgs.Length >= 2) y = float.Parse(vectorArgs[1]);
                if (vectorArgs.Length >= 3 && isVector3) z = float.Parse(vectorArgs[2]);
            }

            object result = isVector3 ? new Vector3(x, y, z) : new Vector2(x, y);
            property.SetValue(instance, result);
            response = $"{property.Name} установлено в {result}";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Ошибка при парсинге вектора: {ex.Message}";
            return false;
        }
    }

    private static bool HandleMap(ArraySegment<string> arguments, MapEditorObject mapEditorObject, out string response)
    {
        if (arguments.Count < 2)
        {
            response = "Использование: mod map <имя_карты>\n" +
                       $"Текущая карта: {mapEditorObject.MapName}";

            return false;
        }

        string newMapName = arguments.At(1);

        if (mapEditorObject.MapName == newMapName)
        {
            response = "Объект уже принадлежит этой карте!";
            return false;
        }

        if (newMapName == MapUtils.UntitledMapName)
        {
            response = $"Имя карты \"{MapUtils.UntitledMapName}\" зарезервировано для внутреннего использования!";
            return false;
        }

        try
        {
            MapSchematic oldMap = mapEditorObject.Map;

            if (!MapUtils.LoadedMaps.TryGetValue(newMapName, out MapSchematic newMap))
            {
                if (!MapUtils.TryGetMapData(newMapName, out newMap))
                {
                    newMap = new(newMapName);
                    MapUtils.LoadedMaps.Add(newMapName, newMap);
                }
            }

            oldMap.TryRemoveElement(mapEditorObject.Id);
            newMap.TryAddElement(mapEditorObject.Base);
            oldMap.Reload();
            newMap.Reload();
        }
        catch (Exception ex)
        {
            Log.Error(
                $"[Modify.HandleMap] Ошибка при переносе объекта {mapEditorObject.Id} на карту {newMapName}: {ex}");

            response = "Произошла ошибка при переносе объекта на другую карту.";
            return false;
        }

        response = "Карта объекта успешно изменена!";
        return true;
    }

    private static bool HandleId(ArraySegment<string> arguments, MapEditorObject mapEditorObject, out string response)
    {
        if (arguments.Count < 2)
        {
            response = "Использование: mod id <новый_id>\n" +
                       $"Текущий ID: {mapEditorObject.Id}";

            return false;
        }

        string newId = arguments.At(1);

        if (mapEditorObject.Map.SpawnedObjects.Any(x => x.Id == newId))
        {
            response = $"ID \"{newId}\" уже используется другим объектом на этой карте!";
            return false;
        }

        mapEditorObject.Base.ObjectId = newId;

        try
        {
            mapEditorObject.Map.Reload();
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleId] Ошибка при перезагрузке карты после смены ID: {ex}");
            response = "ID изменён, но произошла ошибка при перезагрузке карты.";
            return false;
        }

        response = "ID объекта успешно изменён!";
        return true;
    }

    private static bool HandleComplexCollection(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        object? listInstance = property.GetValue(instance);

        if (listInstance == null)
        {
            response = $"Ошибка: свойство {property.Name} не инициализировано (null).";
            return false;
        }

        Type elementType = property.PropertyType.GetGenericArguments()[0];
        List<PropertyInfo> elementProperties = elementType.GetModifiableProperties().ToList();

        if (arguments.Count < 2)
        {
            response = GetComplexCollectionHelp(property.Name, elementType, elementProperties, listInstance);
            return false;
        }

        string action = arguments.At(1).ToLower();

        switch (action)
        {
            case "a":
            case "add":
                return HandleComplexCollectionAdd(arguments, property, listInstance, elementType,
                    elementProperties, out response);

            case "rm":
            case "remove":
                return HandleComplexCollectionRemove(arguments, property, listInstance, elementType, out response);

            case "set":
                return HandleComplexCollectionSet(arguments, listInstance, elementType,
                    elementProperties, out response);

            case "list":
                response = GetComplexCollectionList(property.Name, listInstance, elementType, elementProperties);
                return true;

            default:
                response = $"Неизвестное действие \"{action}\"!\n" +
                           $"Доступные действия: add, remove, set, list";

                return false;
        }
    }

    private static bool HandleComplexCollectionAdd(ArraySegment<string> arguments, PropertyInfo property,
        object listInstance, Type elementType, List<PropertyInfo> elementProperties, out string response)
    {
        object newElement;
        try
        {
            newElement = Activator.CreateInstance(elementType);
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleComplexCollectionAdd] Не удалось создать экземпляр {elementType.Name}: {ex}");
            response = $"Не удалось создать новый элемент типа {elementType.Name}.";
            return false;
        }

        for (int i = 2; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            string[] parts = arg.Split('=');

            if (parts.Length != 2)
            {
                response = $"Неверный формат аргумента \"{arg}\"! Используйте свойство=значение";
                return false;
            }

            PropertyInfo? elementProp =
                elementProperties.FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));

            if (elementProp == null)
            {
                response = $"Свойство \"{parts[0]}\" не найдено в типе {elementType.Name}!";
                return false;
            }

            if (!TrySetPropertyValue(elementProp, newElement, parts[1], out string error))
            {
                response = error;
                return false;
            }
        }

        try
        {
            MethodInfo? addMethod = property.PropertyType.GetMethod("Add");
            addMethod?.Invoke(listInstance, new[] { newElement });

            ICollection collection = (ICollection)listInstance;
            response = $"Элемент успешно добавлен! Всего элементов: {collection.Count}";
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleComplexCollectionAdd] Ошибка при добавлении в коллекцию: {ex}");
            response = "Ошибка при добавлении элемента в коллекцию.";
            return false;
        }
    }

    private static bool HandleComplexCollectionRemove(ArraySegment<string> arguments, PropertyInfo property,
        object listInstance, Type elementType, out string response)
    {
        if (arguments.Count < 3)
        {
            response = $"Использование: mod {property.Name} remove <индекс>";
            return false;
        }

        if (!int.TryParse(arguments.At(2), out int index))
        {
            response = $"\"{arguments.At(2)}\" не является числом!";
            return false;
        }

        IList list = (IList)listInstance;

        if (index < 0 || index >= list.Count)
        {
            response = $"Индекс {index} вне диапазона (0-{list.Count - 1})!";
            return false;
        }

        list.RemoveAt(index);
        response = $"Элемент с индексом {index} удалён! Осталось: {list.Count}";
        return true;
    }

    private static bool HandleComplexCollectionSet(ArraySegment<string> arguments, object listInstance,
        Type elementType, List<PropertyInfo> elementProperties, out string response)
    {
        if (arguments.Count < 4)
        {
            response = "Использование: mod <свойство> set <индекс> <свойство_элемента>=<значение>";
            return false;
        }

        if (!int.TryParse(arguments.At(2), out int index))
        {
            response = $"\"{arguments.At(2)}\" не является числом!";
            return false;
        }

        IList list = (IList)listInstance;
        if (index < 0 || index >= list.Count)
        {
            response = $"Индекс {index} вне диапазона!";
            return false;
        }

        object? element = list[index];
        if (element == null)
        {
            response = "Элемент равен null!";
            return false;
        }

        for (int i = 3; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            string[] parts = arg.Split('=');

            if (parts.Length != 2) continue;

            PropertyInfo? elementProp =
                elementProperties.FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));

            if (elementProp != null)
                TrySetPropertyValue(elementProp, element, parts[1], out _);
        }

        response = $"Элемент с индексом {index} успешно изменён!";
        return true;
    }

    private static bool HandleSimpleCollection(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        object? listInstance = property.GetValue(instance);

        if (listInstance == null)
        {
            response = $"Ошибка: свойство {property.Name} не инициализировано (null).";
            return false;
        }

        Type listType = property.PropertyType.GetGenericArguments()[0];

        if (arguments.Count < 2)
        {
            ICollection collection = (ICollection)listInstance;
            response = $"Использование: mod {property.Name} <add|remove> <значение>\n" +
                       $"Тип: {GetTypeName(listType)}, Количество: {collection.Count}";
            return false;
        }

        string action = arguments.At(1).ToLower();
        bool isAdd = action == "a" || action == "add";
        int processedCount = 0;

        for (int i = 2; i < arguments.Count; i++)
        {
            try
            {
                object value = TypeDescriptor.GetConverter(listType).ConvertFromInvariantString(arguments.At(i));

                if (isAdd)
                    property.PropertyType.GetMethod("Add")?.Invoke(listInstance, new[] { value });
                else
                    property.PropertyType.GetMethod("Remove")?.Invoke(listInstance, new[] { value });

                processedCount++;
            }
            catch (Exception ex)
            {
                response = $"\"{arguments.At(i)}\" не является допустимым значением!";
                return false;
            }
        }

        ICollection resultCollection = (ICollection)listInstance;
        response = $"{(isAdd ? "Добавлено" : "Удалено")} элементов: {processedCount}. Всего: {resultCollection.Count}";
        return true;
    }

    private static bool HandleComplexType(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        object? nestedInstance = property.GetValue(instance);
        Type nestedType = property.PropertyType;
        List<PropertyInfo> nestedProperties = nestedType.GetModifiableProperties().ToList();

        if (nestedInstance == null)
        {
            nestedInstance = Activator.CreateInstance(nestedType);
            property.SetValue(instance, nestedInstance);
        }

        if (arguments.Count < 2)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"Вложенный объект {nestedType.Name}. Доступные свойства:");
            foreach (PropertyInfo prop in nestedProperties)
                sb.AppendLine($"  - {prop.Name} ({GetTypeName(prop.PropertyType)}) = {prop.GetValue(nestedInstance) ?? "null"}");

            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }

        int changedCount = 0;
        for (int i = 1; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            string[] parts = arg.Split('=');
            if (parts.Length != 2) continue;

            PropertyInfo? nestedProp = nestedProperties.FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            if (nestedProp != null && TrySetPropertyValue(nestedProp, nestedInstance, parts[1], out _))
                changedCount++;
        }

        response = $"Изменено свойств вложенного объекта: {changedCount}";
        return true;
    }

    private static bool HandleSimpleType(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        object? currentValue = property.GetValue(instance);

        if (arguments.Count < 2)
        {
            response = $"Использование: mod {property.Name} <значение>\n" +
                       $"Тип: {GetTypeName(property.PropertyType)}, Текущее: {currentValue ?? "null"}";
            return false;
        }

        try
        {
            string inputValue = arguments.At(1);
            object value;
            
            if (property.PropertyType.IsEnum)
            {
                value = Enum.Parse(property.PropertyType, inputValue, ignoreCase: true);
            }
            else
            {
                value = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(inputValue);
            }
            
            property.SetValue(instance, value);
            response = $"{property.Name} изменено: {currentValue} -> {value}";
            return true;
        }
        catch
        {
            response = $"\"{arguments.At(1)}\" не является допустимым значением для {property.Name}!";
            return false;
        }
    }

    private static bool HandleString(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        if (arguments.Count < 2)
        {
            response = $"Использование: mod {property.Name} <текст>\n" +
                       $"Текущее: {property.GetValue(instance) ?? "(пусто)"}";
            return false;
        }

        string newValue = string.Join(" ", arguments.Skip(1));
        string oldValue = property.GetValue(instance)?.ToString() ?? "(пусто)";

        property.SetValue(instance, newValue);
        response = $"{property.Name} изменено: \"{oldValue}\" -> \"{newValue}\"";
        return true;
    }

    private static bool TrySetPropertyValue(PropertyInfo property, object instance, string value, out string error)
    {
        try
        {
            object converted;

            if (property.PropertyType == typeof(Vector3))
            {
                string[] parts = value.Split(',');
                float x = float.Parse(parts[0]);
                float y = parts.Length > 1 ? float.Parse(parts[1]) : x;
                float z = parts.Length > 2 ? float.Parse(parts[2]) : y;
                converted = new Vector3(x, y, z);
            }
            else if (property.PropertyType == typeof(Vector2))
            {
                string[] parts = value.Split(',');
                float x = float.Parse(parts[0]);
                float y = parts.Length > 1 ? float.Parse(parts[1]) : x;
                converted = new Vector2(x, y);
            }
            else
            {
                converted = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(value);
            }

            property.SetValue(instance, converted);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Не удалось установить значение \"{value}\" для {property.Name}: {ex.Message}";
            return false;
        }
    }

    private static bool IsComplexCollection(Type type)
    {
        if (!typeof(ICollection).IsAssignableFrom(type)) return false;
        Type[] genericArgs = type.GetGenericArguments();
        return genericArgs.Length > 0 && IsComplexType(genericArgs[0]);
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return false;
        if (type.Namespace?.StartsWith("System") == true) return false;
        if (type.Namespace?.StartsWith("UnityEngine") == true) return false; // Игнорируем Unity типы (Vector3 и т.д.)
        return type.IsClass || type.IsValueType;
    }

    private static string GetComplexCollectionHelp(string propertyName, Type elementType,
        List<PropertyInfo> elementProperties, object listInstance)
    {
        ICollection collection = (ICollection)listInstance;
        return $"Свойство \"{propertyName}\" ({elementType.Name}, элементов: {collection.Count})\n" +
               $"Команды: add, remove, set, list\n" +
               $"Свойства элементов: {string.Join(", ", elementProperties.Select(p => p.Name))}";
    }

    private static string GetComplexCollectionList(string propertyName, object listInstance,
        Type elementType, List<PropertyInfo> elementProperties)
    {
        IList list = (IList)listInstance;
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine($"Содержимое {propertyName} ({list.Count} элементов):");

        for (int i = 0; i < list.Count; i++)
        {
            sb.AppendLine($"[{i}]:");
            object? element = list[i];
            if (element == null) continue;
            foreach (PropertyInfo prop in elementProperties)
                sb.AppendLine($"    {prop.Name}: {prop.GetValue(element) ?? "null"}");
        }

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(int)) return "целое число";
        if (type == typeof(float)) return "дробное число";
        if (type == typeof(bool)) return "true/false";
        if (type == typeof(string)) return "текст";
        return type.Name;
    }
}