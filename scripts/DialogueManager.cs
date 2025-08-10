using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class DialogueManager : Node
{
    public static DialogueManager I { get; private set; }

    public bool IsActive { get; private set; }
    public event Action Updated;   // UI listens (optional)
    public event Action Ended;     // Player can re-enable movement

    // ===== Data structures =====
    private class ChoiceDef
    {
        public string text { get; set; }
        public string @goto { get; set; }
    }

    private class NodeDef
    {
        public string speaker { get; set; }
        public string text { get; set; }
        public List<ChoiceDef> choices { get; set; }
        public Dictionary<string, JsonElement> set { get; set; }
        public string @goto { get; set; } // optional direct jump
    }

    // conversations[id] -> list of nodes
    private Dictionary<string, List<NodeDef>> _convos = new();
    // simple global variables store
    private Dictionary<string, JsonElement> _vars = new();

    // current run state
    private List<NodeDef> _current;
    private int _index = -1;
    private string[] _choices;
    private Action<int> _onChoose;

    public override void _EnterTree() => I = this;

    public override void _Ready()
    {
        // Godot 4: keep receiving input while the tree is paused
        ProcessMode = Node.ProcessModeEnum.WhenPaused;
    }
    
    public override void _Input(InputEvent e)
    {
        if (!IsActive) return;

        if (e.IsActionPressed("interact"))
        {
            if (_choices == null || _choices.Length == 0)
                Advance();
        }

        // Optional: number keys select choices
        if (_choices != null && _choices.Length > 0)
        {
            for (int i = 0; i < _choices.Length && i < 9; i++)
            {
                if (Input.IsKeyPressed((Key)((int)Key.Key1 + i)))
                {
                    Choose(i);
                    break;
                }
            }
        }
    }

    // ===== Loading =====
    public void LoadFromJson(string resPath)
    {
        try
        {
            using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
            var json = f.GetAsText();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _convos.Clear();
            _vars.Clear();

            // variables (optional)
            if (root.TryGetProperty("variables", out var vars))
            {
                foreach (var v in vars.EnumerateObject())
                    _vars[v.Name] = v.Value;
            }

            // conversations
            var convosEl = root.GetProperty("conversations");
            foreach (var convo in convosEl.EnumerateObject())
            {
                var list = new List<NodeDef>();
                foreach (var n in convo.Value.EnumerateArray())
                {
                    var node = new NodeDef
                    {
                        speaker = n.TryGetProperty("speaker", out var s) ? s.GetString() : "",
                        text = n.TryGetProperty("text", out var t) ? t.GetString() : ""
                    };

                    if (n.TryGetProperty("choices", out var ch))
                    {
                        node.choices = new();
                        foreach (var c in ch.EnumerateArray())
                        {
                            node.choices.Add(new ChoiceDef
                            {
                                text = c.GetProperty("text").GetString(),
                                @goto = c.GetProperty("goto").GetString()
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
                        node.@goto = g.GetString();

                    list.Add(node);
                }

                _convos[convo.Name] = list;
            }

            GD.Print($"DialogueManager: loaded {_convos.Count} conversations from {resPath}");
        }
        catch (Exception e)
        {
            GD.PushError($"DialogueManager: failed to load '{resPath}': {e.Message}");
        }
    }

    // ===== Running a conversation =====
    public void Start(string conversationId)
    {
        if (!_convos.TryGetValue(conversationId, out _current))
        {
            GD.PushError($"Dialogue '{conversationId}' not found.");
            return;
        }

        IsActive = true;
        _index = -1;
        _choices = null;
        _onChoose = null;
        Advance();
    }

    public void Advance()
    {
        if (!IsActive) return;
        if (_choices != null && _choices.Length > 0) return; // waiting for choice

        _index++;
        if (_index >= _current.Count)
        {
            End();
            return;
        }

        var n = _current[_index];

        // apply variable sets (if any)
        if (n.set != null)
            foreach (var kv in n.set)
                _vars[kv.Key] = kv.Value;

        // handle direct goto
        if (!string.IsNullOrEmpty(n.@goto))
        {
            if (!_convos.TryGetValue(n.@goto, out _current))
            {
                GD.PushError($"Dialogue goto '{n.@goto}' not found.");
                End();
                return;
            }
            _index = -1;
            Advance();
            return;
        }

        // display line
        UIRoot.I.ShowDialogue(n.speaker ?? "", n.text ?? "");
        UIRoot.I.SetChoices(null, null);
        _choices = null;
        _onChoose = null;

        // present choices (if any)
        if (n.choices != null && n.choices.Count > 0)
        {
            var arr = new string[n.choices.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = n.choices[i].text;
            _choices = arr;

            // wire UI buttons to Choose()
            UIRoot.I.SetChoices(_choices, Choose);
        }

        Updated?.Invoke();
    }

    public void Choose(int index)
    {
        if (_choices == null || index < 0 || index >= _choices.Length) return;

        // resolve destination
        var node = _current[_index];
        var dest = node.choices[index].@goto;
        if (!_convos.TryGetValue(dest, out _current))
        {
            GD.PushError($"Dialogue goto '{dest}' not found.");
            End();
            return;
        }

        _index = -1;
        _choices = null;
        _onChoose = null;
        UIRoot.I.SetChoices(null, null);
        Advance();
    }

    public void End()
    {
        IsActive = false;
        UIRoot.I.HideDialogue();

        Ended?.Invoke();
    }

    // ===== Convenience getters for variables (expand later) =====
    public bool GetBool(string key, bool def = false)
    {
        if (_vars.TryGetValue(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
        }
        return def;
    }

    public string GetString(string key, string def = "")
    {
        if (_vars.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();
        return def;
    }

    public int GetInt(string key, int def = 0)
    {
        if (_vars.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
            return i;
        return def;
    }
}