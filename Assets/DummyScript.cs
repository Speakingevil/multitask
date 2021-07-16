using UnityEngine;

public class DummyScript : MonoBehaviour {

    void Start()
    {
        GetComponent<KMSelectable>().OnInteract += delegate () { GetComponent<KMBombModule>().HandlePass(); return false; };
    }
}
