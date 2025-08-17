# Remarks

## Number of Civilizations

The maximum number of civilizations that can participate in a game is limited to 8. This total includes the player and the Barbarians, which means that up to 6 civilizations can be controlled by the AI in a single game.

Although there are 14 different civilizations available in the game, they are organized into pairs of "buddy civilizations." Only one civilization from each pair can appear in a game at the same time, so certain combinations are not possible. This pairing system is hardcoded throughout the game, making it difficult to modify without significant changes.

To alter or expand this behavior, it would be necessary to move away from the original game’s logic and storage format. Implementing a custom save format and new logic would allow for more flexibility in the number and combination of civilizations.

A particular challenge arises with the game's replay feature. The replay system only records the player numbers (0-7), not the civilization IDs (0-14). As a result, changing the civilization system would also require a redesign of the replay functionality to properly track and display the correct civilizations.
