using System;

/// <summary>
/// JSON layout of a level (Assets/GlitchVerse/Levels/levelN.json).
/// The editor tool GlitchVerse > Build Game turns these files into scenes, so you can edit a level
/// as text, run the tool again, and get the updated scene.
/// Coordinates: x,y = bottom-left corner of the rectangle, w,h = size (world units). y = 0 is the main floor.
/// </summary>
[Serializable]
public class LevelObj
{
    public string type;     // block, phase, death, zone, plate, door, shard, checkpoint, tip, goal, start, decor
    public string id;       // optional name (plates/doors link by id)
    public float x, y, w, h;

    // block / phase
    public string kind;     // block: solid | shift | shiftinv ; zone: gravity | invert ; death: laser | spikes | spikesdown
    public bool move, del, mat;
    public string axis;     // both | h | v
    public float range;
    public string mats;     // allowed materials, comma separated (Normal,Ice,Mud,Boost,Bouncy)
    public string startMat; // initial material
    public string color;    // ground | accent | wall | gold ... (see builder)
    public string label;    // wheel title for manipulable objects

    // zone
    public float value;     // gravity multiplier (-1 = flip)

    // plate / door
    public string targets;  // plate -> comma separated door ids
    public bool latch;
    public bool flipped;    // plate on a ceiling

    // tip
    public string title;
    public string text;
}

[Serializable]
public class LevelData
{
    public int index;
    public string title;
    public string subtitle;
    public string objective;
    public string tags;
    public int theme = 1;
    public float timeLimit = 180f;
    public float parTime = 90f;
    public float bottomY = -8f;     // camera / kill floor reference
    public float cameraSize = 7.5f; // orthographic size for this level
    public LevelObj[] objects;
}
