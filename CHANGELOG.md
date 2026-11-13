# Changelog

All notable changes to Gaze are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-11-13

### Added

- **Assets > Gaze > Add Gaze Editor Reference**: adds `GrayWolf.Gaze.Editor` to the
  selected editor-only assembly definitions, for code that uses Gaze's editor API, such
  as your own drawers.

### Changed

- The runtime assemblies, `GrayWolf.Gaze` and `GrayWolf.Gaze.Collections`, now ship
  as precompiled DLLs.

### Upgrading from 1.0.0

- If you added `GrayWolf.Gaze` or `GrayWolf.Gaze.Collections` to an assembly
  definition's references, those entries no longer point at anything. They cause no
  errors, but you can remove them.


## [1.0.0] - 2026-11-04

### Added

- Field attributes: `[ShowIf]`, `[HideIf]`, `[EnableIf]`, `[DisableIf]`,
  `[Required]`, `[InfoBox]`, `[ReadOnly]`, `[LabelText]`, `[OnValueChanged]`,
  `[VectorRange]` and `[CurveRange]`
- `[Button]`, which draws a parameterless method as an Inspector button
- `SerializedDictionary<TKey, TValue>`, and `[SerializedDictionary]` for custom
  column labels and row titles
- `SerializedDictionary` drawer with search across keys and values, duplicate
  key detection, a draggable column divider and pagination
- Property drawers for Unity's built-in types, and for any class or struct
  composed of them
- Support for Unity's own attributes: `[Header]`, `[Space]`, `[Tooltip]`,
  `[Range]`, `[Multiline]`, `[TextArea]`, `[Delayed]`, `[ColorUsage]` and
  `[Flags]`
- `[SerializeReference]` fields draw a type picker for their concrete subclasses
- Reporting for `[SerializeReference]` records whose type is gone, and
  `Clear Missing Records` to drop them
- Saved-data check, which reports a component whose edits did not reach the file,
  and `Rebuild Component` to replace it without breaking references to it
- Right-click property menus, and `PropertyClipboard` for copy and paste
- Public drawer API for writing drawers for your own types and attributes
- Settings window under Tools > Gray Wolf > Gaze
- Master switch to disable Gaze
- Troubleshooting tab in the settings window, with `Verify Saved Data`
