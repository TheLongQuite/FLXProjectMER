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
        Log.Debug($"[Modify] Найдено {properties.Count} модифицируемых свойств: {
            string.Join(", ", properties.Select(p => p.Name))}");

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

        Log.Debug($"[Modify] Найдено свойство {foundProperty.Name} типа {foundProperty.PropertyType.Name}");

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
            Log.Debug($"[Modify] Объект {mapEditorObject.Id} успешно обновлён");
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
            Log.Error($"[Modify.HandleComplexCollection] Значение свойства {property.Name} равно null");
            response = $"Ошибка: свойство {property.Name} не инициализировано (null).";
            return false;
        }

        Type elementType = property.PropertyType.GetGenericArguments()[0];
        List<PropertyInfo> elementProperties = elementType.GetModifiableProperties().ToList();

        Log.Debug($"[Modify.HandleComplexCollection] Обработка {property.Name}, тип элемента: {elementType.Name
        }, свойства элемента: {string.Join(", ", elementProperties.Select(p => p.Name))}");

        if (arguments.Count < 2)
        {
            response = GetComplexCollectionHelp(property.Name, elementType, elementProperties, listInstance);
            return false;
        }

        string action = arguments.At(1).ToLower();
        Log.Debug($"[Modify.HandleComplexCollection] Действие: {action}");

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
        Log.Debug($"[Modify.HandleComplexCollectionAdd] Создание элемента типа {elementType.Name}");

        object newElement;
        try
        {
            newElement = Activator.CreateInstance(elementType);
            Log.Debug($"[Modify.HandleComplexCollectionAdd] Элемент создан успешно");
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
            Log.Debug($"[Modify.HandleComplexCollectionAdd] Обработка аргумента: {arg}");

            string[] parts = arg.Split('=');

            if (parts.Length != 2)
            {
                response = $"Неверный формат аргумента \"{arg}\"!\n" +
                           $"Используйте формат: свойство=значение\n" +
                           $"Пример: mod {property.Name} add Id=teleport1 Chance=50";

                return false;
            }

            string propName = parts[0];
            string propValue = parts[1];

            PropertyInfo? elementProp =
                elementProperties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));

            if (elementProp == null)
            {
                response = $"Свойство \"{propName}\" не найдено в типе {elementType.Name}!\n" +
                           $"Доступные свойства: {string.Join(", ",
                               elementProperties.Select(p => $"{p.Name} ({GetTypeName(p.PropertyType)})"))}";

                return false;
            }

            Log.Debug($"[Modify.HandleComplexCollectionAdd] Установка {elementProp.Name} = {propValue}");

            if (!TrySetPropertyValue(elementProp, newElement, propValue, out string error))
            {
                response = error;
                return false;
            }
        }

        try
        {
            MethodInfo? addMethod = property.PropertyType.GetMethod("Add");
            if (addMethod == null)
            {
                Log.Error($"[Modify.HandleComplexCollectionAdd] Метод Add не найден для типа {property.PropertyType.Name
                }");

                response = "Внутренняя ошибка: метод Add не найден.";
                return false;
            }

            addMethod.Invoke(listInstance, new[] { newElement });

            ICollection collection = (ICollection)listInstance;
            Log.Debug($"[Modify.HandleComplexCollectionAdd] Элемент добавлен, новый размер коллекции: {collection.Count
            }");

            response = $"Элемент успешно добавлен! Всего элементов: {collection.Count}";
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleComplexCollectionAdd] Ошибка при добавлении элемента в коллекцию: {ex}");
            response = "Ошибка при добавлении элемента в коллекцию.";
            return false;
        }
    }

    private static bool HandleComplexCollectionRemove(ArraySegment<string> arguments, PropertyInfo property,
        object listInstance, Type elementType, out string response)
    {
        if (arguments.Count < 3)
        {
            response = $"Использование: mod {property.Name} remove <индекс>\n" +
                       $"Индекс начинается с 0. Используйте 'mod {property.Name} list' для просмотра элементов.";

            return false;
        }

        if (!int.TryParse(arguments.At(2), out int index))
        {
            response = $"\"{arguments.At(2)}\" не является числом!\n" +
                       $"Укажите индекс элемента для удаления (начиная с 0).";

            return false;
        }

        IList list = (IList)listInstance;

        if (index < 0 || index >= list.Count)
        {
            response = $"Индекс {index} вне диапазона!\n" +
                       $"Допустимые индексы: 0 - {list.Count - 1} (всего элементов: {list.Count})";

            return false;
        }

        try
        {
            list.RemoveAt(index);
            Log.Debug($"[Modify.HandleComplexCollectionRemove] Элемент с индексом {index} удалён, осталось: {list.Count
            }");

            response = $"Элемент с индексом {index} удалён! Осталось элементов: {list.Count}";
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleComplexCollectionRemove] Ошибка при удалении элемента с индексом {index}: {ex}");
            response = "Ошибка при удалении элемента.";
            return false;
        }
    }

    private static bool HandleComplexCollectionSet(ArraySegment<string> arguments, object listInstance,
        Type elementType, List<PropertyInfo> elementProperties, out string response)
    {
        if (arguments.Count < 4)
        {
            response = "Использование: mod <свойство> set <индекс> <свойство_элемента>=<значение>\n" +
                       "Пример: mod TargetTeleporters set 0 Chance=75";

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
            response = $"Индекс {index} вне диапазона! Допустимые индексы: 0 - {list.Count - 1}";
            return false;
        }

        object? element = list[index];
        if (element == null)
        {
            response = $"Элемент с индексом {index} равен null!";
            return false;
        }

        for (int i = 3; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            string[] parts = arg.Split('=');

            if (parts.Length != 2)
            {
                response = $"Неверный формат \"{arg}\"! Используйте: свойство=значение";
                return false;
            }

            PropertyInfo? elementProp =
                elementProperties.FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));

            if (elementProp == null)
            {
                response = $"Свойство \"{parts[0]}\" не найдено!\n" +
                           $"Доступные: {string.Join(", ", elementProperties.Select(p => p.Name))}";

                return false;
            }

            if (!TrySetPropertyValue(elementProp, element, parts[1], out string error))
            {
                response = error;
                return false;
            }
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
            Log.Error($"[Modify.HandleSimpleCollection] Значение свойства {property.Name} равно null");
            response = $"Ошибка: свойство {property.Name} не инициализировано (null).";
            return false;
        }

        Type listType = property.PropertyType.GetGenericArguments()[0];

        if (arguments.Count < 2)
        {
            ICollection collection = (ICollection)listInstance;
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"Использование: mod {property.Name} <add|remove> <значение1> [значение2] ...");
            sb.AppendLine($"Тип элементов: {GetTypeName(listType)}");
            sb.AppendLine($"Текущее количество элементов: {collection.Count}");

            if (listType.IsEnum)
            {
                sb.AppendLine($"Допустимые значения:");
                foreach (object val in Enum.GetValues(listType))
                    sb.AppendLine($"  - {val}");
            }

            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }

        string action = arguments.At(1).ToLower();

        if (action != "a" && action != "add" && action != "rm" && action != "remove")
        {
            response = $"Неизвестное действие \"{action}\"! Используйте: add или remove";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = $"Укажите хотя бы одно значение для {(action.StartsWith("a") ? "добавления" : "удаления")}!";
            return false;
        }

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
                Log.Error($"[Modify.HandleSimpleCollection] Ошибка преобразования \"{arguments.At(i)}\" в {listType.Name
                }: {ex.Message}");

                StringBuilder sb = StringBuilderPool.Shared.Rent();
                sb.AppendLine($"\"{arguments.At(i)}\" не является допустимым значением типа {GetTypeName(listType)}!");

                if (listType.IsEnum)
                {
                    sb.AppendLine("Допустимые значения:");
                    foreach (object val in Enum.GetValues(listType))
                        sb.AppendLine($"  - {val}");
                }

                response = StringBuilderPool.Shared.ToStringReturn(sb);
                return false;
            }
        }

        ICollection resultCollection = (ICollection)listInstance;
        response = $"{(isAdd ? "Добавлено" : "Удалено")} элементов: {processedCount}. Всего в списке: {
            resultCollection.Count}";

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
            try
            {
                nestedInstance = Activator.CreateInstance(nestedType);
                property.SetValue(instance, nestedInstance);
                Log.Debug($"[Modify.HandleComplexType] Создан новый экземпляр {nestedType.Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Modify.HandleComplexType] Не удалось создать экземпляр {nestedType.Name}: {ex}");
                response = $"Не удалось создать объект типа {nestedType.Name}.";
                return false;
            }
        }

        if (arguments.Count < 2)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"Свойство \"{property.Name}\" является вложенным объектом типа {nestedType.Name}");
            sb.AppendLine($"Использование: mod {property.Name} <свойство>=<значение> [свойство2=значение2] ...");
            sb.AppendLine("Доступные свойства:");

            foreach (PropertyInfo prop in nestedProperties)
            {
                object? value = prop.GetValue(nestedInstance);
                sb.AppendLine($"  - {prop.Name} ({GetTypeName(prop.PropertyType)}) = {value ?? "null"}");
            }

            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }

        int changedCount = 0;

        for (int i = 1; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            string[] parts = arg.Split('=');

            if (parts.Length != 2)
            {
                response = $"Неверный формат \"{arg}\"! Используйте: свойство=значение";
                return false;
            }

            PropertyInfo? nestedProp =
                nestedProperties.FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));

            if (nestedProp == null)
            {
                response = $"Свойство \"{parts[0]}\" не найдено в {nestedType.Name}!\n" +
                           $"Доступные: {string.Join(", ", nestedProperties.Select(p => p.Name))}";

                return false;
            }

            if (!TrySetPropertyValue(nestedProp, nestedInstance, parts[1], out string error))
            {
                response = error;
                return false;
            }

            changedCount++;
        }

        response = $"Изменено свойств вложенного объекта: {changedCount}";
        return true;
    }

    private static bool HandleSimpleType(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        object? currentValue = null;
        try
        {
            currentValue = property.GetValue(instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleSimpleType] Ошибка получения значения {property.Name}: {ex.Message}");
        }

        if (arguments.Count < 2)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"Использование: mod {property.Name} <значение>");
            sb.AppendLine($"Ожидаемый тип: {GetTypeName(property.PropertyType)}");
            sb.AppendLine($"Текущее значение: {currentValue ?? "null"}");
            
            if (property.PropertyType.IsEnum)
            {
                bool isFlags = property.PropertyType.GetCustomAttribute<FlagsAttribute>() != null;
                
                if (isFlags)
                {
                    sb.AppendLine();
                    sb.AppendLine("Это флаговое тип - можно комбинировать значения:");
                    sb.AppendLine("  - Указать число (сумма нужных значений)");
                    sb.AppendLine("  - Указать имена через запятую: Value1,Value2,Value3");
                }
                
                sb.AppendLine();
                sb.AppendLine("Допустимые значения:");
                foreach (object val in Enum.GetValues(property.PropertyType))
                    sb.AppendLine($"  - {val} = {Convert.ToInt32(val)}");
            }
            
            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }

        try
        {
            string inputValue = arguments.At(1);
            object value;
            
            if (property.PropertyType.IsEnum)
            {
                bool isFlags = property.PropertyType.GetCustomAttribute<FlagsAttribute>() != null;
                
                if (int.TryParse(inputValue, out int numericValue))
                {
                    value = Enum.ToObject(property.PropertyType, numericValue);
                }
                else if (isFlags && inputValue.Contains(','))
                {
                    string[] flagNames = inputValue.Split(',');
                    int combinedValue = 0;
                    
                    foreach (string flagName in flagNames)
                    {
                        string trimmedName = flagName.Trim();
                        try
                        {
                            object flagValue = Enum.Parse(property.PropertyType, trimmedName, true);
                            combinedValue |= Convert.ToInt32(flagValue);
                        }
                        catch (ArgumentException)
                        {
                            response = $"Неизвестное значение флага \"{trimmedName}\"!\n" +
                                       $"Допустимые значения: {string.Join(", ", Enum.GetNames(property.PropertyType))}";
                            return false;
                        }
                    }
                    
                    value = Enum.ToObject(property.PropertyType, combinedValue);
                }
                else
                {
                    try
                    {
                        value = Enum.Parse(property.PropertyType, inputValue, ignoreCase: true);
                    }
                    catch (ArgumentException)
                    {
                        StringBuilder sb = StringBuilderPool.Shared.Rent();
                        sb.AppendLine($"\"{inputValue}\" не является допустимым значением для {property.Name}!");
                        sb.AppendLine();
                        sb.AppendLine("Допустимые значения:");
                        foreach (object val in Enum.GetValues(property.PropertyType))
                            sb.AppendLine($"  - {val} = {Convert.ToInt32(val)}");
                        
                        response = StringBuilderPool.Shared.ToStringReturn(sb);
                        return false;
                    }
                }
            }
            else
            {
                value = TypeDescriptor.GetConverter(property.PropertyType)
                    .ConvertFromInvariantString(inputValue);
            }
            
            property.SetValue(instance, value);
            
            Log.Debug($"[Modify.HandleSimpleType] {property.Name}: {currentValue} -> {value}");
            response = $"{property.Name} изменено: {currentValue} -> {value}";
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.HandleSimpleType] Ошибка преобразования \"{arguments.At(1)}\" в {property.PropertyType.Name}: {ex.Message}");
            
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"\"{arguments.At(1)}\" не является допустимым значением типа {GetTypeName(property.PropertyType)}!");

            if (property.PropertyType.IsEnum)
            {
                sb.AppendLine();
                sb.AppendLine($"Допустимые значения для {property.Name}:");
                foreach (object val in Enum.GetValues(property.PropertyType))
                    sb.AppendLine($"  - {val} = {Convert.ToInt32(val)}");
            }

            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }
    }

    private static bool HandleString(ArraySegment<string> arguments, PropertyInfo property,
        object instance, out string response)
    {
        if (arguments.Count < 2)
        {
            response = $"Использование: mod {property.Name} <текст>\n" +
                       $"Текущее значение: {property.GetValue(instance) ?? "(пусто)"}";

            return false;
        }

        StringBuilder spacedStringBuilder = StringBuilderPool.Shared.Rent();
        for (int i = 1; i < arguments.Count; i++)
        {
            if (i > 1)
                spacedStringBuilder.Append(' ');

            spacedStringBuilder.Append(arguments.At(i));
        }

        string oldValue = property.GetValue(instance)?.ToString() ?? "(пусто)";
        string newValue = StringBuilderPool.Shared.ToStringReturn(spacedStringBuilder);

        property.SetValue(instance, newValue);

        response = $"{property.Name} изменено: \"{oldValue}\" -> \"{newValue}\"";
        return true;
    }

    private static bool TrySetPropertyValue(PropertyInfo property, object instance, string value, out string error)
    {
        try
        {
            object converted = TypeDescriptor.GetConverter(property.PropertyType)
                .ConvertFromInvariantString(value);

            property.SetValue(instance, converted);

            Log.Debug($"[Modify.TrySetPropertyValue] Установлено {property.Name} = {converted}");
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[Modify.TrySetPropertyValue] Ошибка установки {property.Name}={value}: {ex.Message}");

            StringBuilder sb = StringBuilderPool.Shared.Rent();
            sb.AppendLine($"Не удалось установить значение \"{value}\" для свойства {property.Name}!");
            sb.AppendLine($"Ожидаемый тип: {GetTypeName(property.PropertyType)}");

            if (property.PropertyType.IsEnum)
            {
                sb.AppendLine("Допустимые значения:");
                foreach (object val in Enum.GetValues(property.PropertyType))
                    sb.AppendLine($"  - {val}");
            }

            error = StringBuilderPool.Shared.ToStringReturn(sb);
            return false;
        }
    }

    private static bool IsComplexCollection(Type type)
    {
        if (!typeof(ICollection).IsAssignableFrom(type))
            return false;

        Type[] genericArgs = type.GetGenericArguments();
        if (genericArgs.Length == 0)
            return false;

        return IsComplexType(genericArgs[0]);
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return false;

        if (type.Namespace?.StartsWith("System") == true)
            return false;

        return type.IsClass || type.IsValueType;
    }

    private static string GetComplexCollectionHelp(string propertyName, Type elementType,
        List<PropertyInfo> elementProperties, object listInstance)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        ICollection collection = (ICollection)listInstance;

        sb.AppendLine($"Свойство \"{propertyName}\" - это список объектов типа {elementType.Name}");
        sb.AppendLine($"Текущее количество элементов: {collection.Count}");
        sb.AppendLine();
        sb.AppendLine("Доступные команды:");
        sb.AppendLine($"  mod {propertyName} list                           - показать все элементы");
        sb.AppendLine($"  mod {propertyName} add [свойство=значение] ...    - добавить элемент");
        sb.AppendLine($"  mod {propertyName} remove <индекс>                - удалить элемент");
        sb.AppendLine($"  mod {propertyName} set <индекс> свойство=значение - изменить элемент");
        sb.AppendLine();
        sb.AppendLine($"Свойства {elementType.Name}:");

        foreach (PropertyInfo prop in elementProperties)
            sb.AppendLine($"  - {prop.Name} ({GetTypeName(prop.PropertyType)})");

        sb.AppendLine();
        sb.AppendLine("Примеры:");
        sb.AppendLine($"  mod {propertyName} add Id=teleport1 Chance=50");
        sb.AppendLine($"  mod {propertyName} set 0 Chance=75");
        sb.AppendLine($"  mod {propertyName} remove 0");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string GetComplexCollectionList(string propertyName, object listInstance,
        Type elementType, List<PropertyInfo> elementProperties)
    {
        IList list = (IList)listInstance;
        StringBuilder sb = StringBuilderPool.Shared.Rent();

        sb.AppendLine($"Содержимое {propertyName} ({list.Count} элементов):");
        sb.AppendLine();

        for (int i = 0; i < list.Count; i++)
        {
            object? element = list[i];
            sb.AppendLine($"[{i}]:");

            if (element == null)
            {
                sb.AppendLine("    (null)");
                continue;
            }

            foreach (PropertyInfo prop in elementProperties)
            {
                object? value = prop.GetValue(element);
                sb.AppendLine($"    {prop.Name}: {value ?? "null"}");
            }
        }

        if (list.Count == 0)
            sb.AppendLine("  (список пуст)");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(int))
            return "целое число";

        if (type == typeof(float))
            return "дробное число";

        if (type == typeof(double))
            return "дробное число";

        if (type == typeof(bool))
            return "true/false";

        if (type == typeof(string))
            return "текст";

        if (type.IsEnum)
            return $"{type.Name}";

        return type.Name;
    }
}