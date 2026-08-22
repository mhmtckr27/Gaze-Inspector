![screenshot](Editor/Icons/gaze_banner.png)
# Gaze Inspector

A focused set of attributes and drawers designed to fill the gaps the default Unity inspector leaves behind. It provides a serializable dictionary, subclass pickers for `[SerializeReference]`, conditional visibility rules, pagination for large collections, and inline validation.


## Features

*   **Serialized Dictionary:** A two-column editable table layout with key/value search and duplicate key detection.
*   **Polymorphic Fields:** Native UI subclass pickers for any `[SerializeReference]` field.
*   **Conditional Fields:** `[ShowIf]`, `[HideIf]`, `[EnableIf]`, and `[DisableIf]` attributes to dynamically structure the inspector.
*   **Paginated Collections:** High-performance pagination for massive lists, arrays, and dictionaries.
*   **Validation & Buttons:** `[Required]` inline error validation and `[Button]` attributes for parameterless method execution.

## Install
Gaze ships as a Unity asset, and it is open source.

[Unity Asset Store](https://u3d.as/47tu): import through Window → Package Manager → My Assets  
[GitHub](https://github.com/mhmtckr27/Gaze-Inspector.git): 
source, releases and issues. Window → Package Manager → + → Add package from git URL:  
`https://github.com/mhmtckr27/Gaze-Inspector.git`

### Requirements
- Unity 2022.3 or newer (including Unity 6).

## Quick Look

```csharp
[SerializeField] private bool useCustomRate;

[ShowIf(nameof(useCustomRate))]
[SerializeField] private float spawnRate = 1f;
```

**Documentation**<br>
Refer to the [Documentation](https://graywolfstudio.net/docs/gaze) for more information.

**License**<br>
See [LICENSE](LICENSE).

**Support**<br>
info@graywolfstudio.net · [Discord](https://discord.gg/D7BAezWAMA)
