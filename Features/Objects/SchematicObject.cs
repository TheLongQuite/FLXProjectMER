using System.Collections.ObjectModel;
using AdminToys;
using Exiled.API.Features;
using Mirror;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;
using Utf8Json;
using Utils.NonAllocLINQ;
using Object = UnityEngine.Object;

namespace ProjectMER.Features.Objects;

public class SchematicObject : MonoBehaviour
{
    public string Name { get; private set; }

    public string DirectoryPath { get; private set; }

    public Room? Room { get; set; }
    
    public Vector3 Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    public Quaternion Rotation
    {
        get => transform.rotation;
        set => transform.rotation = value;
    }

    public Vector3 EulerAngles
    {
        get => Rotation.eulerAngles;
        set => Rotation = Quaternion.Euler(value);
    }

    public Vector3 Scale
    {
        get => transform.localScale;
        set => transform.localScale = value;
    }

    public ObservableCollection<GameObject> AttachedBlocks
    {
        get
        {
            if (_attachedBlocks.Count != 0 && _attachedBlocks.All(x => x != null))
                return _attachedBlocks;

            _attachedBlocks.Clear();
            foreach (Transform transform in GetComponentsInChildren<Transform>())
            {
                if (transform == this.transform)
                    continue;

                _attachedBlocks.Add(transform.gameObject);
            }

            return _attachedBlocks;
        }
    }

    public IReadOnlyList<NetworkIdentity> NetworkIdentities
    {
        get
        {
            if (_networkIdentities.Count > 0 && _networkIdentities.All(x => x != null))
                return _networkIdentities;

            _networkIdentities.Clear();
            foreach (GameObject block in AttachedBlocks)
            {
                if (block.TryGetComponent(out NetworkIdentity networkIdentity))
                    _networkIdentities.Add(networkIdentity);
            }
            
            return _networkIdentities;
        }
    }

    public IReadOnlyList<AdminToyBase> AdminToyBases
    {
        get
        {
            if (_adminToyBases.Count > 0 && _adminToyBases.All(x => x != null))
                return _adminToyBases;

            _adminToyBases.Clear();
            foreach (NetworkIdentity netId in NetworkIdentities)
            {
                if (netId.TryGetComponent(out AdminToyBase adminToyBase))
                    _adminToyBases.Add(adminToyBase);
            }

            return _adminToyBases;
        }
    }

    public AnimationController AnimationController => AnimationController.Get(this);

    public SchematicObject Init(SchematicObjectDataList data, bool shouldBeOptimized = true, Room? room = null)
    {
        Name = Path.GetFileNameWithoutExtension(data.Path);
        DirectoryPath = data.Path;

        Room = room;

        ObjectFromId = new(data.Blocks.Count + 1) { { data.RootObjectId, transform } };

        try
        {
            CreateRecursiveFromID(data.RootObjectId, data.Blocks, transform);
        }
        catch (Exception ex)
        {
            Log.Error($"[SchematicObject.Init] Ошибка при создании блоков схематика {Name}: {ex.Message}");
        }

        try
        {
            AddRigidbodies();
        }
        catch (Exception ex)
        {
            Log.Error($"[SchematicObject.Init] Ошибка при добавлении Rigidbody для {Name}: {ex.Message}");
        }

        try
        {
            AddAnimators();
        }
        catch (Exception ex)
        {
            Log.Error($"[SchematicObject.Init] Ошибка при добавлении аниматоров для {Name}: {ex.Message}");
        }

        Schematic.OnSchematicSpawned(new(this, Name, shouldBeOptimized));

        return this;
    }

