using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;
using FlaxInk;

namespace Game;

public class DialogueManager : Script
{
    [Serialize] public CameraController cameraController; // Reference to CameraController script to disable controls during dialogue
    public DialogueRunner dialogueRunner;
    public JsonAssetReference<InkStory> dialogue;
    public ControlReference<TextBox> TextBox { get; set; }
    public ControlReference<VerticalPanel> ChoicesContainer; // Reference to choices container

    private TextBox textBox;
    private List<Button> choiceButtons = new List<Button>();
    private VerticalPanel choicesPanel;
    private bool wasMouseVisible; // Store original mouse state

    /// <inheritdoc/>
    public override void OnStart()
    {

        // Cache text box control
        textBox = TextBox.Control;
        choicesPanel = ChoicesContainer.Control;

        StartDialogue(dialogue);

        // Subscribe to events
        dialogueRunner.NewDialogueLine += OnNewLine;
        dialogueRunner.NewDialogueChoices += OnNewChoices;
        dialogueRunner.DialogueEnded += OnDialogueEnded;


    }

    public void StartDialogue(JsonAssetReference<InkStory> inkStory)
    {
        // Store original mouse visibility state
        wasMouseVisible = Screen.CursorVisible;
        // Start the dialogue
        dialogueRunner.StartDialogue(inkStory);
        // Hide dialogue UI initially
        textBox.Parent.Visible = false;
    }
    private void OnNewLine(DialogueLine line)
    {
        Debug.Log("New dialogue line");
        textBox.Text = line.Text;
        ClearChoiceButtons(); // Clear choices when new line appears
        textBox.Parent.Visible = true; // Ensure dialogue UI is visible
        cameraController.shouldControl = false; // Disable camera control when dialogue is active

        // Show mouse if we have choices coming up, otherwise keep current state
        if (dialogueRunner.IsStoryActive && dialogueRunner.CurrentChoices.Count > 0)
        {
            showCursor();
        }
    }

    private void showCursor()
    {
        Screen.CursorVisible = true;
        Screen.CursorLock = CursorLockMode.None;

    }

    private void hideCursor()
    {
        Screen.CursorVisible = false;
        Screen.CursorLock = CursorLockMode.Locked;
    }
    private void OnNewChoices(List<DialogueChoice> choices)
    {
        Debug.Log($"Got {choices.Count} choices");
        ClearChoiceButtons();

        // Show mouse when choices appear
        showCursor();

        if (ChoicesContainer != null && choices.Count > 0)
        {
            CreateChoiceButtons(choices);
        }
        else
        {
            // Fallback: show choices in text box
            string choicesText = textBox.Text + "\n\n";
            for (int i = 0; i < choices.Count; i++)
            {
                choicesText += $"{i + 1}. {choices[i].Text}\n";
            }
            textBox.Text = choicesText;
        }
    }

    private void OnDialogueEnded()
    {
        Debug.Log("Dialogue finished!");
        textBox.Text = "Conversation ended.";
        textBox.Parent.Visible = false; // Hide dialogue UI
        ClearChoiceButtons();
        cameraController.shouldControl = true; // Enable camera control when dialogue ends
        // Restore original mouse visibility when dialogue ends
        if (wasMouseVisible)
            showCursor();
        else
            hideCursor();
    }

    private void CreateChoiceButtons(List<DialogueChoice> choices)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];

            // Create button - no need to set bounds with VerticalPanel!
            var button = new Button
            {
                Text = choice.Text,
                Height = 30, // Set consistent height
                Parent = choicesPanel
            };

            // Capture the index for the click event
            int choiceIndex = i;
            button.Clicked += () => OnChoiceSelected(choiceIndex);

            choiceButtons.Add(button);
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        dialogueRunner.ChooseChoice(choiceIndex, true); // true = auto continue after choice

        // Hide mouse after choice is made (until next choices appear)
        if (dialogueRunner.CurrentChoices.Count == 0)
        {
            hideCursor();
        }
    }

    private void ClearChoiceButtons()
    {
        foreach (var button in choiceButtons)
        {
            button.Dispose();
        }
        choiceButtons.Clear();
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // F key to continue to next line (when no choices are present)
        if (Input.GetKeyUp(KeyboardKeys.F))
        {
            if (dialogueRunner.IsStoryActive && dialogueRunner.CurrentChoices.Count == 0)
            {
                dialogueRunner.ContinueDialogue();

                // Hide mouse when advancing dialogue without choices
                if (dialogueRunner.CurrentChoices.Count == 0)
                {
                    hideCursor();
                }
            }
        }

        // Number keys as alternative to button clicks
        if (dialogueRunner.IsStoryActive && dialogueRunner.CurrentChoices.Count > 0)
        {
            if (Input.GetKeyUp(KeyboardKeys.Numpad1) && dialogueRunner.CurrentChoices.Count >= 1)
            {
                dialogueRunner.ChooseChoice(0, true);
                hideCursor();
            }
            else if (Input.GetKeyUp(KeyboardKeys.Numpad2) && dialogueRunner.CurrentChoices.Count >= 2)
            {
                dialogueRunner.ChooseChoice(1, true);
                hideCursor();
            }
            else if (Input.GetKeyUp(KeyboardKeys.Numpad3) && dialogueRunner.CurrentChoices.Count >= 3)
            {
                dialogueRunner.ChooseChoice(2, true);
                hideCursor();
            }
        }

        // Optional: Press Escape to hide mouse during dialogue
        if (Input.GetKeyUp(KeyboardKeys.Escape) && dialogueRunner.IsStoryActive)
        {
            if (Screen.CursorVisible)
                hideCursor();
            else
                showCursor();
        }
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Unsubscribe from events
        if (dialogueRunner != null)
        {
            dialogueRunner.NewDialogueLine -= OnNewLine;
            dialogueRunner.NewDialogueChoices -= OnNewChoices;
            dialogueRunner.DialogueEnded -= OnDialogueEnded;
        }
        ClearChoiceButtons();

        // Restore original mouse visibility
        if (wasMouseVisible)
            showCursor();
        else
            hideCursor();
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        // Ensure mouse state is restored when script is destroyed
        if (wasMouseVisible)
            showCursor();
        else
            hideCursor();
    }
}