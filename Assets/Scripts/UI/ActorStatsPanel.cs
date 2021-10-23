using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActorStatsPanel : MonoBehaviour
{
    public float offScreenPosX = 300;
    public float onScreenPosX = 0;

    public float appearAnimTime = 0.5f;
    private float appearAnimTimer = 0f;
    private bool animatePanel = false;

    Text myText;
    MouseController mouseController;
    SelectionInfo mySelection;

    Actor selectedActor = null;

    bool actorSelected = false;

    RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        myText = GetComponentInChildren<Text>();
        mouseController = GameObject.FindObjectOfType<MouseController>();

        rectTransform = GetComponent<RectTransform>();

        if (myText == null)
        {
            Debug.LogError("ActorStats: No 'Text' UI component on this object.");
            this.enabled = false;
            return;
        }

        if (mouseController == null)
        {
            Debug.LogError("ActorStats: No 'MouseController' object in scene.");
            this.enabled = false;
            return;
        }

        if (rectTransform == null)
        {
            Debug.LogError("ActorStats: No 'RectTransform' UI component on this object.");
            this.enabled = false;
            return;
        }

        rectTransform.anchoredPosition = new Vector3(offScreenPosX, rectTransform.anchoredPosition.y, 0);
        myText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        bool prevActorSelected = actorSelected;
        

        if (myText != null && mouseController != null)
        {
            

            Actor prevSelectedActor = null;
            if (selectedActor != null)
                prevSelectedActor = selectedActor;

            if (mouseController.mySelection != null)
            {
                mySelection = mouseController.mySelection;

                actorSelected = mySelection.IsActorSelected();
                if (actorSelected && mySelection.GetSelectedStuff() != null)
                {
                    selectedActor = mySelection.GetSelectedStuff() as Actor;

                    if (prevSelectedActor != selectedActor)
                    {
                        string strText = "";

                        strText += selectedActor.GetName() + "\n";
                        
                        string strGender = selectedActor.IsFemale ? "comment#actor_gender_female" : "comment#actor_gender_male";
                        strText += StringUtils.GetLocalizedTextFiltered(strGender) + " - ";
                        strText += StringUtils.GetLocalizedTextFiltered(selectedActor.Race.Name) + "\n\n";

                        foreach (Stat stat in selectedActor.Stats.Values)
                        {
                            strText += StringUtils.GetLocalizedTextFiltered(stat.Name) + ": " + stat.Value + "\n";
                        }

                        myText.text = strText;
                    }
                }
            }
            else
            {
                actorSelected = false;
            }
        }

        if (actorSelected != prevActorSelected)
        {
            appearAnimTimer = 0;
            animatePanel = true;
            Debug.Log("Animate Panel!");
        }

        if (animatePanel)
        {
            float x;
            appearAnimTimer += Time.deltaTime;

            if (actorSelected)
            {
                x = Mathf.Lerp(offScreenPosX, onScreenPosX, appearAnimTimer / appearAnimTime);
            }
            else
            {
                x = Mathf.Lerp(onScreenPosX, offScreenPosX, appearAnimTimer / appearAnimTime);
            }

            float y = rectTransform.anchoredPosition.y;
            rectTransform.anchoredPosition = new Vector3(x, y, 0);

            if (appearAnimTimer > appearAnimTime)
            {
                animatePanel = false;
                appearAnimTimer = 0;
            }
        }
    }
}