    private void CreateRecursiveFromID(int id, List<SchematicBlockData> blocks, Transform parentGameObject, string currentPath = "")
    {
        SchematicBlockData? blockData = blocks.Find(c => c.ObjectId == id);
        
        Transform childGameObjectTransform = CreateObject(blockData, parentGameObject, currentPath) ?? transform;

        int[] parentSchematics =
            blocks.Where(bl => bl.BlockType == BlockType.Schematic).Select(bl => bl.ObjectId).ToArray();

        int childIndex = 0;
        foreach (SchematicBlockData block in blocks.FindAll(c => c.ParentId == id))
        {
            if (parentSchematics.Contains(block.ParentId))
            {
                childIndex++;
                continue;
            }

            string childPath = childIndex + (string.IsNullOrEmpty(currentPath) ? "" : " " + currentPath);
            CreateRecursiveFromID(block.ObjectId, blocks, childGameObjectTransform, childPath);
            childIndex++;
        }
    }

    private Transform? CreateObject(SchematicBlockData? block, Transform parentTransform, string currentPath)
    {
        if (block == null)
            return null;

        GameObject gameObject;
        try
        {
            gameObject = block.Create(this, parentTransform);
            NetworkServer.Spawn(gameObject);
        }
        catch (Exception ex)
        {
            Log.Error($"[SchematicObject.CreateObject] Ошибка создания блока '{block.Name}' (ID: {block.ObjectId}): {ex.Message}");
            return null;
        }

        ObjectFromId.Add(block.ObjectId, gameObject.transform);

        if (!string.IsNullOrEmpty(block.Guid))
        {
            GuidToTransform[block.Guid] = gameObject.transform;
            if (!string.IsNullOrEmpty(currentPath))
                PathToGuid[currentPath] = block.Guid;
        }
        else
        {
            string tempGuid = System.Guid.NewGuid().ToString("N");
            GuidToTransform[tempGuid] = gameObject.transform;
            if (!string.IsNullOrEmpty(currentPath))
                PathToGuid[currentPath] = tempGuid;
        }

        if (block.BlockType != BlockType.Light && !string.IsNullOrEmpty(block.AnimatorName))
        {
            if (TryGetAnimatorController(block.AnimatorName, out RuntimeAnimatorController animatorController))
            {
                _animators.Add(gameObject, animatorController);
            }
        }

        return gameObject.transform;
    }

    public Transform? GetTransform(string guid, string path)
    {
        if (!string.IsNullOrEmpty(guid) && GuidToTransform.TryGetValue(guid, out Transform t))
            return t;

        if (string.IsNullOrEmpty(path))
            return null;

        if (PathToGuid.TryGetValue(path, out string pathGuid) && GuidToTransform.TryGetValue(pathGuid, out Transform pt))
            return pt;

        return FindObjectWithPath(transform, path);
    }

    private static Transform? FindObjectWithPath(Transform target, string pathO)
    {
        if (pathO == "")
            return target;

        string[] path = pathO.Split(' ');
        for (int i = path.Length - 1; i > -1; i--)
        {
            if (target.childCount == 0 || target.childCount <= int.Parse(path[i]))
                return null;

            target = target.GetChild(int.Parse(path[i]));
        }

        return target;
    }

    private bool TryGetAnimatorController(string animatorName, out RuntimeAnimatorController animatorController)
    {
        animatorController = null!;

        if (string.IsNullOrEmpty(animatorName))
            return false;

        string cacheKey = Path.Combine(DirectoryPath, animatorName);

        if (AnimatorControllerCache.TryGetValue(cacheKey, out RuntimeAnimatorController cached))
        {
            animatorController = cached;
            return animatorController != null;
        }

        try
        {
            AssetBundle? matchingBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(x => x.mainAsset != null && x.mainAsset.name == animatorName);

            Object? animatorObject = null;

            if (matchingBundle != null)
            {
                animatorObject = matchingBundle.LoadAllAssets()
                    .FirstOrDefault(x => x is RuntimeAnimatorController);
            }

            if (animatorObject == null)
            {
                if (!File.Exists(cacheKey))
                {
                    AnimatorControllerCache[cacheKey] = null!;
                    return false;
                }

                AssetBundle? bundle = AssetBundle.LoadFromFile(cacheKey);
                if (bundle == null)
                {
                    AnimatorControllerCache[cacheKey] = null!;
                    return false;
                }

                animatorObject = bundle.LoadAllAssets()
                    .FirstOrDefault(x => x is RuntimeAnimatorController);

                if (animatorObject == null)
                {
                    AnimatorControllerCache[cacheKey] = null!;
                    return false;
                }
            }

            animatorController = (RuntimeAnimatorController)animatorObject;
            AnimatorControllerCache[cacheKey] = animatorController;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[TryGetAnimatorController] Ошибка при поиске аниматора '{animatorName}': {ex.Message}");
            AnimatorControllerCache[cacheKey] = null!;
            return false;
        }
    }

