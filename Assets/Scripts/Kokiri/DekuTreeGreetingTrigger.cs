using UnityEngine;
using ZeldaOoT.Inventory;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Great Deku Tree intro speech welcoming Link inside.
    /// </summary>
    public class DekuTreeGreetingTrigger : MonoBehaviour
    {
        private bool hasSpoken = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasSpoken) return;
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerInventory>() != null)
            {
                hasSpoken = true;

                string[] speech = new string[]
                {
                    "Welcome, Link... I am the Great Deku Tree, protector of this sacred forest.",
                    "A wicked curse has been cast upon me by a man from the desert... A parasite named Queen Gohma is destroying my vitality from within.",
                    "With your Kokiri Sword and Deku Shield, you now have the courage and strength to enter.",
                    "Step inside my trunk, navigate the deep roots, and cleanse the evil that festers within me!"
                };

                if (ZeldaDialogueBox.Instance != null)
                {
                    ZeldaDialogueBox.Instance.ShowDialogue("Great Deku Tree", speech);
                }
            }
        }
    }
}
