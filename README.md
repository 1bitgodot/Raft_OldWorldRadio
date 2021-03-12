# Raft_OldWorldRadio
### *A Raft mod to implement a Fallout radio*


[Download and Install Mod](https://www.raftmodding.com/mods/old-world-radio)


## Version 0.2.1 (Fixed Issues in Version 0.2)

### Issues Fixed
- Forgot to set permanent mod on after debugging
- Fixed audio source created in the wrong scene (main menu instead of game world)

The main issue was not testing the mod in permanent mod mode. The bug in v0.2.0 was not detected when the mod is loaded inside the world rather than loaded inside the main menu. The problem is resolved by making sure the audio source is created only in the game world. Version v.0.1.0 did not have this problem as the audio source was created as part of the mod's game object rather than within a separate game object in the world which is necessary to facilitate 3d audio. In the future, I will make sure to test the mod in permanent mod before release.


## Version 0.2 (Do not download, has a breaking bug!)
- Resolved an error with versioning
- Improved audio system (not noticable to the player)
- Improved code stability (less chances of mod crashing)


## Version 0.1
### Current Features & Limitations
- Audio will be "broadcasted" continuously when the mod is loaded
- You need an **Old World Radio** item to listen to the broadcast
- Player automatically gets a **Old World Radio** item when the world is loaded
- The **Old World Radio** will only be switched on when it's in the player's hotbar
- Use console command **take_radio** to remove the radio from the player
- Use console command **give_radio** to give a radio to the player
- Use console command **radio_volume** to see or change the radio volume
- 20 songs are currently included in the **oldworldradio.assets** asset bundle
- The radio looks the same as the basic Raft radio
- The radio can be dropped but not placed

### Future Planned Features
- The radio will have its own unique graphics and 3d model
- The radio can be placed on floors, foundations, and tables
- A placed radio will use 3D positional audio and audio effects
- A quest may need to be completed in order for the radio to be available
- The radio may only be obtained by digging into dirt
