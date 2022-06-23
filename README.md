# CamTest2

To run this with the data examples I have been given:
1) Open the scene (these are not included in the repo due to size constraints). There is a good chance that there will be a lot of shader errors.
2) Add an empty object called "PartsList" to the scene.
3) Add all instruction parts to this object (in the order the instructions are to be viewed)
4) Add another empty object (preferably called "ScriptObject") to the scene.
6) Add the "SetupSceneUtil" script to this object.
7) Tick the box "Setup All" in the editor. Everything necessary to run the script should now be handled (including shader errors).

Controls:
Only very simple controls have been added. The spacebar moves to the next instruction. 
Left and right arrow keys cycle through the found positions (right key for worse positions)
