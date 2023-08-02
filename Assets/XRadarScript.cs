using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class XRadarScript : MonoBehaviour
{
    public Transform[] Anchors;
    public Transform LightParent, Pivot, CameraRig;
    public GameObject[] Shapes, Buttons;
    public Color[] Colors;
    public string[] ColorNames;
    public GameObject Screen;
    public RenderTexture RT;
    public Camera Cam;
    public KMSelectable[] ButtonSels;
    public KMAudio Audio;
    public KMBombModule Module;
    public Texture Black;

    private static int _idc = 1;
    private int _id, _correctPresses;
    private int[] _dispCols, _dispShps, _submissionRequired;
    private bool _isSolved;

    private void Start()
    {
        _id = _idc++;
        Color[] newCol = new Color[5];
        GameObject[] newShp = new GameObject[5];

        Colors.CopyTo(newCol, 0);
        Shapes.CopyTo(newShp, 0);
        newCol.Shuffle();
        newShp.Shuffle();

        for(int i = 0; i < 5; ++i)
        {
            Buttons[i].GetComponent<Renderer>().material.color = newCol[i];
            GameObject n = Instantiate(newShp[i], Buttons[i].transform);
            n.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            n.transform.localScale = 0.02f * Vector3.one;
            n.transform.localRotation = Random.rotation;
            n.SetActive(true);
            StartCoroutine(Spin(n));
        }

        CameraRig.localPosition += new Vector3(Random.Range(-1000f, 1000f), Random.Range(0f, 1000f), Random.Range(-1000f, 1000f));
        LightParent.localEulerAngles = new Vector3(Random.Range(0f, 360f), 0f, 0f);
        RT = Instantiate(RT);
        Screen.GetComponent<Renderer>().material.mainTexture = RT;
        Cam.targetTexture = RT;

        _dispCols = new int[5];
        _dispShps = new int[5];
        _dispShps[0] = _dispCols[0] = Random.Range(0, 5);
        for(int i = 1; i < 5; i++)
        {
            _dispCols[i] = Random.Range(0, 5);
            do
                _dispShps[i] = Random.Range(0, 5);
            while(_dispShps[i] == _dispCols[i]);
        }

        Debug.LogFormat("[X-Radar #{0}] The shapes are:", _id);

        for(int i = 0; i < Anchors.Length; i++)
        {
            Transform anchor = Anchors[i];
            GameObject n = Instantiate(newShp[_dispShps[i]], anchor);
            n.transform.localPosition = Vector3.zero;
            n.transform.localRotation = Random.rotation;
            n.transform.localScale = Vector3.one;
            n.GetComponent<Renderer>().material.color = newCol[_dispCols[i]];
            n.SetActive(true);
            Debug.LogFormat("[X-Radar #{0}] {1} in {2}", _id, newShp[_dispShps[i]].name, ColorNames[System.Array.IndexOf(Colors, newCol[_dispCols[i]])]);
        }

        List<int> presses = new List<int>();
        for(int i = 0; i < 5; i++)
        {
            presses.Add(_dispShps[i]);
            presses.Add(_dispCols[i]);
        }
        _submissionRequired = presses.ToArray();

        StartCoroutine(RotateObjects());

        for(int i = 0; i < ButtonSels.Length; i++)
        {
            int j = i;
            ButtonSels[i].OnInteract += () => { Interact(j); return false; };
        }
    }

    private void Interact(int j)
    {
        ButtonSels[j].AddInteractionPunch(.1f);
        Audio.PlaySoundAtTransform("Press", Buttons[j].transform);

        if(!_isSolved)
        {
            if(_submissionRequired[_correctPresses] == j)
            {
                _correctPresses++;
                if(_correctPresses >= 10)
                {
                    Audio.PlaySoundAtTransform("Solve", transform);
                    Debug.LogFormat("[X-Radar #{0}] Module disarmed.", _id);
                    _isSolved = true;
                    Screen.GetComponent<Renderer>().material.mainTexture = Black;
                    Module.HandlePass();
                }
            }
            else
            {
                Debug.LogFormat("[X-Radar #{0}] You pressed {1}, but I expected {2}. Strike!", _id, j, _submissionRequired[_correctPresses]);
                _correctPresses = 0;
                Module.HandleStrike();
            }
        }
    }

    private IEnumerator Spin(GameObject n)
    {
        while(true)
        {
            n.transform.Rotate(new Vector3(20f, 30f, 50f) * Time.deltaTime);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        Destroy(RT);
    }

    private IEnumerator RotateObjects()
    {
        const float ROTATESPEED = 9f;
        while(true)
        {
            Pivot.localEulerAngles = new Vector3(Pivot.localEulerAngles.x, Pivot.localEulerAngles.y + ROTATESPEED * Time.deltaTime, Pivot.localEulerAngles.z);
            yield return null;
        }
    }

#pragma warning disable 414
    private const string TwitchHelpMessage = "Use \"!{0} 1 2 3 4 5\" to press each button in reading order.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if(Regex.IsMatch(command, @"^\s*[1-5](\s*[1-5])*\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            foreach(char c in command)
            {
                switch(c)
                {
                    case '1':
                        ButtonSels[0].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    case '2':
                        ButtonSels[1].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    case '3':
                        ButtonSels[3].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    case '4':
                        ButtonSels[2].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    case '5':
                        ButtonSels[4].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while(!_isSolved)
        {
            ButtonSels[_submissionRequired[_correctPresses]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
