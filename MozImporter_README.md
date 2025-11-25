4️⃣ README – MozImporter & MozImporterBounds (and how they tie into Unity)
You can drop this into a file like MozImporter_README.md in your repo.

Overview
This setup is for importing Mozaik .moz cabinet files into Unity and getting:


Properly oriented parts as Unity cubes.


A bounding box representing total width / height / depth.


A resizable rig using a bounds cube + corner handles.


There are three main scripts:


MozImporter.cs


Base importer.


Also contains the shared Moz data/utility types:


MozCabinet


MozPart


MozCoordinateMapper


MozParser






MozImporterBounds.cs


Uses the same shared types.


Imports a cabinet and adds a Bounds cube around all parts.




CabinetBoundsResizer.cs


Uses the Bounds cube as a rig.


Creates a green outline and red corner handles.


Lets you “pull” corners to resize the cabinet; full-width parts stretch.





Mozaik vs Unity – Coordinates & Pivot
Mozaik:


Uses millimeters.


For parts:


X, Y, Z positions are in job/cabinet space.


Origin is basically at front / bottom / left of the cabinet.




Each part has:


PartL (length), PARTW (width/depth), Thickness.


R1/R2/R3 axes and A1/A2/A3 rotation angles.




Unity:


Uses meters.


Default Cube:


Pivot is at the center of the cube.


When you set transform.position, you’re positioning the center, not the front/bottom/left.




So we have two big jobs:


Unit conversion – mm → meters (0.001f).


Pivot correction – convert from “front/bottom/left” in Mozaik to “center” in Unity.


That’s why in SpawnPart we do:


Scale in meters:
float sx = part.PartL * MM_TO_M;
float sy = part.Thickness * MM_TO_M;
float sz = part.PartW * MM_TO_M;



Compute the world/local position from Moz:
Vector3 mozPos = MozCoordinateMapper.PositionMmToUnity(part.X, part.Y, part.Z);
Quaternion mozRot = MozCoordinateMapper.RotationFromMoz(...);



Then offset by half the dimensions so the cube’s center lands where Mozaik’s front/bottom/left would have been:
Vector3 half = new Vector3(
    sx * 0.5f,
    sy * 0.5f,
    -(sz * 0.5f) // negative because we map Mozaik Y → Unity -Z
);

go.transform.localRotation = mozRot;
go.transform.localPosition = mozPos + mozRot * half;



Local vs World:


We keep all parts as children of a root GameObject (the cabinet).


We mostly use localPosition / localScale / localRotation relative to that root.


For the bounds cube:


MozImporterBounds uses world-space Renderer.bounds to compute an overall Bounds.


Then we create a Bounds cube at bounds.center with bounds.size and parent it to the root.





Script Roles
MozImporter.cs


Purpose: Fast, simple way to turn a .moz file into a Unity hierarchy of cubes.


Key pieces:


mozFile: a TextAsset with the contents of your .moz.


MozParser.ParseMozFromText:


Strips any non-XML header from the .moz.


Parses the XML into:


One MozCabinet with a Name.


Many MozPart entries (with position, size, rotation).






MozCoordinateMapper:


Converts Mozaik’s mm coordinates into Unity meters.


Handles the axis swap (X → X, Z → Y, Y → -Z).




SpawnPart:


Builds primitives.


Scales them in meters.


Positions/rotates them based on Mozaik data.


Corrects for center vs. front/bottom/left pivot.






How to use it:


Create an empty GameObject in your scene (e.g. Moz Importer).


Add MozImporter component.


Drag a .moz (or .moz.txt) into the Project, then into mozFile.


In the MozImporter context menu (three dots on the component), click “Import Moz”.


You’ll get:


A root object named after the cabinet (e.g. 87 DH).


Child cubes for each Mozaik part.





MozImporterBounds.cs


Purpose: Same as MozImporter, but also computes and spawns a Bounds cube that wraps all parts.


Why: The Bounds cube is used later by CabinetBoundsResizer as a simple, visual, resizable rig.


How it works:


Parses the .moz file with the same MozParser as MozImporter.


Creates a root:
string rootName = cab.Name + "_WithBounds";
GameObject root = new GameObject(rootName);



For each MozPart, calls its own SpawnPart (similar to MozImporter’s).


While spawning parts, it uses Renderer.bounds (world-space) to grow a Bounds totalBounds.


After all parts are spawned, it calls CreateBoundsObject:


Creates a cube named "Bounds".


Positions it at totalBounds.center.


Scales it to totalBounds.size.


Parents it to the cabinet root.


Optionally assigns a transparent / trigger material.




How to use it:


Create an empty GameObject (e.g. Moz Importer Bounds).


Add MozImporterBounds.


Assign mozFile to your .moz TextAsset.


Click “Import Moz With Bounds” in the component menu.


You’ll get:


A root object, e.g. 87 DH_WithBounds.


Child cubes for each part.


A child cube named Bounds that wraps everything.





CabinetBoundsResizer.cs (bonus rig)


Purpose: Take a cabinet imported via MozImporterBounds and make it interactive:


Hides the solid Bounds cube.


Draws a green wireframe around it.


Adds eight red corner handles (spheres).


When you drag those handles:


Cabinet’s bounds update.


Part positions update accordingly.


“Full width” parts (rod, toe, top, bottom, F.Shelf) stretch with width.


Thickness (Y) stays constant.






Key concepts:


All computations are done in cabinetRoot local space:


Handles are in world space → we convert them to local with InverseTransformPoint.


Compute min/max across all handle positions → new center + size.




The Option A check:


We skip updates if a new size axis is negative (corrupt / flipped).


Zero size is allowed (you just squashed something flat).




It caches:


Original bounds center/size.


Original part positions and scales.




When scaling:


It scales part positions relative to the original center.


It scales X/Z for parts.


It only scales X for “full width” parts (name contains ROD, TOE, TOP, BOTTOM, BOT, FSHELF, F.SHELF, F SHELF).


It keeps Y scale (thickness) unchanged.




How to use it:


After importing with MozImporterBounds, you have 87 DH_WithBounds:


Child parts.


Child Bounds cube.




Add CabinetBoundsResizer to the root (87 DH_WithBounds).


In the Inspector:


cabinetRoot → drag the root (or leave empty; it defaults to this.transform).


boundsCube → drag the Bounds child.


Assign cornerMaterial (red) and outlineMaterial (green).




With autoUpdate checked:


You should see a green box and 8 red handles.


Drag a handle with the Move tool → cabinet resizes.





Recreating from scratch if you nuke everything
If you completely wipe it, the minimum steps to rebuild:


Create MozImporter.cs


Paste the full code from section 1.


This alone gives you:


MozImporter component.


All shared Moz types.






Create MozImporterBounds.cs


Paste section 2.


It will compile because it references shared types from MozImporter.cs.




Create CabinetBoundsResizer.cs


Paste section 3.




Then:


For “just import and view” → use MozImporter.


For “import + bounds rig” → use MozImporterBounds + CabinetBoundsResizer.



Tomorrow when your brain is fresher we can walk through:


Why we chose local vs world here.


How this lines up with your Mozaik calibration rig (CALIB_AXES_FACES_01.moz).


How to extend this to multi-section closets, islands, multiple cabinets, etc.

