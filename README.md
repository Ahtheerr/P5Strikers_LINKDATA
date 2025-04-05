# P5Strikers_LINKDATA

Commands:

To extract the files from LINKDATA:
```
P5Strikers.exe LINKDATA.IDX LINKDATA.BIN [output_path]
```
-PC must be used when extracting the LINKDATA from the Steam version.

-En can be used to extract only the english files.

Note: -En was not tested on the Switch version.

Note 2: I'm not sure if -En extracts every english file, but it seems so.

To encrypt the files back: (for PC version)
```
P5Strikers.exe enc [folder_path]
```
Note: The names of the files must be unchanged.

Note 2: P5Strikers_LD was made using AI. Every credit goes to [Cethleann](https://github.com/yretenai/Cethleann), because they documented everything this program does.

# Linkdata.exe

Note: This program was edited, but the credits goes to Falo, on this [GBATemp post](https://gbatemp.net/threads/dragon-quest-builders-2.528161/post-8466669)

Command:
```
Linkdata.exe injectfolder LINKDATA.BIN [folder]
```

Note: If editing the PC LINKDATA, the folder with the encrypted files must be chosen.
