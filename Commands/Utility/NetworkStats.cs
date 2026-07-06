using System.Text;
using AdminToys;
using CommandSystem;
using Exiled.Permissions.Extensions;
using Mirror;
using NorthwoodLib.Pools;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace ProjectMER.Commands.Utility;

public class NetworkStats : ICommand
{
    public string Command => "netstats";

    public string[] Aliases => ["net"];

    public string Description => "Отображает статистику активных объектов NetworkIdentity на сервере.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"mpr.{Command}"))
        {
            response = $"У вас нет прав для выполнения этой команды. Требуется право: mpr.{Command}";
            return false;
        }

        if (!NetworkServer.active)
        {
            response = "Сетевой сервер не активен.";
            return false;
        }

        List<PrimitiveObjectToy> primitives = [];
        Dictionary<PrimitiveObjectToy, string> primitiveToSchematic = new();
        foreach (NetworkIdentity netId in NetworkServer.spawned.Values)
        {
            if (netId != null && netId.gameObject != null && netId.TryGetComponent(out PrimitiveObjectToy prim))
            {
                primitives.Add(prim);
                primitiveToSchematic[prim] = GetSchematicName(prim);
            }
        }

        if (primitives.Count == 0)
        {
            response = "На сервере нет активных PrimitiveObjectToy.";
            return true;
        }
        
        int[] parent = new int[primitives.Count];
        for (int i = 0; i < primitives.Count; i++) parent[i] = i;

        for (int i = 0; i < primitives.Count; i++)
        {
            for (int j = i + 1; j < primitives.Count; j++)
            {
                if (!(Vector3.Distance(primitives[i].transform.position, primitives[j].transform.position) <= 12f))
                    continue;

                int rootI = FindParent(parent, i);
                int rootJ = FindParent(parent, j);
                    
                if (rootI != rootJ)
                    parent[rootI] = rootJ;
            }
        }
        
        Dictionary<int, List<int>> clusters = new();
        for (int i = 0; i < primitives.Count; i++)
        {
            int root = FindParent(parent, i);
            if (!clusters.ContainsKey(root))
                clusters[root] = [];
                
            clusters[root].Add(i);
        }

        Dictionary<string, int> globalSchematicCounts = new();
        foreach (string schemName in primitives.Select(prim => primitiveToSchematic[prim]))
        {
            if (!globalSchematicCounts.ContainsKey(schemName))
                globalSchematicCounts[schemName] = 0;
            
            globalSchematicCounts[schemName]++;
        }

        StringBuilder sb = StringBuilderPool.Shared.Rent();

        sb.AppendLine();
        sb.AppendLine($"<color=red><b>Всего активных PrimitiveObjectToy: {primitives.Count}</b></color>");
        sb.AppendLine($"<color=green><b>Сформировано кластеров (дистанция <= 12): {clusters.Count}</b></color>");
        sb.AppendLine();

        int clusterIndex = 1;
        foreach (List<int>? cluster in clusters.Values.OrderByDescending(c => c.Count))
        {
            Vector3 center = Vector3.zero;
            Dictionary<string, int> clusterSchematicCounts = new();

            foreach (int idx in cluster)
            {
                center += primitives[idx].transform.position;
                string schemName = primitiveToSchematic[primitives[idx]];
                if (!clusterSchematicCounts.ContainsKey(schemName))
                    clusterSchematicCounts[schemName] = 0;
                
                clusterSchematicCounts[schemName]++;
            }

            center /= cluster.Count;

            string color = cluster.Count > 50 ? "red" : cluster.Count > 20 ? "yellow" : "white";
            sb.AppendLine($"<color={color}><b>Кластер {clusterIndex}</b> ({cluster.Count} примитивов): Центр ≈ ({center.x:F1}, {center.y:F1}, {center.z:F1})</color>");
            
            foreach (KeyValuePair<string, int> kvp in clusterSchematicCounts.OrderByDescending(x => x.Value))
                sb.AppendLine($"   └ Схематик: <color=cyan>{kvp.Key}</color> ({kvp.Value} шт.)");

            clusterIndex++;
        }
        
        sb.AppendLine();
        sb.AppendLine("<color=orange><b>--- Всего примитивов по схематикам ---</b></color>");
        
        foreach (KeyValuePair<string, int> kvp in globalSchematicCounts.OrderByDescending(x => x.Value))
            sb.AppendLine($"- <color=cyan>{kvp.Key}</color>: {kvp.Value} примитивов");

        response = StringBuilderPool.Shared.ToStringReturn(sb);
        return true;
    }
    
    private static string GetSchematicName(PrimitiveObjectToy prim)
    {
        Transform current = prim.transform.parent;
        while (current != null)
        {
            if (current.TryGetComponent<SchematicObject>(out SchematicObject schematicObject))
                return schematicObject.Name;

            current = current.parent;
        }
        
        return "Вне схематика (Карта)";
    }
    
    private static int FindParent(int[] parent, int i)
    {
        if (parent[i] == i)
            return i;
        
        return parent[i] = FindParent(parent, parent[i]);
    }
}