    private void AddAnimators()
    {
        if (!_animators.IsEmpty())
        {
            foreach (KeyValuePair<GameObject, RuntimeAnimatorController> pair in _animators)
            {
                try
                {
                    pair.Key.AddComponent<Animator>().runtimeAnimatorController = pair.Value;
                }
                catch (Exception ex)
                {
                    Log.Error($"[AddAnimators] Ошибка добавления аниматора к объекту: {ex.Message}");
                }
            }
        }
        
        _animators.Clear();
        try
        {
            AssetBundle.UnloadAllAssetBundles(false);
        }
        catch (Exception ex)
        {
            Log.Debug($"[AddAnimators] Ошибка выгрузки бандлов: {ex.Message}");
        }
    }

    private bool AddRigidbodies()
    {
        string rigidbodyPath = Path.Combine(DirectoryPath, $"{Name}-Rigidbodies.json");

        if (!RigidbodyCache.TryGetValue(rigidbodyPath, out Dictionary<int, SerializableRigidbody> rigidbodies))
        {
            rigidbodies = LoadRigidbodies(rigidbodyPath);
            RigidbodyCache[rigidbodyPath] = rigidbodies;
        }

        if (rigidbodies == null)
            return false;

        bool hasRigidbodies = false;

        foreach (KeyValuePair<int, SerializableRigidbody> dict in rigidbodies)
        {
            if (!ObjectFromId.TryGetValue(dict.Key, out Transform objTransform))
                continue;

            if (!objTransform.gameObject.TryGetComponent(out Rigidbody rigidbody))
                rigidbody = objTransform.gameObject.AddComponent<Rigidbody>();

            rigidbody.isKinematic = dict.Value.IsKinematic;
            rigidbody.useGravity = dict.Value.UseGravity;
            rigidbody.constraints = dict.Value.Constraints;
            rigidbody.mass = dict.Value.Mass;

            hasRigidbodies = true;
        }

        return hasRigidbodies;
    }

    private static Dictionary<int, SerializableRigidbody> LoadRigidbodies(string rigidbodyPath)
    {
        if (!File.Exists(rigidbodyPath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, SerializableRigidbody>>(File.ReadAllText(rigidbodyPath));
        }
        catch (Exception ex)
        {
            Log.Error($"[AddRigidbodies] Ошибка загрузки Rigidbodies из {rigidbodyPath}: {ex.Message}");
            return null;
        }
    }

    public void Destroy() => Destroy(gameObject);

    public void OnDestroy()
    {
        AnimationController.Dictionary.Remove(this);
        NetworkServer.Destroy(gameObject);
        Schematic.OnSchematicDestroyed(new(this, Name));
    }

    internal Dictionary<int, Transform> ObjectFromId = [];
    public readonly Dictionary<string, Transform> GuidToTransform = new();
    public readonly Dictionary<string, string> PathToGuid = new();

    private readonly ObservableCollection<GameObject> _attachedBlocks = [];
    private readonly List<NetworkIdentity> _networkIdentities = [];
    private readonly List<AdminToyBase> _adminToyBases = [];
    private readonly Dictionary<GameObject, RuntimeAnimatorController> _animators = [];

    private static readonly Dictionary<string, Dictionary<int, SerializableRigidbody>> RigidbodyCache = new();
    private static readonly Dictionary<string, RuntimeAnimatorController> AnimatorControllerCache = new();
}