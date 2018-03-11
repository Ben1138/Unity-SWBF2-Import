# Unity SWBF2 Import

Import Maps from the Star Wars Battlefront II (2005) Mod Tools into Unity.<br />
This Importer is based on the [LibSWBF2](https://github.com/Ben1138/LibSWBF2) Library.
<br /><br />
![](Screenshots/unity2.jpg)
![](Screenshots/unity3.jpg)
<br /><br /><br />
What's supported:
- World ```*.wld``` Files
- Layer ```*.lyr``` Files
- Mesh ```*.msh``` Files (with Materials)
- Terrain ```*.ter Files``` (without Textures, so far just height information)

# How to install
1. Simply put the ```SWBF2Import``` folder into your ```Assets/``` directory
2. You should see a ```SWBF2``` Menu Entry on the top:
<br /><br />
![](Screenshots/menu.jpg)
<br /><br />

# How to use
1. In your ```Assets/``` directory, create a new directory ```Resources/Textures```
2. Put all Textures (```*.tga``` Files) needed into that directory
3. Click on ```SWBF2 --> Open World``` to open the Import Window
4. Click on ```Open *.wld File``` to browse for a world File (e.g. BF2_ModTools/assets/worlds/GEO/world1/geo1.wld)
5. Optional: Specify alternate msh directorys to search for (additional to the local world msh directory)
6. Select if you wish to import the Terrain
7. Select the Layers you wish to import (The number in parentheses indicates the amount of objects that layer contains)
8. Optional: Specify a Material (Shader) that should be used for all imported models. Will use Standard shader if not set
9. Hit ```Import```. This may take a while, so be patient (even if it seems unity froze)
<br /><br />
![](Screenshots/importer.jpg)
