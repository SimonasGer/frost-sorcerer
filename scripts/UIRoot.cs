using Godot;
using System;

public partial class UIRoot : CanvasLayer
{
    [Export] public Panel InteractionPanel;
    [Export] public Label InteractionLabel;

    [Export] public Panel DialoguePanel;
    [Export] public Label SpeakerLabel;
    [Export] public Label TextLabel;
    [Export] public VBoxContainer ChoicesVBox;

    public static UIRoot I { get; private set; }

    public override void _EnterTree() => I = this;

    public override void _Ready()
    {
        InteractionPanel.Visible = false;
        DialoguePanel.Visible = false;
        Layer = 100; // keep UI on top
        DialoguePanel.MouseFilter = Control.MouseFilterEnum.Stop;
        ChoicesVBox.MouseFilter   = Control.MouseFilterEnum.Stop;
    }

    public void ShowPrompt(string text)
    {
        InteractionLabel.Text = text;
        InteractionPanel.Visible = true;
    }

    public void HidePrompt() => InteractionPanel.Visible = false;

    public void ShowDialogue(string speaker, string text)
    {
        DialoguePanel.Visible = true;
        SpeakerLabel.Text = speaker;
        TextLabel.Text = text;
        ClearChoices();

        TogglePlayerMovement(false);
    }

    public void SetChoices(string[] choices, Action<int> onChoose)
    {
        foreach (Node c in ChoicesVBox.GetChildren()) c.QueueFree();
        if (choices == null || choices.Length == 0) return;

        for (int i = 0; i < choices.Length; i++)
        {
            int idx = i;
            var btn = new Button { Text = choices[i], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            btn.Pressed += () => onChoose?.Invoke(idx);
            ChoicesVBox.AddChild(btn);
        }

        DialoguePanel.Visible = true;
    }

    public void HideDialogue()
    {
        DialoguePanel.Visible = false;
        ClearChoices();

        TogglePlayerMovement(true);    // ← unfreeze player here
    }

    private void ClearChoices()
    {
        foreach (Node c in ChoicesVBox.GetChildren()) c.QueueFree();
    }

    private void TogglePlayerMovement(bool canMove)
    {
        // Assumes your Player node is in group "player"
        foreach (var n in GetTree().GetNodesInGroup("player"))
        {
            // Works even if the script type isn't directly referenced:
            n.Set("can_move", canMove);
        }
    }
}
