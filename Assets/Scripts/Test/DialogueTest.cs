using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public DialogueData testDialogue;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            DialogueManager.instance.StartDialogue(testDialogue);
        }
    }
}