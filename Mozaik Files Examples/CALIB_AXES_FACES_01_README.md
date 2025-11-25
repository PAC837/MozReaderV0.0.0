CALIB_AXES_FACES_01_README.md
1. Purpose

CALIB_AXES_FACES_01.moz is a calibration product used to align:

Mozaik’s job space (X/Y/Z, rotations, faces)

With Unity’s world space (X/Y/Z, quaternions, front vs flip side)

You use this one product to lock in your .moz → Unity transform math (position, rotation, and face handling). Once it matches visually, you reuse the same rules for every cabinet.

2. Mozaik conventions (this rig)
2.1 Axes and cabinet dimensions

Cabinet W (width)

Along Mozaik X (left ↔ right)

Cabinet D (depth)

Along Mozaik Y (front ↔ back)

Cabinet height

Along Mozaik Z (bottom ↔ top)

Origin for this product:

(X=0, Y=0, Z=0) = front / bottom / left of the cabinet.

2.2 Part dimensions

For each CabProdPart in this rig:

PartL = part length along cabinet X (Mozaik L field)

PARTW = part width along cabinet Y / depth (Mozaik W field)

Part thickness is along cabinet Z.

So:

PartL  → Mozaik X axis
PARTW  → Mozaik Y axis
thick  → Mozaik Z axis

3. Contents of CALIB_AXES_FACES_01.moz

There are 13 CabProdPart entries:

3.1 Axis markers

All axis markers are at:

X = 0, Y = 0, Z = 0

Face = 0

R1 = X, R2 = Y, R3 = Z

Origin Marker

Name: Origin Marker

PartL = 50 mm (Mozaik L=50)

PARTW = 50 mm (Mozaik W=50)

All rotations: A1=0, A2=0, A3=0

A small square at the product origin (front/bottom/left).

AXIS_X_POS

Name: AXIS_X_POS

PartL = 600, PARTW = 20

Rotations: A1=0, A2=0, A3=0

Long skinny part running along +X (cabinet width).

AXIS_Y_POS

Name: AXIS_Y_POS

PartL = 400, PARTW = 20

Rotations: A1=0, A2=0, A3=0

Long skinny part running along +Y (cabinet depth).

AXIS_Z_POS

Name: AXIS_Z_POS

PartL = 600, PARTW = 20

Rotations:

A1 = 0 (R1 = X)

A2 = -90 (R2 = Y)

A3 = 0 (R3 = Z)

Same raw PartL/PARTW as a horizontal strip, but rotated so it runs along +Z (vertical).

Together, these four tell you:

Which direction each Mozaik axis points.

How Mozaik uses rotation to turn a horizontal part into a vertical one.

3.2 Rotation tests

All rotation test parts share:

X = 0, Y = 0, Z = 0

PartL = 500, PARTW = 100

R1 = X, R2 = Y, R3 = Z

They differ only in A1/A2/A3:

ROT_RX+90

A1 = +90, A2 = 0, A3 = 0

+90° about Mozaik X.

ROT_RX-90

A1 = -90, A2 = 0, A3 = 0

−90° about Mozaik X.

ROT_RY+90

A1 = 0, A2 = +90, A3 = 0

+90° about Mozaik Y.

ROT_RY-90

A1 = 0, A2 = -90, A3 = 0

−90° about Mozaik Y.

ROT_RZ+90

A1 = 0, A2 = 0, A3 = +90

+90° about Mozaik Z.

ROT_RZ-90

A1 = 0, A2 = 0, A3 = -90

−90° about Mozaik Z.

ROT_RY+90_RZ+90

A1 = 0, A2 = +90, A3 = +90

Combined: 90° about Y, then 90° about Z.

Used to confirm you’re composing rotations in the same order as Mozaik.

These are your ground truth for mapping A1/A2/A3, R1/R2/R3 → Unity quaternions.

3.3 Face / flip tests

Both face tests share:

X = 0, Y = 0, Z = 0

PartL = 600, PARTW = 300

Rotations: A1=0, A2=0, A3=0

FACE_TEST_NORMAL

Name: FACE_TEST_NORMAL

Contains an <OperationHole ... FlipSideOp="False">

The hole is on the normal face (the “front” face in Mozaik).

FACE_TEST_FLIPPED

Name: FACE_TEST_FLIPPED

Contains an <OperationHole ... FlipSideOp="True">

The identical hole is applied to the flip-side of the panel.

These two are used to pin down:

How Mozaik encodes “this machining is on the flipped face”

How to orient normals / materials in Unity when a hole or dado is on the back vs front face.

4. How this ties into Unity

Unity’s rules are fixed:

Unity axes:

X = right

Y = up

Z = forward

Rotations: stored as Quaternion, often edited as Euler angles in degrees.

What you’re solving with this product is the mapping function:

Position mapping example

// Mozaik job units are mm
const float MM_TO_M = 0.001f;

// Mozaik X (cabinet W), Y (cabinet D), Z (height)
// → Unity X, Y, Z
Vector3 MozToUnityPos(float X, float Y, float Z)
{
    return new Vector3(
        X * MM_TO_M,        // PartL → Unity X
        Z * MM_TO_M,        // height → Unity Y
        -Y * MM_TO_M        // depth → Unity -Z (back toward wall)
    );
}


You can flip the sign on Z if you’d rather have +Z going into the room; the calibration rig will show you which choice matches Mozaik’s view best.

Rotation mapping example

You read A1/A2/A3 and R1/R2/R3 from each CabProdPart and map Mozaik axes to Unity axes:

Vector3 AxisFromMoz(char mozAxis)
{
    switch (mozAxis)
    {
        case 'X': return new Vector3(1, 0, 0);  // cabinet W
        case 'Y': return new Vector3(0, 0, -1); // cabinet D → Unity -Z
        case 'Z': return new Vector3(0, 1, 0);  // height → Unity Y
        default:  return Vector3.zero;
    }
}

Quaternion MozToUnityRot(string R1, float A1,
                         string R2, float A2,
                         string R3, float A3)
{
    var q1 = Quaternion.AngleAxis(A1, AxisFromMoz(R1[0]));
    var q2 = Quaternion.AngleAxis(A2, AxisFromMoz(R2[0]));
    var q3 = Quaternion.AngleAxis(A3, AxisFromMoz(R3[0]));

    // Try q = q3 * q2 * q1 vs q1 * q2 * q3 with this rig and keep the one that matches.
    return q3 * q2 * q1;
}


You use the rotation test parts (ROT_RX+90, ROT_RY+90, etc.) to confirm you’ve chosen the correct axis mapping and multiplication order.

Face / flip mapping

From the .moz:

FACE_TEST_NORMAL → FlipSideOp="False"

FACE_TEST_FLIPPED → FlipSideOp="True"

In Unity, you can:

Treat the panel’s local +Z (thickness) as the “front face” normal.

If you detect FlipSideOp="True" (for a particular op, or for that part’s main machining), apply an extra 180° rotation about the thickness axis or flip normals/material assignment for that face.

The face tests let you visually confirm that the face with the hole in Mozaik matches the face with the “front” material in Unity.

5. Summary

Unity already knows how its axes and rotations work.

This calibration product does not change Unity.

It provides a known Mozaik scene so your importer can figure out:

How to map (X, Y, Z) → Unity (X, Y, Z).

How to map (A1/A2/A3, R1/R2/R3) → Unity Quaternion.

How to map FlipSideOp / faces → Unity normals/material sides.

Once CALIB_AXES_FACES_01 looks identical in Mozaik and Unity, you’ve solved the problem: every other .moz can run through the same transform functions and “just work.”