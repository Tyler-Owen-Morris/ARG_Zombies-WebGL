using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CraftPanelManager : MonoBehaviour {

    public GameObject[] weaponTypePanelArray, tier1_craftButtonArray, tier2_craftButtonArray, tier3_craftButtonArray;

    public void SetMeUpThePanelManager()
    {
        
        if (GameManager.instance.crafting_t1 == true)
        {
            foreach(GameObject button in tier1_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = true; 
            }
        }else
        {
            foreach (GameObject button in tier1_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = false;
            }
        }

        if (GameManager.instance.crafting_t2 == true)
        {
            foreach (GameObject button in tier2_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = true;
            }
        }
        else
        {
            foreach (GameObject button in tier2_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = false;
            }
        }

        if (GameManager.instance.crafting_t3 == true)
        {
            foreach (GameObject button in tier3_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = true;
            }
        }
        else
        {
            foreach (GameObject button in tier3_craftButtonArray)
            {
                Button my_button = button.GetComponent<Button>();
                my_button.interactable = false;
            }
        }
    }

    public void KnivesPressed()
    {
        weaponTypePanelArray[0].SetActive(true);
        weaponTypePanelArray[1].SetActive(false);
        weaponTypePanelArray[2].SetActive(false);
    }

    public void ClubsPressed()
    {
        weaponTypePanelArray[0].SetActive(false);
        weaponTypePanelArray[1].SetActive(true);
        weaponTypePanelArray[2].SetActive(false);
    }

    public void GunsPressed()
    {
        weaponTypePanelArray[0].SetActive(false);
        weaponTypePanelArray[1].SetActive(false);
        weaponTypePanelArray[2].SetActive(true);
    }

}
