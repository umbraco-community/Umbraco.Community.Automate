# Settings fields: choosing the right editor

Every property on a connection, action or trigger settings class becomes a field in the backoffice. Automate picks a default editor from the property's C# type, but a plain text box is often the wrong control: an option from a fixed list should be a dropdown or radio list, a long message a text area, a number a number input with limits, a folder a media picker. Choosing well stops users typing invalid values, and makes the step obvious to fill in.

This file covers the editors that Umbraco's backoffice provides, the configuration each takes, the C# type to bind it to, and the patterns used in this repo and in the maintainers' other Automate packages.

## Contents

- [How a field is rendered](#how-a-field-is-rendered)
- [Choosing an editor](#choosing-an-editor)
- [Editor reference](#editor-reference)
- [Validation](#validation)
- [Bindings and editors](#bindings-and-editors)
- [Custom editors](#custom-editors)
- [Checklist](#checklist)

## How a field is rendered

```csharp
[Field(
    Label = "Priority",                                   // shown above the field; defaults to the humanised property name
    Description = "How urgently the notification is shown.", // the user's only help text
    SortOrder = 2,                                        // order within the step
    Group = "Advanced",                                   // optional: a separate section, PascalCase
    SupportsBindings = false,                             // true: ${ ... } is evaluated at run time
    IsSensitive = false,                                  // true: encrypted at rest and masked in run logs
    EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",      // which editor to render
    EditorConfig = """[{ "alias": "items", "value": ["Low", "Normal", "High"] }]""")] // that editor's settings
public string Priority { get; set; } = "Normal";          // the initializer is the default value shown
```

- **Always set `Label` and `Description`.** Without them Automate falls back to a localization key (`#uaFields_<settings><property>Label` / `Description`), and since only Automate's own fields have translations, the backoffice shows that raw key. Nothing fails, so add a test that checks every field (see Skoda's `SkodaFieldTests`).
- **`Group`** works the same way: the heading is the key `#uaFieldGroups_<group>Label`, and Automate only translates `Advanced`. Use `Group = "Advanced"`, or ship a localization file that defines your group's label (DevTo's `wwwroot/lang/en.js`, registered in its `umbraco-package.json`, adds `uaFieldGroups: { optionalLabel: "Optional" }`).
- **No `EditorUiAlias`?** Automate infers one from the type: `string` → text box, `int`/`long` → integer, `decimal`/`double` → decimal, `bool` → toggle, `DateTime` → date picker. Set it only when you want something else, or to add configuration.
- **`EditorConfig`** is a JSON array of `{ "alias": ..., "value": ... }` pairs, the same shape as a data type's configuration in Umbraco. Use a C# raw string literal (`"""..."""`) so the JSON needs no escaping. An alias the editor doesn't know is ignored silently, so check it against the reference below.
- **The default value** comes from the property initializer (`= "Normal"`). Give every option-type field a sensible default, so a new step works without the user touching it.

## Choosing an editor

| The value is... | Use | C# type |
|---|---|---|
| Short free text, a name, an ID, a URL (often bound from the trigger) | Text box (the default) | `string` |
| A message, body, description or anything multi-line | `Umb.PropertyEditorUi.TextArea` with `rows` | `string` |
| One of a fixed set of values the service defines | `Umb.PropertyEditorUi.Dropdown` (many options) or `Umb.PropertyEditorUi.RadioButtonList` (2 to 5 options, all visible at once) | `string` |
| Any number of a fixed set | `Umb.PropertyEditorUi.CheckBoxList` | `List<string>` |
| On or off | Toggle (the default for `bool`) | `bool` |
| A whole number with limits | `Umb.PropertyEditorUi.Integer` with `min`, `max`, `step` | `int` / `int?` |
| A decimal number | `Umb.PropertyEditorUi.Decimal` | `decimal` / `double` |
| A number in a range, where the exact value matters less | `Umb.PropertyEditorUi.Slider` | `decimal` |
| A colour | `Umb.PropertyEditorUi.EyeDropper` | `string` (hex) |
| A date or time | Date picker (the default for `DateTime`) | `DateTime` / `DateTime?` |
| A media item or folder | `Umb.PropertyEditorUi.MediaPicker` | `List<MediaPickerValue>` (see below) |
| Document types, media types or member groups to filter on | `Umb.PropertyEditorUi.DocumentTypePicker`, `MediaTypePicker`, `MemberGroupPicker` | `string` |
| Anything the above can't express | A custom editor in `Client/` (see [Custom editors](#custom-editors)) | whatever it stores |

Two rules come before the table:

1. **If the value should come from the trigger or an earlier step, keep it a text field with `SupportsBindings = true`.** For a text box or text area with `SupportsBindings`, Automate swaps in its own binding-aware version, which offers the trigger's and earlier steps' values to pick from. Every other editor is rendered as it is and ignores `SupportsBindings`, so a dropdown, toggle or number input can't take a binding. When a value is usually bound but has a fixed format (a number, a choice), keep it a `string`, say the expected format in the description, and parse it in the action with a clear `Validation` error.
2. **For a fixed set of values, use a list editor, never free text.** Users can't mistype a dropdown, and you don't need to validate the spelling.

## Editor reference

The aliases and configuration below come from Umbraco's backoffice (`@umbraco-cms/backoffice`, Umbraco 17) and Automate's own steps. For an editor that isn't listed, check what value it stores in the Demo site before choosing the C# type.

### Text box

The default for `string`. Only set `EditorUiAlias` to add configuration.

```csharp
[Field(Label = "Title", Description = "The post title. Supports ${ binding } expressions.",
    SupportsBindings = true,
    EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
    EditorConfig = """[{ "alias": "maxChars", "value": 100 }, { "alias": "placeholder", "value": "${ trigger.contentName }" }]""")]
public string Title { get; set; } = string.Empty;
```

Config: `maxChars`, `placeholder`.

### Text area

For messages and bodies. Set `rows` to roughly the length you expect (2 for a short note, 4 to 6 for a message), and keep `SupportsBindings` on.

```csharp
[Field(Label = "Message", Description = "The message to send. Supports ${ binding } expressions.",
    SupportsBindings = true,
    EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
    EditorConfig = """[{ "alias": "rows", "value": 4 }]""")]
public string Message { get; set; } = string.Empty;
```

Config: `rows`.

### Dropdown

For one value from a fixed set. Items can be plain strings, where the label is the value, or `{ "name": ..., "value": ... }` pairs, which show a friendly label but store a stable value. Use pairs whenever the service's value isn't readable (`SkipOnCycle`, `PUBLIC`, `2`): the stored value is what saved automations keep, so it must never change, while the label can.

```csharp
// Plain strings, when the values read well as they are.
[Field(Label = "Priority", Description = "How urgently the notification is shown.",
    EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
    EditorConfig = """[{ "alias": "items", "value": ["Min", "Low", "Default", "High", "Max"] }]""")]
public string Priority { get; set; } = "Default";

// Label/value pairs, when the service's values don't (Automate's own triggers do this).
[Field(Label = "Visibility", Description = "Who can see the post.",
    EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
    EditorConfig = """
        [{ "alias": "items", "value": [
            { "name": "Everyone", "value": "PUBLIC" },
            { "name": "Connections only", "value": "CONNECTIONS" }
        ] }]
        """)]
public string Visibility { get; set; } = "PUBLIC";
```

Bind it to a `string`. The dropdown stores a list (`["High"]`), which Automate's `SingleValueArrayConverter` turns into `"High"`; an empty list becomes `null`.

**An optional choice** ("leave as it is") gets an explicit first item with an empty value, e.g. `{ "name": "Keep current setting", "value": "" }`, on a `string?` property. Picking it stores an empty list, so the action receives `null`: treat null or blank as "not set" (`string.IsNullOrWhiteSpace`), not just null. Skoda's Update Charging Profile does this. To use the value as an enum, keep the property a `string` and parse it in the action (`Enum.TryParse<QrCodeOutputFormat>(settings.Format, ignoreCase: true, out var format)`), failing with `StepRunErrorCategory.Validation` if it doesn't parse. That keeps the stored value a plain string that round-trips through the editor, and a renamed enum member can't break saved automations.

### Radio button list

The same `items` as the dropdown, but every option is visible. Better than a dropdown for two to five options the user should compare (a mode, a direction). Bind to `string`.

```csharp
[Field(Label = "Match mode", Description = "How the name is compared.",
    EditorUiAlias = "Umb.PropertyEditorUi.RadioButtonList",
    EditorConfig = """[{ "alias": "items", "value": ["Exact", "StartsWith", "Contains"] }]""")]
public string MatchMode { get; set; } = "Exact";
```

### Checkbox list

Any number of values from a fixed set. The same `items` format; bind to `List<string>`.

```csharp
[Field(Label = "Platforms", Description = "Where to post. Leave all unticked to post everywhere.",
    EditorUiAlias = "Umb.PropertyEditorUi.CheckBoxList",
    EditorConfig = """[{ "alias": "items", "value": ["Web", "iOS", "Android"] }]""")]
public List<string> Platforms { get; set; } = [];
```

### Toggle

The default for `bool`, so no `EditorUiAlias` is needed. Phrase the label as the "on" state ("Include quiet zone", "Send as HTML") and set the default in the initializer.

```csharp
[Field(Label = "Include quiet zone", Description = "Adds the padding most cameras need to scan the code.")]
public bool IncludeQuietZone { get; set; } = true;
```

Config (optional): `labelOn`, `labelOff`, `showLabels`.

### Integer and decimal

The defaults for whole and decimal numbers. Add `min`, `max` and `step` so the editor stops out-of-range values, and say the range and default in the description. Pair with `[Range]` (see [Validation](#validation)) so the action rejects bad values too.

```csharp
[Range(1, 50)]
[Field(Label = "Size (pixels per module)", Description = "From 1 to 50. Defaults to 20.",
    EditorUiAlias = "Umb.PropertyEditorUi.Integer",
    EditorConfig = """[{ "alias": "min", "value": 1 }, { "alias": "max", "value": 50 }]""")]
public int PixelsPerModule { get; set; } = 20;
```

Config: `min`, `max`, `step`, `placeholder`. Use `int?` when "not set" means something different from a number. `Umb.PropertyEditorUi.Slider` (config `minVal`, `maxVal`, `step`) suits values where the rough position matters more than the exact number.

### Colour

```csharp
[Field(Label = "Dark colour", Description = "A hex colour, e.g. #000000.",
    EditorUiAlias = "Umb.PropertyEditorUi.EyeDropper",
    EditorConfig = """[{ "alias": "showAlpha", "value": false }]""")]
public string DarkColor { get; set; } = "#000000";
```

Config: `showAlpha`, `showPalette`. Stores a hex string.

### Media picker

For choosing a media item or folder. It stores a JSON array of picked items even when limited to one, so bind it to a list of a small record that reads the key:

```csharp
[Field(Label = "Media folder", Description = "Where to save the file. Leave empty for the root of the Media library.",
    EditorUiAlias = "Umb.PropertyEditorUi.MediaPicker",
    EditorConfig = """
        [{ "alias": "multiple", "value": false },
         { "alias": "validationLimit", "value": { "min": 0, "max": 1 } },
         { "alias": "filter", "value": "f38bd2d7-65d0-48e6-95dc-87ce06ec2d3d" }]
        """)]
public List<MediaPickerValue>? MediaFolder { get; set; }

/// <summary>One picked item from the media picker's stored value; only its key is needed.</summary>
public sealed record MediaPickerValue([property: JsonPropertyName("mediaKey")] Guid? MediaKey);
```

Read it with `settings.MediaFolder?.FirstOrDefault()?.MediaKey`. Config: `multiple`, `validationLimit` (`{ "min", "max" }`), `filter` (comma-separated media type keys: `f38bd2d7-65d0-48e6-95dc-87ce06ec2d3d` is the built-in Folder type, so the example only allows folders), `startNodeId`.

### Document type, media type and member group pickers

What Automate's own triggers use to let an automation filter by type ("Only fire for these content types. Leave blank to match all."). Bind to `string`, which holds the selected keys; an empty value means "all".

```csharp
[Field(Label = "Content types", Description = "Only run for these content types. Leave blank to match all.",
    EditorUiAlias = "Umb.PropertyEditorUi.DocumentTypePicker")]
public string? ContentTypes { get; set; }
```

Also `Umb.PropertyEditorUi.MediaTypePicker` and `Umb.PropertyEditorUi.MemberGroupPicker`. Check the stored format in the Demo site before parsing it, and write a test for the parsing.

### Other editors

Umbraco also has `Umb.PropertyEditorUi.Tags`, `MultipleTextString` (a repeatable list of text boxes), `DocumentPicker`, `ContentPicker`, `MemberPicker`, `UserPicker`, `MultiUrlPicker`, `DatePicker`/`DateTimePicker`/`TimeOnlyPicker`, `IconPicker`, `CodeEditor` and `MarkdownEditor`. They work the same way; before using one, save a step in the Demo site and look at the value it stores, then bind a C# type that deserializes it, and test that.

## Validation

Automate checks **data annotations** on action settings before `ExecuteAsync` runs (its `SettingsValidationMiddleware`), and fails the step with `StepRunErrorCategory.Validation` if one fails. `[Required]` also marks the field as required in the editor, and `[Range]` is shown as a rule.

```csharp
using System.ComponentModel.DataAnnotations;

[Required(ErrorMessage = "A value is required.")]
[StringLength(2000, ErrorMessage = "The value can't be longer than 2,000 characters.")]
[Field(Label = "Value", Description = "Up to 2,000 characters. Supports ${ binding } expressions.", SupportsBindings = true)]
public string Value { get; set; } = string.Empty;
```

**Non-nullable text is required, whether or not it has `[Required]`.** Automate adds an implicit `[Required]` to every non-nullable reference-type setting (`string`, a class) except collections, and checks it each time the step runs. `[Required]` rejects empty strings, so an optional setting declared as `public string Range { get; set; } = string.Empty;` fails the step with *The Range field is required* when it's left blank, whatever its description says. Declare optional text as `string?`, and treat null or blank as "not set":

```csharp
[Field(Label = "Range", Description = "A1 range to read, e.g. A2:D50. Leave blank for the whole sheet.")]
public string? Range { get; set; }
```

- Use annotations for rules about one field (required, length, range), with an `ErrorMessage` the user can act on.
- Keep checks that span fields, or that need the connection, in the action itself, failing fast with a clear message before any API call.
- Connection settings get the same checks whenever Automate loads a connection, and `ValidateAsync` runs on **Test connection**. See the warning about connection settings in SKILL.md: an invalid connection can fail other packages' steps.

## Bindings and editors

- `SupportsBindings = true` on every field whose value could reasonably come from the trigger or an earlier step: titles, messages, IDs, URLs, keys. Mention it in the description ("Supports ${ binding } expressions"), with an example where it helps (`${ trigger.contentName }`).
- Leave it off for configuration that's chosen once (a mode, a format, a colour, a folder): those fields use the editors above.
- A field can default to a binding: `public string ContentKey { get; set; } = "${ trigger.contentKey }";` pre-fills the usual value (DevTo does this), so most users never touch it.

## Custom editors

Build your own editor in `Client/` only when no built-in editor can express the value, and the field is central enough to be worth a front end, its build and its tests. Examples: Google Sheets' column list (pairs of column names and bound values), the Kitchen Sink's message editor with a character count, a button that starts an OAuth sign-in from a connection's settings. Set `EditorUiAlias` to your `propertyEditorUi` manifest's alias; see [full.md](full.md#backoffice-front-end-client).

Automate has its own custom editors for its steps (`Umb.Automate.UserGroupPicker`, `Umb.Automate.MemberTypePicker`, the condition builder). They aren't documented for reuse, so don't depend on them.

## Checklist

For every settings property:

- [ ] Label, description and sort order set (a missing label or description shows a raw `#uaFields_...` key). The description says what the value is, any range or format, and the default.
- [ ] A `Group` other than `Advanced` has a localization file defining its label.
- [ ] Could the value come from the trigger? Then it's text with `SupportsBindings = true`.
- [ ] A fixed set of values? Dropdown, radio or checkbox list, with label/value pairs if the stored values aren't readable.
- [ ] A long text? Text area with `rows`.
- [ ] A number? Integer or decimal with `min`/`max`, plus `[Range]`.
- [ ] A default in the initializer that makes a new step work as it is.
- [ ] `[Required]` (or a check in the action) for anything the action can't run without.
- [ ] Optional text is `string?`: a non-nullable `string` is required, even with `= string.Empty`.
- [ ] Credentials: `IsSensitive = true` and a default configuration reference (see SKILL.md).
- [ ] Opened in the Demo site: the editor renders, saves, and the action receives the value.
