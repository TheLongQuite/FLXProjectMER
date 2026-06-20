using Newtonsoft.Json.Linq;

namespace ProjectMER.Features.Extensions;

public static class StringExtensions
{
    public static List<Dictionary<string, object>> GetListOfDicts(this string key, Dictionary<string, object> properties)
    {
        List<Dictionary<string, object>> result = new();

        if (properties == null || !properties.TryGetValue(key, out object obj))
            return result;

        if (obj is JArray jArray)
        {
            foreach (JToken item in jArray)
            {
                if (item is JObject jObj)
                    result.Add(jObj.ToObject<Dictionary<string, object>>());
            }
        }
        else if (obj is List<object> list)
        {
            foreach (object item in list)
            {
                if (item is Dictionary<string, object> dict)
                    result.Add(dict);
                else if (item is JObject jObj)
                    result.Add(jObj.ToObject<Dictionary<string, object>>());
            }
        }

        return result;
    }

    public static List<string> GetStringList(this string key, Dictionary<string, object> properties)
    {
        List<string> result = new();

        if (properties == null || !properties.TryGetValue(key, out object obj))
            return result;

        if (obj is JArray jArray)
        {
            foreach (JToken item in jArray)
                result.Add(item.ToString());
        }
        else if (obj is List<object> list)
        {
            foreach (object item in list)
                result.Add(Convert.ToString(item));
        }

        return result;
    }
}