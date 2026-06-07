Dialogue System usage

1. Add Assets/Prefabs/Dialog/DialogueSystem.prefab to the scene.
2. Create DialogueData via Create -> Game -> Dialogue Data.
3. Fill the dialogue lines list.
4. Add NPCInteractable to an NPC object.
5. Assign DialogueData to the dialogueData field.
6. Make sure the player object has the Player tag.
7. Approach the NPC and press E.
8. E / Enter / Click advance dialogue lines.
9. Space skips/closes the whole dialogue.

Notes:
- Use only one DialogueSystem in a scene.
- Do not create a separate Canvas for every NPC.
- Dialog Root should be disabled by default.
- Dialogue open / next / close sounds can be assigned on DialogueManager.
- DialogueManager is scene-local and should not use DontDestroyOnLoad.
