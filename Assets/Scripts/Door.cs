using UnityEngine;

// ANIMATION (Animator): Open() fires the "Open" trigger and the door slides up.
// COLLISION (trigger): once open, touching the door zone wins the game.
// Needs: BoxCollider (Is Trigger) on this object, Animator on the door panel.
public class Door : MonoBehaviour
{
    public Animator animator; // Animator of the door panel (child)

    bool isOpen;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        animator.SetTrigger("Open");
    }

    // Stay (not Enter): the player may already be standing in the zone when it opens
    void OnTriggerStay(Collider other)
    {
        if (isOpen && other.CompareTag("Player"))
        {
            GameManager.Instance.Win();
        }
    }
}
