using UnityEngine;
using UnityEngine.UI;

public class ActivateMenu : MonoBehaviour
{
    public Button button;
    public GameObject thisMenu, otherMenu;
    public void Click()
    {
        if (thisMenu.activeSelf == false)
        {
            otherMenu.SetActive(false);
            thisMenu.SetActive(true);
        } else
        {
            thisMenu.SetActive(false);
        }
        
    }
}
