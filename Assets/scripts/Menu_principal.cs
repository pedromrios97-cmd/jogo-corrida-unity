using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu_principal : MonoBehaviour
{
    public void StarGame()
    {
        SceneManager.LoadScene("Loading");
    }

    public void Options()
    {
        Debug.Log("botao de options");
    }

    public void ExitGame()
    {
        Debug.Log("botao de sair");
        Application.Quit(); 
    }
}
