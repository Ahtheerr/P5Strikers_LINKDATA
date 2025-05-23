# P5Strikers_LD

Commands:

To extract the files from LINKDATA:
```
P5Strikers.exe LINKDATA.IDX LINKDATA.BIN [output_path]
```
-PC must be used when extracting the LINKDATA from the Steam version.

-En can be used to extract only the english files.

Note: -En was not tested on the Switch version.

Note 2: I'm not sure if -En extracts every english file, but it seems so.

The 2 args can be used together or just 1:
```
P5Strikers.exe LINKDATA.IDX LINKDATA.BIN [output_path] -PC -En
```
~~To encrypt the files back: (for PC version)~~ This function still exists, but it is now unecessary due to encryption on import with linkdata.exe
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

Use argument *-En* to encrypt files during import. (For Steam version.)

Note: If editing the PC LINKDATA, the folder with the encrypted files must be chosen.

# P5SPC_LD

Used to extract and inject LINKDATA.IDX of the Steam version.

This tool does not need Command Line, just open it normally and select the game.exe.

Select an IDX to inject it, cancel the operation to extract it.

# ~~P5S_Text~~ Currently broken. Use the Russian Tool from here instead. https://github.com/Ahtheerr/P5-Strikers-LINKDATA-Tools

This tool can export and import texts from files extracted from LINKDATA.

To export:

```
P5S_Text X.bin
```

To import:

```
P5S_Text X.txt
```

Both .txt and .bin have to be on the same folder.

Note: This tool wasn't tested with every single text file, but it worked on the ones I tested on.
