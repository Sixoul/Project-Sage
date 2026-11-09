using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using ZeldaOoT.Player;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Classic Ocarina of Time Dialogue Box system.
    /// Features typewriter text, speaker titles, paging, and action button confirmation.
    /// </summary>
    public class ZeldaDialogueBox : MonoBehaviour
    {
        public static ZeldaDialogueBox Instance { get; private set; }

        private VisualElement dialogueContainer;
        private Label speakerLabel;
        private Label contentLabel;
        private Label continuePrompt;

        private Queue<string> currentPages = new Queue<string>();
        private string activePageText = "";
        private bool isTyping = false;
        private Action onDialogueComplete;
        private Coroutine typingRoutine;

        public bool IsOpen => dialogueContainer != null && dialogueContainer.style.display == DisplayStyle.Flex;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }
        }

        private void Start()
        {
            var uiDoc = FindFirstObjectByType<UIDocument>();
            if (uiDoc != null && uiDoc.rootVisualElement != null)
            {
                BuildDialogueUI(uiDoc.rootVisualElement);
            }
        }

        public void BuildDialogueUI(VisualElement root)
        {
            if (dialogueContainer != null) return;

            dialogueContainer = new VisualElement();
            dialogueContainer.name = "OoT_DialogueContainer";
            dialogueContainer.style.position = Position.Absolute;
            dialogueContainer.style.bottom = 45;
            dialogueContainer.style.left = Length.Percent(50);
            dialogueContainer.style.width = 680;
            dialogueContainer.style.height = 160;
            dialogueContainer.transform.position = new Vector3(-340, 0, 0); // Center horizontally

            // Authentic OoT Dark Green / Slate Box with Gold border
            dialogueContainer.style.backgroundColor = new Color(0.04f, 0.16f, 0.08f, 0.92f);
            dialogueContainer.style.borderTopWidth = 3;
            dialogueContainer.style.borderBottomWidth = 3;
            dialogueContainer.style.borderLeftWidth = 3;
            dialogueContainer.style.borderRightWidth = 3;
            dialogueContainer.style.borderTopColor = new Color(0.95f, 0.8f, 0.25f);
            dialogueContainer.style.borderBottomColor = new Color(0.95f, 0.8f, 0.25f);
            dialogueContainer.style.borderLeftColor = new Color(0.95f, 0.8f, 0.25f);
            dialogueContainer.style.borderRightColor = new Color(0.95f, 0.8f, 0.25f);
            dialogueContainer.style.borderTopLeftRadius = 10;
            dialogueContainer.style.borderTopRightRadius = 10;
            dialogueContainer.style.borderBottomLeftRadius = 10;
            dialogueContainer.style.borderBottomRightRadius = 10;
            dialogueContainer.style.paddingTop = 14;
            dialogueContainer.style.paddingBottom = 12;
            dialogueContainer.style.paddingLeft = 20;
            dialogueContainer.style.paddingRight = 20;
            dialogueContainer.style.display = DisplayStyle.None; // Hidden by default

            // Speaker Title
            speakerLabel = new Label("Speaker");
            speakerLabel.name = "SpeakerLabel";
            speakerLabel.style.fontSize = 17;
            speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            speakerLabel.style.color = new Color(0.4f, 0.95f, 0.45f);
            speakerLabel.style.marginBottom = 6;
            dialogueContainer.Add(speakerLabel);

            // Message text
            contentLabel = new Label("Dialogue text goes here...");
            contentLabel.name = "ContentLabel";
            contentLabel.style.fontSize = 15;
            contentLabel.style.whiteSpace = WhiteSpace.Normal;
            contentLabel.style.color = Color.white;
            contentLabel.style.flexGrow = 1;
            dialogueContainer.Add(contentLabel);

            // Continue Prompt
            continuePrompt = new Label("▶ [E] Next");
            continuePrompt.name = "ContinuePrompt";
            continuePrompt.style.fontSize = 13;
            continuePrompt.style.color = new Color(0.95f, 0.8f, 0.25f);
            continuePrompt.style.alignSelf = Align.FlexEnd;
            dialogueContainer.Add(continuePrompt);

            root.Add(dialogueContainer);
        }

        public void ShowDialogue(string speaker, string[] pages, Action onComplete = null)
        {
            if (dialogueContainer == null)
            {
                var uiDoc = FindFirstObjectByType<UIDocument>();
                if (uiDoc != null) BuildDialogueUI(uiDoc.rootVisualElement);
            }

            if (dialogueContainer == null) return;

            onDialogueComplete = onComplete;
            currentPages.Clear();
            foreach (var p in pages) currentPages.Enqueue(p);

            speakerLabel.text = speaker;
            dialogueContainer.style.display = DisplayStyle.Flex;

            AdvancePage();
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Press 'E' or Attack (Left Click / Space) to advance text
            bool advancePressed = (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                               || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (advancePressed)
            {
                if (isTyping)
                {
                    // Fast forward typing
                    if (typingRoutine != null) StopCoroutine(typingRoutine);
                    contentLabel.text = activePageText;
                    isTyping = false;
                    continuePrompt.style.visibility = Visibility.Visible;
                }
                else
                {
                    AdvancePage();
                }
            }
        }

        private void AdvancePage()
        {
            if (currentPages.Count > 0)
            {
                activePageText = currentPages.Dequeue();
                continuePrompt.text = currentPages.Count > 0 ? "▶ [E] Next" : "▶ [E] Close";
                continuePrompt.style.visibility = Visibility.Hidden;

                if (typingRoutine != null) StopCoroutine(typingRoutine);
                typingRoutine = StartCoroutine(TypewriterRoutine(activePageText));
            }
            else
            {
                CloseDialogue();
            }
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            isTyping = true;
            contentLabel.text = "";

            for (int i = 0; i <= fullText.Length; i++)
            {
                contentLabel.text = fullText.Substring(0, i);
                yield return new WaitForSeconds(0.016f);
            }

            isTyping = false;
            continuePrompt.style.visibility = Visibility.Visible;
        }

        public void CloseDialogue()
        {
            if (dialogueContainer != null) dialogueContainer.style.display = DisplayStyle.None;
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }
    }
}
