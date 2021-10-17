using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MultitaskScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo bomb;
    public GameObject[] hatches;
    public GameObject[] needleobj;
    public GameObject[] arrowobj;
    public GameObject[] gridobj;
    public GameObject[] matchobj;
    public KMSelectable[] needlebuttons;
    public KMSelectable[] arrowbuttons;
    public KMSelectable[] gridbuttons;
    public KMSelectable[] matchbuttons;
    public Renderer[] arrowleds;
    public Renderer[] dodgeleds;
    public Renderer[] gridleds;
    public Renderer[] matchleds;
    public Renderer[] matchbar;
    public Material[] ledcols;
    public Material[] onebit;
    public Material glowGray;
    public TextMesh[] timers;

    private int modnum;
    private static string[] exempt = null;
    private int buffer;
    private bool firstactive;
    private bool[] hatchmove = new bool[4];
    private bool[][] active = new bool[4][] { new bool[4], new bool[10], new bool[25], new bool[10] };
    private int simul;
    private IEnumerator[] countdowns = new IEnumerator[5];
    private IEnumerator[] tasks = new IEnumerator[4];
    private IEnumerator[] arrows = new IEnumerator[10];
    private IEnumerator[] grid = new IEnumerator[25];
    private IEnumerator[] match = new IEnumerator[15];
    private int needleup;
    private float needleangle;
    private bool needlereset = true;
    private int lastactive = 2;
    private int arrowled = 2;
    private int matchcol;

    private bool[] final = new bool[4];
    private bool start;
    private bool moduleSolved;

    private static int moduleIDCounter = 1;
    private static int minmoduleID;
    private static bool stagger;
    private int moduleID;


    private bool TwitchPlaysActive;
    private bool autosolving;
    private float speedMultiplier = 1;
    private float speedInverse = 1;
    private float timerMultiplier = 1;
    private bool disablingNeedle;

    #region ModSettings
    class MultitaskSettings
    {
        public float speedMultiplier = 1;
        public float timerMultiplier = 1;
    }
    MultitaskSettings settings = new MultitaskSettings();
    private static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
          new Dictionary<string, object>
          {
            { "Filename", "MultitaskSettings.json"},
            { "Name", "Multitask" },
            { "Listings", new List<Dictionary<string, object>>
                {
                  new Dictionary<string, object>
                  {
                    { "Key", "speedMultiplier" },
                    { "Text", "Multiplier altering the speed at which the modules function."}
                  },
                  new Dictionary<string, object>
                  {
                    { "Key", "timerMultiplier" },
                    { "Text", "Multiplier altering the length of the module timers.."}
                  }
                }
            }
          }
    };
    void SetSettings()
    {
        if (settings.speedMultiplier == 0)
            speedMultiplier = 1;
        else speedMultiplier = settings.speedMultiplier;
        speedInverse = 1f / settings.speedMultiplier;
        if (settings.timerMultiplier == 0)
            timerMultiplier = 1;
        else timerMultiplier = settings.timerMultiplier;
    }
    #endregion

    void Awake()
    {
        if (!stagger)
        {
            stagger = true;
            minmoduleID = moduleIDCounter;
        }
        moduleID = moduleIDCounter++;
        module.OnActivate = Activate;      
        for (int i = 0; i < 10; i++)
            arrows[i] = Arrow(i);
        for (int i = 0; i < 25; i++)
            grid[i] = Grid(i);
        for (int i = 0; i < 15; i++)
            match[i] = Match(i);
        tasks[0] = Needle();
        for (int i = 1; i < 4; i++)
            tasks[i] = Task(i);
        foreach (KMSelectable button in gridbuttons)
        {
            int i = Array.IndexOf(gridbuttons, button);
            gridbuttons[i].OnInteract += delegate () { GridPress(i); return false; };
        }
        modnum = bomb.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count();
        foreach (KMSelectable button in matchbuttons)
        {
            int i = Array.IndexOf(matchbuttons, button);
            matchbuttons[i].OnInteract += delegate () { MatchPress(i); return false; };
        }
        needlebuttons[0].OnInteract += delegate () { needlebuttons[0].AddInteractionPunch(0.25f); needleup = -1; return false; };
        needlebuttons[0].OnInteractEnded += delegate () { Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform); needleup = 0; };
        needlebuttons[1].OnInteract += delegate () { needlebuttons[0].AddInteractionPunch(0.25f); needleup = 1; return false; };
        needlebuttons[1].OnInteractEnded += delegate () { Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform); needleup = 0; };
        arrowbuttons[0].OnInteract += delegate () { ArrowPress(true); return false; };
        arrowbuttons[1].OnInteract += delegate () { ArrowPress(false); return false; };

        ModConfig<MultitaskSettings> modConfig = new ModConfig<MultitaskSettings>("MultitaskSettings");
        settings = modConfig.Read();
        modConfig.Write(settings);
        SetSettings();
    }

    private void Activate()
    {
        exempt = GetComponent<KMBossModule>().GetIgnoredModules("Multitask", new string[]
        {
            "14",
            "Bamboozling Time Keeper",
            "Brainf---",
            "Divided Squares",
            "Forget Enigma",
            "Forget Everything",
            "Forget It Not",
            "Forget Me Later",
            "Forget Me Not",
            "Forget Perspective",
            "Forget The Colors",
            "Forget Them All",
            "Forget This",
            "Forget Us Not",
            "Hogwarts",
            "Iconic",
            "Organization",
            "Pressure",
            "Purgatory",
            "Random Access Memory",
            "RPS Judging",
            "Simon Forgets",
            "Simon's Stages",
            "Souvenir",
            "Tallordered Keys",
            "The Digits",
            "The Heart",
            "The Swan",
            "The Time Keeper",
            "The Troll",
            "The Twin",
            "The Very Annoying Button",
            "Timing is Everything",
            "Turn The Key",
            "Ultimate Custom Night",
            "Übermodule",
            "Multitask",
            "The Task Master",
            "Simon Supervises",
            "Bad Mouth",
            "Bad TV",
            "Simon Superintends"
        });
        modnum = bomb.GetSolvableModuleNames().Where(x => !exempt.Contains(x)).Count();
        StartCoroutine(HatchMove(0, true));
        if (modnum == 0)
        {
            StartCoroutine(DoneSoSoon());
        }
        else
        {
            StartCoroutine(Simul());
            StartCoroutine(Manager());
        }
        foreach (GameObject obj in needleobj)
            obj.SetActive(false);
        foreach (GameObject obj in arrowobj)
            obj.SetActive(false);
        foreach (GameObject obj in gridobj)
            obj.SetActive(false);
        foreach (GameObject obj in matchobj)
            obj.SetActive(false);
        stagger = false;
        if (TwitchPlaysActive && settings.speedMultiplier == 1 && settings.timerMultiplier == 1)
        {
            speedMultiplier = 0.25f; //Default TP settings.
            speedInverse = 4;
            timerMultiplier = 1.5f;
            foreach (Renderer rn in gridleds)
                rn.transform.parent.gameObject.GetComponent<MeshRenderer>().material = glowGray;
        }
        if (70 * timerMultiplier >= 100)
            timers[0].transform.localScale = new Vector3(0.0012f, 0.001f, 1);
    }

    private IEnumerator Simul()
    {
        while (!final.Contains(true))
        {
            while (simul < Mathf.CeilToInt(4f * bomb.GetSolvedModuleNames().Count() / modnum))
                simul++;
            if (simul > 3)
            {
                final[Random.Range(0, 4)] = true;
                StopAllCoroutines();
                if (autosolving)
                {
                    StartCoroutine(CompletePressure());
                    StartCoroutine(CompleteAvoidance());
                    StartCoroutine(CompleteGrid());
                    StartCoroutine(CompleteMatch());
                }
                if (!start)
                {
                    StartCoroutine(HatchMove(0, true));
                }
                StartCoroutine(Manager());
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator Manager()
    {
        while (!final.Contains(true))
        {
            if (active[0].Where(b => b).Count() < simul)
            {
                int task = Random.Range(0, 4);
                while (active[0][task] || task == lastactive)
                    task = Random.Range(0, 4);
                lastactive = task;
                if (task == 0)
                {
                    needleobj[1].transform.localEulerAngles = new Vector3(90, 0, 0);
                    needleangle = 0;
                    tasks[0] = Needle();
                }
                else
                    tasks[task] = Task(task);
                active[0][task] = true;
                hatchmove[task] = true;
                StartCoroutine(HatchMove(task + 1, true));
                yield return new WaitForSeconds(1);
                int timeset = (int)(timerMultiplier * Random.Range(20, 50));
                Debug.LogFormat("[Multitask #{0}] Activated {1} at {2} for {3} seconds", moduleID, new string[] { "Pressure Gauge", "Avoidance", "Selection Grid", "Signal Jammer" }[task], bomb.GetFormattedTime(), timeset);
                countdowns[task + 1] = Timer(task, timeset);
                timers[task + 1].text = timeset.ToString();
                StartCoroutine(countdowns[task + 1]);
                StartCoroutine(tasks[task]);                    
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        for(int i = 1; i < 5; i++)
        {
            hatches[2 * i].transform.localScale = new Vector3(1.02f, 1, 1);
            hatches[2 * i].transform.localPosition = new Vector3(0, 3.7f, 0);
            hatches[(2 * i) + 1].transform.localPosition = new Vector3(0, 3.7f, -0.482f);
        }
        for (int j = 0; j < 4; j++)
        {
            active[0][j] = false;
            timers[j + 1].text = string.Empty;         
            tasks[j] = j == 0 ? Needle() : Task(j);
        }
        for (int i = 0; i < 10; i++)
        {
            if (active[1][i])
            {
                StopCoroutine(arrows[i]);
                active[1][i] = false;
                for (int j = 0; j < 5; j++)
                    arrowleds[(5 * i) + j].material = ledcols[0];
            }
            arrows[i] = Arrow(i);
            if (i > 4)
                dodgeleds[i].material = ledcols[0];
        }
        for (int j = 0; j < 25; j++)
        {
            if (active[2][j])
            {
                StopCoroutine(grid[j]);
                active[2][j] = false;
                gridleds[j].material = ledcols[0];
            }
            grid[j] = Grid(j);
        }
        for (int i = 0; i < 15; i++)
        {
            StopCoroutine(match[i]);
            if (i < 5)
            {
                for (int j = 0; j < 5; j++)
                    matchleds[(5 * i) + j].material = ledcols[0];
            }
            if(i < 10)
                active[3][i] = false;
            match[i] = Match(i);
        }
        Debug.LogFormat("[Multitask #{0}] Entering standby for final phase", moduleID);
        yield return new WaitForSeconds((75 * (moduleID - minmoduleID)) + 5);
        Debug.LogFormat("[Multitask #{0}] Final phase activated at {1}", moduleID, bomb.GetFormattedTime());
        Audio.PlaySoundAtTransform("HatchOpen", transform);
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(HatchMove(i + 1, true));
            active[0][i] = true;
            StartCoroutine(tasks[i]);
            countdowns[i] = Timer(i, (int)(timerMultiplier * 70));
            StartCoroutine(countdowns[i]);
        }
        needleangle = Random.Range(0, 2) == 0 ? -1f : 1f;
        needleobj[1].transform.localEulerAngles = new Vector3(90f, needleangle, 0f);
        while (hatchmove.Contains(true))
            yield return null;
        for (int i = (int)(timerMultiplier * 70); i > -1; i--)
        {
            timers[0].text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        Audio.PlaySoundAtTransform("InputCorrect", transform);
        Debug.LogFormat("[Multitask #{0}] Final phase deactivated: Module solved", moduleID);
        StartCoroutine(HatchMove(0, false));
        timers[0].text = "GG";
        timers[0].color = new Color32(0, 255, 0, 255);
        moduleSolved = true;
        module.HandlePass();
    }

    private IEnumerator HatchMove(int hatch, bool up)
    {
        if (hatch > 0)
            hatchmove[hatch - 1] = true;
        if (up)
        {
            if(moduleID + 1 == moduleIDCounter && !final.Contains(true))
                 Audio.PlaySoundAtTransform("HatchOpen", transform);
            yield return null;
            switch (hatch)
            {
                case 1:
                    disablingNeedle = false;
                    foreach (GameObject obj in needleobj)
                        obj.SetActive(true);
                    break;
                case 2:
                    foreach (GameObject obj in arrowobj)
                        obj.SetActive(true);
                    break;
                case 3:
                    foreach (GameObject obj in gridobj)
                        obj.SetActive(true);
                    break;
                default:
                    foreach (GameObject obj in matchobj)
                        obj.SetActive(true);
                    break;
            }
        }
        for (int i = 0; i < 60; i++)
        {
            if (hatch == 0)
            {
                hatches[0].transform.localScale = new Vector3(0.112f, 0.002f, up ? Mathf.Lerp(0.112f, 0, (float)i / 60) : Mathf.Lerp(0, 0.112f, (float)i / 60));
                hatches[0].transform.localPosition = new Vector3(0, 0.0277f, up ? Mathf.Lerp(-0.022f, 0.033f, (float)i / 60) : Mathf.Lerp(0.033f, -0.022f, (float)i / 60));
                hatches[1].transform.localPosition = new Vector3(0, 0.0277f, up ? Mathf.Lerp(-0.075f, 0.033f, (float)i / 60) : Mathf.Lerp(0.033f, -0.075f, (float)i / 60));
            }
            else
            {
                hatches[2 * hatch].transform.localScale = new Vector3(1.02f, 1, up ? Mathf.Lerp(1, 0, (float)i / 60) : Mathf.Lerp(0, 1, (float)i / 60));
                hatches[2 * hatch].transform.localPosition = new Vector3(0, 3.7f, up ? Mathf.Lerp(0f, 0.5f, (float)i / 60) : Mathf.Lerp(0.5f, 0f, (float)i / 60));
                hatches[(2 * hatch) + 1].transform.localPosition = new Vector3(0, 3.7f, up ? Mathf.Lerp(-0.482f, 0.5f, (float)i / 60) : Mathf.Lerp(0.5f, -0.482f, (float)i / 60));
            }
            yield return null;
        }
        if (!start)
            start = true;
        if(hatch > 0)
            hatchmove[hatch - 1] = false;
        if (!up)
        {
            switch (hatch)
            {
                case 1:
                    disablingNeedle = true;
                    yield return new WaitForSeconds(.11f);
                    foreach (GameObject obj in needleobj)
                        obj.SetActive(false);
                    break;
                case 2:
                    foreach (GameObject obj in arrowobj)
                        obj.SetActive(false);
                    break;
                case 3:
                    foreach (GameObject obj in gridobj)
                        obj.SetActive(false);
                    break;
                case 4:
                    foreach (GameObject obj in matchobj)
                        obj.SetActive(false);
                    break;
                default:
                    foreach (GameObject obj in arrowobj)
                        obj.SetActive(false);
                    foreach (GameObject obj in gridobj)
                        obj.SetActive(false);
                    foreach (GameObject obj in matchobj)
                        obj.SetActive(false);
                    disablingNeedle = true;
                    yield return new WaitForSeconds(.11f);
                    foreach (GameObject obj in needleobj)
                        obj.SetActive(false);
                    Audio.PlaySoundAtTransform("Slam", transform);
                    break;
            }
            if (hatch > 0)
            {
                yield return new WaitForSeconds(Random.Range(15f, 30f));
                active[0][hatch - 1] = false;
            }
        }
    }

    private IEnumerator Task(int t)
    {
        switch (t)
        {
            case 1:
                while (active[0][1])
                {
                    while (hatchmove[1] || active[1].Where(a => a).Count() > (final[1] ? 6 : 3))
                    {
                        yield return null;
                    }
                    int arrowrand = Random.Range(0, 10);
                    while (active[1][arrowrand])
                        arrowrand = Random.Range(0, 10);
                    active[1][arrowrand] = true;
                    StartCoroutine(arrows[arrowrand]);
                    yield return new WaitForSeconds(Random.Range(0.2f, 2f));
                }
                break;
            case 2:
                while (active[0][2])
                {
                    while (hatchmove[2] || active[2].Where(a => a).Count() > 6)
                    {
                        yield return null;
                    }
                    int gridrand = Random.Range(0, 25);
                    while (active[2][gridrand])
                        gridrand = Random.Range(0, 25);
                    active[2][gridrand] = true;
                    StartCoroutine(grid[gridrand]);
                    yield return new WaitForSeconds(Random.Range(0, Random.Range(4, 8)) == 0 ? Random.Range(0.25f, 1f) : Random.Range(1f, 4f));
                }
                break;
            case 3:
                while (active[0][3])
                {
                    while (hatchmove[3])
                    {
                        yield return null;
                    }
                    int[] matchrand = new int[2] { Random.Range(0, 2), Random.Range(0, 5)};
                    if (matchrand[0] == 0)
                    {
                        if (active[3][matchrand[1]])
                            matchrand[1] += 5;
                        if (active[3][matchrand[1]])
                            matchrand[1] += 5;
                        StartCoroutine(match[matchrand[1]]);
                    }
                    yield return new WaitForSeconds(speedInverse * ( matchrand[0] == 0 ? (final[3] ? 2.4f : 3.2f) : final[3] ? 1.2f : 1.6f));
                }
                break;
        }
    }

    private IEnumerator Timer(int t, int time)
    {
        while (hatchmove[t])
        {
            yield return null;
        }
        for (int i = time; i > -1; i--)
        {
            if (!final.Contains(true))
                timers[t + 1].text = i < 10 ? "0" + i.ToString() : i.ToString();
            yield return new WaitForSeconds(1);
        }      
        timers[t + 1].text = string.Empty;
        StopCoroutine(tasks[t]);
        if (!final.Contains(true))
        {
            Audio.PlaySoundAtTransform("InputCorrect", transform);
            Debug.LogFormat("[Multitask #{0}] OK: {1} deactivated", moduleID, new string[] { "Pressure Gauge", "Avoidance", "Selection Grid", "Signal Jammer" }[t]);
        }
        switch (t)
        {
            case 0:
                needlereset = true;
                break;
            case 1:
                for (int i = 0; i < 10; i++)
                {
                    if (active[1][i])
                    {
                        StopCoroutine(arrows[i]);
                        active[1][i] = false;
                        for (int k = 0; k < 5; k++)
                            arrowleds[(5 * i) + k].material = ledcols[0];
                    }
                    arrows[i] = Arrow(i);
                    if (i > 4)
                        dodgeleds[i].material = ledcols[0];
                }
                break;
            case 2:
                for (int j = 0; j < 25; j++)
                {
                    if (active[2][j])
                    {
                        StopCoroutine(grid[j]);
                        active[2][j] = false;
                        gridleds[j].material = ledcols[0];
                    }
                    grid[j] = Grid(j);
                }
                break;
            case 3:
                for (int i = 0; i < 15; i++)
                {
                    StopCoroutine(match[i]);
                    if (i < 5)
                    {
                        for (int j = 0; j < 5; j++)
                            matchleds[(5 * i) + j].material = ledcols[0];
                    }
                    if(i < 10)
                        active[3][i] = false;
                    match[i] = Match(i);
                }
                break;
        }
        StartCoroutine(HatchMove(t + 1, false));
    }

    private IEnumerator Needle()
    {
        while (active[0][0])
        {
            while (hatchmove[0])
                yield return null;
            if (needlereset)
            {
                needlereset = false;
                needleangle = Random.Range(0, 2) == 0 ? -1f : 1f;
            }
            needleobj[1].transform.localEulerAngles = new Vector3(90, needleangle, 0);
            switch (needleup)
            {
                case -1:
                    needleangle -= 2f;
                    break;
                case 1:
                    needleangle += 2f;
                    break;
                default:
                    if (needleangle < -60)
                        needleangle -= speedMultiplier * 0.7f;
                    else if (needleangle < -30)
                        needleangle -= speedMultiplier * 0.5f;
                    else if (needleangle < 0)
                        needleangle -= speedMultiplier * 0.3f;
                    else if (needleangle > 60)
                        needleangle += speedMultiplier * 0.7f;
                    else if (needleangle > 30)
                        needleangle += speedMultiplier * 0.5f;
                    else
                        needleangle += speedMultiplier * 0.3f;
                    break;
            }
            if (Mathf.Abs(needleangle) > 90)
            {
                module.HandleStrike();
                Debug.LogFormat("[Multitask #{0}] Oops: Pressure too {1}", moduleID, needleangle > 0 ? "high" : "low");
                if (!final.Contains(true))
                {
                    timers[1].text = string.Empty;
                    StopCoroutine(countdowns[1]);
                    StartCoroutine(HatchMove(1, false));
                    yield break;
                }
                else
                    needlereset = true;
            }
            yield return new WaitForSeconds(final[0] ? 0.03f : 0.04f);
        }
    }

    private IEnumerator Arrow(int k)
    {
        Audio.PlaySoundAtTransform("Sharp", transform);
        float waittime = speedInverse * Random.Range(1.5f, 2.5f);
        arrowleds[5 * k + 4].material = ledcols[5];
        for (int i = 4; i > 0; i--)
        {
            yield return new WaitForSeconds(waittime);
            arrowleds[(5 * k) + i].material = ledcols[0];
            arrowleds[(5 * k) + i - 1].material = ledcols[i];
        }
        yield return new WaitForSeconds(waittime);
        for (int i = 0; i < 5; i++)
            arrowleds[5 * k + i].material = ledcols[1];
        dodgeleds[(k % 5) + 5].material = ledcols[1];
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 5; i++)
            arrowleds[5 * k + i].material = ledcols[0];
        dodgeleds[(k % 5) + 5].material = ledcols[0];
        active[1][k] = false;
        Audio.PlaySoundAtTransform("Laser", transform);
        if (arrowled == k % 5)
        {
            module.HandleStrike();
            Audio.PlaySoundAtTransform("Hit", transform);
            Debug.LogFormat("[Multitask #{0}] Oops: Signal {1} not avoided", moduleID, k);
            for (int i = 0; i < 10; i++)
            {
                if (active[1][i])
                {
                    StopCoroutine(arrows[i]);
                    active[1][i] = false;
                    for (int j = 0; j < 5; j++)
                        arrowleds[(5 * i) + j].material = ledcols[0];
                }
                arrows[i] = Arrow(i);
                if (i > 4)
                    dodgeleds[i].material = ledcols[0];
            }
            if (!final.Contains(true))
            {
                StopCoroutine(tasks[1]);
                StopCoroutine(countdowns[2]);
                timers[2].text = string.Empty;              
                StartCoroutine(HatchMove(2, false));
            }
        }
        arrows[k] = Arrow(k);
    }

    private void ArrowPress(bool up)
    {
        if (active[0][1])
        {
            if (up && arrowled > 0)
            {
                arrowbuttons[0].AddInteractionPunch(0.25f);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
                dodgeleds[arrowled].material = onebit[0];
                arrowled--;
                dodgeleds[arrowled].material = onebit[1];
            }
            else if (!up && arrowled < 4)
            {
                arrowbuttons[1].AddInteractionPunch(0.25f);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
                dodgeleds[arrowled].material = onebit[0];
                arrowled++;
                dodgeleds[arrowled].material = onebit[1];
            }
        }
    }

    private IEnumerator Grid(int g)
    {
        active[2][g] = true;
        Audio.PlaySoundAtTransform("Alert", transform);
        float waittime = speedInverse * (final[2] ? 2 : 2.4f);
        gridleds[g].material = ledcols[5];
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(waittime);
            gridleds[g].material = ledcols[4 - i];
        }
        active[2][g] = false;
        module.HandleStrike();
        Debug.LogFormat("[Multitask #{0}] Oops: Cell {1} left active for too long", moduleID, g);
        for (int i = 0; i < 25; i++)
        {
            if (active[2][i])
            {
                StopCoroutine(grid[i]);
                active[2][i] = false;
                gridleds[i].material = ledcols[0];
            }
            grid[i] = Grid(i);
        }
        if (!final.Contains(true))
        {
            StopCoroutine(tasks[2]);
            StopCoroutine(countdowns[3]);
            timers[3].text = string.Empty;         
            StartCoroutine(HatchMove(3, false));
        }
    }

    private void GridPress(int i)
    {
        if (active[0][2])
        {
            gridbuttons[i].AddInteractionPunch(0.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if (active[2][i])
            {
                StopCoroutine(grid[i]);
                grid[i] = Grid(i);
                gridleds[i].material = ledcols[0];
                active[2][i] = false;
            }
            else
            {
                module.HandleStrike();
                Debug.LogFormat("[Multitask #{0}] Oops: Cell {1} pressed while inactive", moduleID, i);
                for (int j = 0; j < 25; j++)
                {
                    if (active[2][j])
                    {
                        StopCoroutine(grid[j]);
                        active[2][j] = false;
                        gridleds[j].material = ledcols[0];
                    }
                    grid[j] = Grid(j);
                }
                if (!final.Contains(true))
                {
                    StopCoroutine(tasks[2]);
                    StopCoroutine(countdowns[3]);
                    timers[3].text = string.Empty;                
                    StartCoroutine(HatchMove(3, false));
                }
            }
        }
    }

    private IEnumerator Match(int d)
    {
        int k = d % 5;
        int m = d / 5;
        if(d < 10)
            active[3][(5 * m) + k] = true;
        Audio.PlaySoundAtTransform("SegSelect" + (k + 1).ToString(), transform);
        for (int i = 0; i < 5; i++)
        {
            if (i > 0)
                matchleds[(5 * k) + i - 1].material = ledcols[0];
            if (active[0][3])
                matchleds[(5 * k) + i].material = ledcols[k + 1];
            yield return new WaitForSeconds(speedInverse * (final[3] ? 1.2f : 1.6f));
        }
        matchleds[(5 * k) + 4].material = ledcols[0];
        matchbar[1].material = ledcols[k + 1];
        yield return new WaitForSeconds(0.1f);
        matchbar[1].material = ledcols[0];
        if(d < 10)
            active[3][(5 * m) + k] = false;
        match[d] = Match(d);
        if (active[0][3])
        {
            if (matchcol != k)
            {
                module.HandleStrike();
                Audio.PlaySoundAtTransform("Hit", transform);
                Debug.LogFormat("[Multitask #{0}] Oops: {1} signal was not intercepted", moduleID, new string[] { "Red", "Orange", "Yellow", "Lime", "Green" }[k]);
                for (int i = 0; i < 15; i++)
                {
                    StopCoroutine(match[i]);
                    if (i < 5)
                    {                       
                        for (int j = 0; j < 5; j++)
                            matchleds[(5 * i) + j].material = ledcols[0];
                    }
                    if(i < 10)
                         active[3][i] = false;
                    match[i] = Match(i);
                }
                if (!final.Contains(true))
                {
                    StopCoroutine(tasks[3]);
                    StopCoroutine(countdowns[4]);
                    timers[4].text = string.Empty;
                    StartCoroutine(HatchMove(4, false));
                }
            }
            else
            {
                Audio.PlaySoundAtTransform("Jam" + (k + 1).ToString(), transform);
            }
        }
    }

    private void MatchPress(int i)
    {
        if (active[0][3])
        {
            matchbuttons[i].AddInteractionPunch(0.75f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            matchcol = i;
            matchbar[0].material = ledcols[i + 1];
        }
    }

    private IEnumerator DoneSoSoon()
    {
        yield return new WaitForSeconds(5);
        StartCoroutine(HatchMove(0, false));
        yield return new WaitForSeconds(2);
        timers[0].text = "OH";
        timers[0].color = new Color32(0, 255, 0, 255);
        module.HandlePass();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} left/right> to move the Pressure Gauge in that direction. Use <!{0} up/down 1-4> to move the Avoidance pawn that many spaces in that direction. Use <!{0} press A1 B2 C3 D4 E5> to press those cells in the Selection Grid. Use <!{0} press 1-5> to press that Signal Jammer button from left to right. On TP, all animations are 3 times slower and all timers last twice as long.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] coords = { "A1", "B1", "C1", "D1", "E1", "A2", "B2", "C2", "D2", "E2", "A3", "B3", "C3", "D3", "E3", "A4", "B4", "C4", "D4", "E4", "A5", "B5", "C5", "D5", "E5" };
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (command == "LEFT" || command == "L")
        {
            if (!active[0].Any(x => x))
                yield break;
            yield return null;
            needlebuttons[0].OnInteract();
            while (true)
            {
                if (needleangle <= 0 || disablingNeedle)
                {
                    needlebuttons[0].OnInteractEnded();
                    needleup = 0;
                    yield   break; 
                }
                yield return null;
            }
        }
        else if (command == "RIGHT" || command == "R")
        {
            if (!active[0].Any(x => x))
                yield break;
            yield return null;
            needlebuttons[1].OnInteract();
            while (true)
            {
                if (needleangle >= 0 || disablingNeedle)
                {
                    needlebuttons[1].OnInteractEnded();
                    needleup = 0;
                    yield break;
                }
                yield return null;
            }
        }
        else if (Regex.IsMatch(command, @"^(U(?:P)?|D(?:OWN)?)\s+[1-4]$"))
        {
            yield return null;
            for (int i = 0; i < command.Last() - '0'; i++)
            {
                arrowbuttons[command.First() == 'U' ? 0 : 1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (Regex.IsMatch(command, @"^(?:P(?:RESS)?\s+)?(\s*[A-E][1-5])+$"))
        {
            yield return null;
            foreach (string coord in parameters.Where(x => coords.Contains(x)))
            {
                if (!active[2].Any(x => x))
                    yield break;
                gridbuttons[Array.IndexOf(coords, coord)].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (Regex.IsMatch(command, @"^(?:P(?:RESS)?\s+)?[1-5]$"))
        {
            yield return null;
            matchbuttons[command.Last() - '1'].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void TwitchHandleForcedSolve()
    { //If we set the return of the autosolve method to void, it'll get called instantly upon running !solvebomb. This will avoid multitask accumulating strikes if solvebomb is ran without.
        speedMultiplier = 3; //Just for funsies. Sets the timer to triple speed for the autosolver
        speedInverse = 0.333f;
        StartCoroutine(BeginAutosolve());
    }
    IEnumerator BeginAutosolve()
    {
        autosolving = true;
        StartCoroutine(CompletePressure());
        StartCoroutine(CompleteAvoidance());
        StartCoroutine(CompleteGrid());
        StartCoroutine(CompleteMatch());
        while (!moduleSolved)
            yield return true;
    }
    IEnumerator CompletePressure()
    {
        while (!moduleSolved)
        {
            while (!active[0].Any(x => x))
                yield return null;
            if (needleangle > 30)
            {
                needlebuttons[0].OnInteract();
                while (needleangle > 0)
                    yield return null;
                needlebuttons[0].OnInteractEnded();
                needleup = 0;
            }
            else if (needleangle < -30)
            {
                needlebuttons[1].OnInteract();
                while (needleangle < 0)
                    yield return null;
                needlebuttons[1].OnInteractEnded();
                needleup = 0;
            }
            yield return null;
        }
    }
    IEnumerator CompleteAvoidance()
    {
        while (!moduleSolved)
        {
            while (!active[1].Any(x => x))
                yield return null;
            int[] arrowPositions = Enumerable.Repeat(0, 10).ToArray();
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 5; j++)  //Goes through each row of LEDs and determines how far away the signals are from the led.
                    arrowPositions[i] += Array.IndexOf(ledcols.Select(x => x.name).ToArray(), arrowleds[5 * i + j].material.name.TakeWhile(x => x != ' ').Join(""));
            for (int i = 0; i < 10; i++)
                if (arrowPositions[i] == 0)
                    arrowPositions[i] = 6; //Empty spaces are always the highest priority. 
            int[] priorities = Enumerable.Range(0, 5).Select(x => Math.Min(arrowPositions[x], arrowPositions[x + 5])).ToArray(); //If a row has two arrows on it, use the closer arrow.
            int targetPos = Array.IndexOf(priorities, priorities.Max());
            while (arrowled != targetPos)
            {
                if (priorities[arrowled + (arrowled > targetPos ? -1 : 1)] == 1 && priorities[arrowled] != 1) //Do not try to move into a space which has a red arrow. This might strike.
                    break;
                yield return new WaitForSeconds(0.1f);
                arrowbuttons[arrowled > targetPos ? 0 : 1].OnInteract();
            }
            yield return new WaitForSeconds(0.1f); //Hefty calculation, so let's not do it every frame.
        }
    }
    IEnumerator CompleteGrid()
    {
        while (!moduleSolved)
        {
            while (!active[2].Any(x => x))
                yield return null;
            yield return new WaitForSeconds(0.5f);
            List<int> litPositions = new List<int>();
            for (int i = 0; i < 25; i++)
                if (active[2][i])
                    litPositions.Add(i);
            if (litPositions.Count != 0)
                gridbuttons[litPositions.PickRandom()].OnInteract();
        }
    }
    IEnumerator CompleteMatch()
    {
        while (!moduleSolved)
        {
            while (!active[0].Any(x => x))
                yield return null;
            int[] ledPositions = Enumerable.Repeat(-1, 5).ToArray();
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++) //For each column, go through the row and if there's an LED lit, set the column'th position of an array to its closeness value.
                    if (matchleds[5 * i + j].material.name != "K (Instance)")
                        ledPositions[i] = j;
            int highestPriority = Enumerable.Range(0, 5).OrderBy(x => ledPositions[x]).Last();
            yield return new WaitForSeconds(0.1f);
            if (matchcol != highestPriority)
                matchbuttons[highestPriority].OnInteract(); //Press the button whose signal is the closest. The closest (maximum) value goes to the end of the orderby.
        }
    }

}
