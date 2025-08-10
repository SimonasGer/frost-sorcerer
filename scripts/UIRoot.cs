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

    public override void _EnterTree()
    {
        I = this; // set the global instance when the autoload enters the tree
    }

    public override void _Ready()
    {
        InteractionPanel.Visible = false;
        DialoguePanel.Visible = false;
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
    }

    public void SetChoices(string[] choices, Action<int> onChoose)
    {
        ClearChoices();
        if (choices == null || choices.Length == 0) return;

        for (int i = 0; i < choices.Length; i++)
        {
            var idx = i; // capture
            var b = new Button { Text = $"{i+1}. {choices[i]}" };
            b.Pressed += () => onChoose(idx);
            ChoicesVBox.AddChild(b);
        }
    }

    public void HideDialogue()
    {
        DialoguePanel.Visible = false;
        ClearChoices();
    }

    private void ClearChoices()
    {
        foreach (Node c in ChoicesVBox.GetChildren()) c.QueueFree();
    }
}
