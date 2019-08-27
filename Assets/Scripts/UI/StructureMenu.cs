using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StructureMenu : MonoBehaviour
{
    public GameObject buildStructureButtonPrefab;

    // Start is called before the first frame update
    void Start()
    {
        BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();
        // For each furniture prototype in our world, create one instance
        // of the button to be clicked!

        foreach (string s in World.current.structurePrototypes.Keys)
        {
            GameObject go = Instantiate(buildStructureButtonPrefab);
            go.transform.SetParent(this.transform);

            go.name = "Button - Build " + s;
            go.transform.GetComponentInChildren<Text>().text = "Build " + s; // TODO: Locale

            Button b = go.GetComponent<Button>();
            string objectId = s;
            b.onClick.AddListener(delegate { bmc.SetMode_Structure(); bmc.SetBuildStructure(objectId); });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
