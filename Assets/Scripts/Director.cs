using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Keyframe
{
    public string rawText;
    public float timestamp;
    public List<ObjectState> states;
    public Keyframe(float _timestamp, List<string> lines)
    {
        timestamp = _timestamp;
        states = new List<ObjectState>();
        foreach (var line in lines)
        {
            string l = line.Replace("\t", "");
            if (l.StartsWith("temp") || l.StartsWith("tempsub"))
                continue;
            
            ObjectState os = new ObjectState(line);
            states.Add(os);     
        }
    }
}

public class ObjectState
{
    public string name;
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 acc;
    public Vector3 rot;
    public Vector3 rvel;
    public Vector3 racc;

    static Vector3 parseString(string s1, string s2, string s3)
    {
        s1 = s1.Replace(",", "");
        s2 = s2.Replace(",", "");
        s3 = s3.Replace(",", "");
        return new Vector3(float.Parse(s1), float.Parse(s2), float.Parse(s3));
    }
    public ObjectState(string rawText)
    {
        pos = Vector3.zero;
        vel = Vector3.zero;
        acc = Vector3.zero;
        rot = Vector3.zero;
        rvel = Vector3.zero;
        racc = Vector3.zero;

        string[] param = rawText.Split(' ');
        for (int i = 0; i < param.Length; ++i)
        {
            if (param[i] == "object")
            {
                name = param[i + 1];
                ++i;
            }
            else if (param[i] == "pos")
            {
                pos = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
            else if (param[i] == "vel")
            {
                vel = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
            else if (param[i] == "acc")
            {
                acc = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
            else if (param[i] == "rot")
            {
                rot = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
            else if (param[i] == "rvel")
            {
                rvel = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
            else if (param[i] == "racc")
            {
                racc = parseString(param[i + 1], param[i + 2], param[i + 3]);
            }
        }
    }
}

public class Timeline
{
    public List<Keyframe> keyframes;
    public Timeline()
    {
        keyframes = new List<Keyframe>();
    }
}

public class Director : MonoBehaviour
{

    public GameObject box;
    public Text infoText;
    Timeline timeline;
    int currentFrame = 0;

    string ReadMdfLocal()
    {
        return File.ReadAllText("Assets/model_small.mdf");
    }
    void ParseMdf(string text)
    {
        string[] lines = text.Split('\n');
        timeline = new Timeline();
        List<string> keyframeLines = new List<string>();
        float timestamp = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("time"))
            {
                string[] param = line.Split(' ');

                float temp;
                if (float.TryParse(param[1], out temp))
                {
                    timestamp = temp;
                    keyframeLines.Add(line);
                }
            }
            else if (line.StartsWith("\t"))
            {
                keyframeLines.Add(line);
            }
            // close keyframe
            else if (keyframeLines.Count > 0 && line.Trim().Length == 0)
            {
                Keyframe kf = new Keyframe(timestamp, keyframeLines);
                timeline.keyframes.Add(kf);
                keyframeLines.Clear();
            }
        }

        foreach (var k in timeline.keyframes)
        {
            Debug.Log("Parse done");
            Debug.Log($"timestamp {k.timestamp}, count {k.states.Count}");
        }

    }

    public void RunTimeline(string text)
    {
        Debug.Log(text);
        ParseMdf(text);

        // Replay from frame 0
        int currentFrame = 0;
        UpdateObjectState(currentFrame);
        StartCoroutine(RunKeyFrame(++currentFrame));
    }

    void Start()
    {
#if UNITY_EDITOR
        string text = ReadMdfLocal();
        RunTimeline(text);
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
        // disable WebGLInput.captureAllKeyboardInput so elements in web page can handle keabord inputs
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }

    void UpdateObjectState(int frameIndex)
    {
        Debug.Log($"Updating frame no.{frameIndex}");
        infoText.text = $"Running frame no. {frameIndex}";

        foreach (var s in timeline.keyframes[frameIndex].states)
        {
            Rigidbody rb = box.GetComponent<Rigidbody>();
            rb.position = s.pos;
            rb.velocity = s.vel;
            rb.rotation = Quaternion.Euler(s.rot);
            rb.angularVelocity = s.rvel;
        }
    }
    IEnumerator RunKeyFrame(int frameIndex)
    {
        float prevTs = timeline.keyframes[frameIndex - 1].timestamp;
        float thisTs = timeline.keyframes[frameIndex].timestamp;
        yield return new WaitForSeconds(thisTs-prevTs);
        currentFrame = frameIndex;
        UpdateObjectState(frameIndex);

        ++frameIndex;
        if (frameIndex < timeline.keyframes.Count)
        {
            StartCoroutine(RunKeyFrame(frameIndex));
        }
        yield return null;

    }

    private void FixedUpdate()
    {
        if (currentFrame >= timeline.keyframes.Count)
            return;

        foreach (var s in timeline.keyframes[currentFrame].states)
        {
            Rigidbody rb = box.GetComponent<Rigidbody>();
            rb.AddForce(s.acc, ForceMode.Acceleration);
            rb.AddTorque(s.racc, ForceMode.Acceleration);
        }
    }

}
