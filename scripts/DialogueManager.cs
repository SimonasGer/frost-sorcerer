using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class DialogueManager : Node
{
    public static DialogueManager I { get; private set; }

    public bool IsActive { get; private set; }
    public event Action Updated;
    public event Action Ended;

    private class ChoiceDef { public string text { get; set; } public string @goto { get; set; } }
    private class NodeDef
    {
        public string speaker { get; set; }
        public string text { get; set; }
        public List<ChoiceDef> choices { get; set; }
        public Dictionary<string, JsonElement> set { get; set; }
        public string @goto { get; set; }
    }

    // Case-insensitive keys so "Intro" == "intro"
    private readonly Dictionary<string, List<NodeDef>> _convos =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonElement> _vars = new();

    private List<NodeDef> _current;
    private int _index = -1;
    private string[] _choices;

    public override void _EnterTree() => I = this;

    public override void _Ready()
    {
        if (UIRoot.I == null)
            GD.PushError("[DM] UIRoot.I is null. Did you autoload UI.tscn?");
        else
            GD.Print($"[DM] Using UI at {UIRoot.I.GetPath()}");

        LoadFromJson("res://dialogue/dialogue.json");
    }

    public override void _Input(InputEvent e)
    {
        if (!IsActive) return;

        // Advance on Space ONLY when no choices are shown
        if (e.IsActionPressed("interact"))
        {
            if (_choices == null || _choices.Length == 0)
                Advance();

            // Godot 4: stop this input from bubbling to others
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public void LoadFromJson(string resPath)
    {
        try
        {
            using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
            using var doc = JsonDocument.Parse(f.GetAsText());
            var root = doc.RootElement;

            _convos.Clear();
            _vars.Clear();

            if (root.TryGetProperty("variables", out var vars))
                foreach (var v in vars.EnumerateObject())
                    _vars[v.Name] = v.Value;

            foreach (var convo in root.GetProperty("conversations").EnumerateObject())
            {
                var key = convo.Name.Trim(); // normalize id
                var list = new List<NodeDef>();

                foreach (var n in convo.Value.EnumerateArray())
                {
                    var node = new NodeDef
                    {
                        speaker = n.TryGetProperty("speaker", out var s) ? s.GetString() : "",
                        text    = n.TryGetProperty("text", out var t) ? t.GetString() : ""
                    };

                    if (n.TryGetProperty("choices", out var ch))
                    {
                        node.choices = new();
                        foreach (var c in ch.EnumerateArray())
                        {
                            node.choices.Add(new ChoiceDef
                            {
                                text = c.GetProperty("text").GetString(),
                                @goto = c.GetProperty("goto").GetString()?.Trim() // normalize target
                            });
                        }
                    }

                    if (n.TryGetProperty("set", out var setEl))
                    {
                        node.set = new();
                        foreach (var sv in setEl.EnumerateObject())
                            node.set[sv.Name] = sv.Value;
                    }

                    if (n.TryGetProperty("goto", out var g))
                        node.@goto = g.GetString()?.Trim(); // normalize inline goto

                    list.Add(node);
                }

                _convos[key] = list;
            }

            GD.Print($"[DM] Loaded {_convos.Count} conversations from {resPath}");
        }
        catch (Exception ex)
        {
            GD.PushError($"[DM] Failed to load '{resPath}': {ex.Message}");
        }
    }

    public void Start(string conversationId)
    {
        conversationId = conversationId?.Trim();
        if (string.IsNullOrEmpty(conversationId) ||
            !_convos.TryGetValue(conversationId, out _current))
        {
            GD.PushError($"[DM] Dialogue '{conversationId}' not found. Known: {string.Join(", ", _convos.Keys)}");
            return;
        }

        IsActive = true;
        _index = -1;
        _choices = null;
        Advance();
    }

    public void Advance()
    {
        if (!IsActive) return;
        if (_choices != null && _choices.Length > 0) return; // waiting for a choice

        _index++;
        if (_index >= _current.Count) { End(); return; }

        var n = _current[_index];

        if (n.set != null)
            foreach (var kv in n.set)
                _vars[kv.Key] = kv.Value;

        if (!string.IsNullOrEmpty(n.@goto))
        {
            if (!_convos.TryGetValue(n.@goto, out _current))
            {
                GD.PushError($"[DM] goto '{n.@goto}' not found. Known: {string.Join(", ", _convos.Keys)}");
                End();
                return;
            }
            _index = -1;
            Advance();
            return;
        }

        if (UIRoot.I == null) { GD.PushError("[DM] UIRoot.I missing."); return; }

        UIRoot.I.ShowDialogue(n.speaker ?? "", n.text ?? "");
        UIRoot.I.SetChoices(null, null);
        _choices = null;

        if (n.choices != null && n.choices.Count > 0)
        {
            _choices = new string[n.choices.Count];
            for (int i = 0; i < _choices.Length; i++) _choices[i] = n.choices[i].text;
            GD.Print($"[DM] Presenting {_choices.Length} choices");
            UIRoot.I.SetChoices(_choices, Choose); // buttons call back here
        }

        Updated?.Invoke();
    }

    public void Choose(int index)
    {
        GD.Print($"[DM] Choose({index})");
        if (_choices == null || index < 0 || index >= _choices.Length) return;

        var node = _current[_index];
        var dest = node.choices[index].@goto; // already trimmed on load
        GD.Print($"[DM] goto -> '{dest}'");

        if (string.IsNullOrEmpty(dest) || !_convos.TryGetValue(dest, out _current))
        {
            GD.PushError($"[DM] goto '{dest}' not found. Known: {string.Join(", ", _convos.Keys)}");
            // stay on current node; clear choices so UI isn't stuck
            _choices = null;
            UIRoot.I.SetChoices(null, null);
            Updated?.Invoke();
            return;
        }

        _index = -1;
        _choices = null;
        UIRoot.I.SetChoices(null, null);
        Advance();
    }

    public void End()
    {
        IsActive = false;
        if (UIRoot.I != null) UIRoot.I.HideDialogue();
        Ended?.Invoke();
    }

    // Variable helpers (kept for later)
    public bool GetBool(string key, bool def = false) =>
        _vars.TryGetValue(key, out var v) ? v.ValueKind == JsonValueKind.True ? true :
        v.ValueKind == JsonValueKind.False ? false : def : def;

    public string GetString(string key, string def = "") =>
        _vars.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : def;

    public int GetInt(string key, int def = 0) =>
        _vars.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : def;
}
