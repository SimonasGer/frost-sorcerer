using UnityEngine;
using UnityEngine.UI;

public class Quit : MonoBehaviour
{
    public Button quit;
    public void Click()
    {
        Application.Quit();
    }
}
