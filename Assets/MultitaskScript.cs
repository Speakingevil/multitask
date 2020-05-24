using System;
using System.Collections;
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
    private static bool finalchoice;
    private bool final;

    private static int moduleIDCounter;
    private int moduleID;

    void Awake()
    {
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
    }

    private void Activate()
    {
        finalchoice = false;
        exempt = GetComponent<KMBossModule>().GetIgnoredModules("Multitask", new string[]
        {
            "Forget Me Not",
            "Forget Everything",
            "Forget This",
            "Forget Infinity",
            "Forget Them All",
            "Simon's Stages",
            "Turn The Key",
            "The Time Keeper",
            "Bamboozling Time Keeper",
            "Timing is Everything",
            "Purgatory",
            "Hogwarts",
            "Souvenir",
            "The Troll",
            "Tallordered Keys",
            "Forget Enigma",
            "Forget Us Not",
            "Organization",
            "Forget Perspective",
            "The Very Annoying Button",
            "Forget Me Later",
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
    }

    private IEnumerator Simul()
    {
        while (!final)
        {
            while (simul < Mathf.CeilToInt(4f * bomb.GetSolvedModuleNames().Count() / modnum))
                simul++;
            if (simul > 3)
            {
                final = true;
                StopAllCoroutines();
                StartCoroutine(Manager());
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator Manager()
    {
        while (!final)
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
                yield return new WaitForSeconds(Random.Range(5f, 15f));
                StartCoroutine(HatchMove(task + 1, true));
                yield return new WaitForSeconds(1);
                int timeset = Random.Range(20, 50);
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
        yield return new WaitForSeconds(Random.Range(2f, 12f));
        while (finalchoice)
            yield return new WaitForSeconds(Random.Range(5f, 10f));
        finalchoice = true;
        Debug.LogFormat("[Multitask #{0}] Final phase activated at {1}", moduleID, bomb.GetFormattedTime());
        Audio.PlaySoundAtTransform("HatchOpen", transform);
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(HatchMove(i + 1, true));
            active[0][i] = true;
            StartCoroutine(tasks[i]);
            countdowns[i] = Timer(i, 70);
            StartCoroutine(countdowns[i]);
        }
        while (hatchmove.Contains(true))
            yield return null;
        for (int i = 70; i > -1; i--)
        {
            timers[0].text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        Audio.PlaySoundAtTransform("InputCorrect", transform);
        Debug.LogFormat("[Multitask #{0}] Final phase deactivated: Module solved", moduleID);
        StartCoroutine(HatchMove(0, false));
        timers[0].text = "GG";
        timers[0].color = new Color32(0, 255, 0, 255);
        finalchoice = false;
        module.HandlePass();
    }

    private IEnumerator HatchMove(int hatch, bool up)
    {
        if (hatch > 0)
            hatchmove[hatch - 1] = true;
        if (up)
        {
            if(moduleID + 1 == moduleIDCounter && !final)
                 Audio.PlaySoundAtTransform("HatchOpen", transform);
            switch (hatch)
            {
                case 1:
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
        if (!up)
        {
            switch (hatch)
            {
                case 1:
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
                    foreach (GameObject obj in needleobj)
                        obj.SetActive(false);
                    foreach (GameObject obj in arrowobj)
                        obj.SetActive(false);
                    foreach (GameObject obj in gridobj)
                        obj.SetActive(false);
                    foreach (GameObject obj in matchobj)
                        obj.SetActive(false);
                    Audio.PlaySoundAtTransform("Slam", transform);
                    break;
            }
            if (hatch > 0)
            {
                active[0][hatch - 1] = false;
            }
        }
        if (hatch > 0)
            hatchmove[hatch - 1] = false;
    }

    private IEnumerator Task(int t)
    {
        switch (t)
        {
            case 1:
                while (active[0][1])
                {
                    while (hatchmove[1] || active[1].Where(a => a).Count() > (final ? 6 : 3))
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
                    while (hatchmove[2])
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
                    yield return new WaitForSeconds(matchrand[0] == 0 ? (final ? 2.4f : 3.2f) : final ? 1.2f : 1.6f);
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
            if (!final)
                timers[t + 1].text = i < 10 ? "0" + i.ToString() : i.ToString();
            yield return new WaitForSeconds(1);
        }      
        timers[t + 1].text = string.Empty;
        StopCoroutine(tasks[t]);
        active[0][t] = false;
        if (!final)
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
                        needleangle -= 0.9f;
                    else if (needleangle < -30)
                        needleangle -= 0.6f;
                    else if (needleangle < 0)
                        needleangle -= 0.3f;
                    else if (needleangle > 60)
                        needleangle += 0.9f;
                    else if (needleangle > 30)
                        needleangle += 0.6f;
                    else
                        needleangle += 0.3f;
                    break;
            }
            if (Mathf.Abs(needleangle) > 90)
            {
                module.HandleStrike();
                Debug.LogFormat("[Multitask #{0}] Oops: Pressure too {1}", moduleID, needleangle > 0 ? "high" : "low");
                if (!final)
                {
                    timers[1].text = string.Empty;
                    StopCoroutine(countdowns[1]);
                    StartCoroutine(HatchMove(1, false));
                    yield break;
                }
                else
                    needlereset = true;
            }
            yield return new WaitForSeconds(final ? 0.03f : 0.04f);
        }
    }

    private IEnumerator Arrow(int k)
    {
        Audio.PlaySoundAtTransform("Sharp", transform);
        float waittime = Random.Range(1.5f, 2.5f);
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
            if (!final)
            {
                StopCoroutine(tasks[1]);
                StopCoroutine(countdowns[2]);
                timers[2].text = string.Empty;
                active[0][1] = false;                
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
        float waittime = final ? 2 : 2.4f;
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
        if (!final)
        {
            StopCoroutine(tasks[2]);
            StopCoroutine(countdowns[3]);
            timers[3].text = string.Empty;
            active[0][2] = false;          
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
                if (!final)
                {
                    StopCoroutine(tasks[2]);
                    StopCoroutine(countdowns[3]);
                    timers[3].text = string.Empty;
                    active[0][2] = false;                  
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
            yield return new WaitForSeconds(final ? 1.2f : 1.6f);
        }
        matchleds[(5 * k) + 4].material = ledcols[0];
        matchbar[1].material = ledcols[k + 1];
        yield return new WaitForSeconds(0.1f);
        matchbar[1].material = ledcols[0];
        if(d < 10)
            active[3][(5 * m) + k] = false;
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
                if (!final)
                {
                    StopCoroutine(tasks[3]);
                    StopCoroutine(countdowns[4]);
                    timers[4].text = string.Empty;
                    active[0][3] = false;
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
        yield return new WaitForSeconds(Random.Range(2f, 12f));
        while (finalchoice)
            yield return new WaitForSeconds(Random.Range(5f, 10f));
        finalchoice = true;
        StartCoroutine(HatchMove(0, false));
        yield return new WaitForSeconds(2);
        finalchoice = false;
        timers[0].text = "OH";
        timers[0].color = new Color32(0, 255, 0, 255);
        module.HandlePass();
    }
}
