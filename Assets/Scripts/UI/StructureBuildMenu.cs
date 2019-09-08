using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StructureBuildMenu : MonoBehaviour
{
    public GameObject buildStructureButtonPrefab;

    // Start is called before the first frame update
    void Start()
    {
        BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();
        // For each furniture prototype in our world, create one instance
        // of the button to be clicked!

        foreach (string s in PrototypeManager.Structure.Keys)
        {
            GameObject go = Instantiate(buildStructureButtonPrefab);
            go.transform.SetParent(this.transform);

            string objectName = StringUtils.GetLocalizedTextFiltered("comment#" + PrototypeManager.Structure.Get(s).Name);
            string objectId = s;

            go.name = "Button - Build " + objectId;
            go.transform.GetComponentInChildren<Text>().text = "Build " + objectName; // TODO: Locale

            Button b = go.GetComponent<Button>();
            b.onClick.AddListener(delegate { bmc.SetMode_Structure(); bmc.SetBuildStructure(objectId); });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
