using System.Collections.Generic;
using UnityEngine;
using ZeldaOoT.Kokiri;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// The iconic "2-3-1" Deku Scrub Order Puzzle in Basement 2 of the Great Deku Tree.
    /// Tracks the sequence of defeated scrubs: Middle (2) -> Right (3) -> Left (1).
    /// Upon completion, reveals Queen Gohma's boss secret and unlocks the Boss Door!
    /// </summary>
    public class DekuScrubOrderPuzzle : MonoBehaviour
    {
        [SerializeField] private GameObject bossDoorBars;
        [SerializeField] private List<DekuScrubAI> scrubs = new List<DekuScrubAI>();

        private readonly List<int> expectedOrder = new List<int> { 2, 3, 1 };
        private List<int> currentSequence = new List<int>();
        private bool isSolved = false;

        public void RegisterScrub(DekuScrubAI scrub)
        {
            if (!scrubs.Contains(scrub)) scrubs.Add(scrub);
        }

        public void SetBossDoorBars(GameObject bars)
        {
            bossDoorBars = bars;
        }

        public void OnScrubStunned(DekuScrubAI scrub, int number)
        {
            if (isSolved) return;

            currentSequence.Add(number);
            int stepIndex = currentSequence.Count - 1;

            if (stepIndex < expectedOrder.Count && currentSequence[stepIndex] == expectedOrder[stepIndex])
            {
                // Correct step so far!
                if (currentSequence.Count == expectedOrder.Count)
                {
                    // Solved!
                    PuzzleSolved();
                }
            }
            else
            {
                // Failed order!
                PuzzleFailed();
            }
        }

        private void PuzzleSolved()
        {
            isSolved = true;

            foreach (var scrub in scrubs)
            {
                scrub.MarkDefeated();
            }

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlaySpinWave(transform.position, 6.5f, 2);
            }

            if (bossDoorBars != null)
            {
                bossDoorBars.SetActive(false);
            }

            // Authentic OoT Scrub dialogue
            string[] surrenderDialogue = new string[]
            {
                "Aieee! I give up! Twenty-three is number one... You really know the secret!",
                "Please spare me and I'll tell you Queen Gohma's weakness!",
                "When Queen Gohma glares at you or crawls along the ceiling, her eye turns fiery RED!",
                "That's when you must shoot her red eye with your Slingshot to blind her!",
                "When she collapses stunned, rush in and strike with your sword! The door is open!"
            };

            if (ZeldaDialogueBox.Instance != null)
            {
                ZeldaDialogueBox.Instance.ShowDialogue("Deku Scrub", surrenderDialogue);
            }
        }

        private void PuzzleFailed()
        {
            string[] failureDialogue = new string[]
            {
                "Hehehe! You didn't do it in the right order!",
                "Start over! Remember: Twenty-three is number one!"
            };

            if (ZeldaDialogueBox.Instance != null)
            {
                ZeldaDialogueBox.Instance.ShowDialogue("Deku Scrub", failureDialogue, () =>
                {
                    currentSequence.Clear();
                    foreach (var s in scrubs)
                    {
                        s.ResetScrub();
                    }
                });
            }
            else
            {
                currentSequence.Clear();
                foreach (var s in scrubs)
                {
                    s.ResetScrub();
                }
            }
        }
    }
}
