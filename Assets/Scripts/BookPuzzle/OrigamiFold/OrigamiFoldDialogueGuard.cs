public static class OrigamiFoldDialogueGuard
{
    public static bool IsDialogueActive()
    {
        return DialogueManager.Instance != null && DialogueManager.IsDialogueActive;
    }
}
