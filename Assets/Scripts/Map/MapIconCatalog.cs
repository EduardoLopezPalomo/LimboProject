using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves a <see cref="MapIconType"/> to one of the drawings in Assets/UI Images/Map/Elements,
/// so "which drawing means which thing" is data and never a switch in code.
/// The 20 icons are small and stay as single sprites; no atlas is introduced (D-11 Q6).
/// </summary>
[CreateAssetMenu(fileName = "MapIconCatalog", menuName = "Map/Map Icon Catalog")]
public class MapIconCatalog : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public MapIconType type;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    [SerializeField, Tooltip("Used when a marker asks for a type this catalog has no drawing for, so it never renders an empty RectTransform.")]
    private Sprite missingSprite;

    private readonly Dictionary<MapIconType, Sprite> lookup = new Dictionary<MapIconType, Sprite>();
    private readonly HashSet<MapIconType> warnedMissing = new HashSet<MapIconType>();

    /// <summary>The authored pairs. Read-only view of a private list (T-016).</summary>
    public IReadOnlyList<Entry> Entries => entries;

    /// <summary>How many entries map to a usable sprite. The editor validator reports the gap against the enum.</summary>
    public int CoveredCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].sprite != null) count++;
            return count;
        }
    }

    public Sprite Get(MapIconType type)
    {
        if (lookup.Count != entries.Count)
            Rebuild();

        if (lookup.TryGetValue(type, out Sprite sprite) && sprite != null)
            return sprite;

        if (missingSprite == null)
        {
            if (warnedMissing.Add(type))
                Debug.LogError($"[MapIconCatalog] '{name}' has no sprite for {type} and no fallback sprite. Markers of this type stay invisible.", this);
            return null;
        }

        if (warnedMissing.Add(type))
            Debug.LogWarning($"[MapIconCatalog] '{name}' has no sprite for {type}; using the fallback drawing instead.", this);

        return missingSprite;
    }

    public bool Has(MapIconType type)
    {
        if (lookup.Count != entries.Count)
            Rebuild();

        return lookup.TryGetValue(type, out Sprite sprite) && sprite != null;
    }

    /// <summary>Adds or replaces an entry. Used by the editor tool that seeds the catalog from the
    /// Elements folder; nothing calls it at runtime.</summary>
    public void SetEntry(MapIconType type, Sprite sprite)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].type != type) continue;
            Entry entry = entries[i];
            entry.sprite = sprite;
            entries[i] = entry;
            Rebuild();
            return;
        }

        entries.Add(new Entry { type = type, sprite = sprite });
        Rebuild();
    }

    /// <summary>Sets the drawing used for types this catalog has none for. Editor-time only.</summary>
    public void SetMissingSprite(Sprite sprite)
    {
        missingSprite = sprite;
    }

    private void Rebuild()
    {
        lookup.Clear();
        warnedMissing.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.type == MapIconType.None) continue;
            lookup[entry.type] = entry.sprite;
        }
    }
}
