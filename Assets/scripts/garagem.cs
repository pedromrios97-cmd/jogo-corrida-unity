using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class garagem : MonoBehaviour
{
    public TextMeshProUGUI garageText;
    public bool isPlayerInGarage;
    public player player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            garageText.gameObject.SetActive(true);
            isPlayerInGarage = true;
        }

        if (other.CompareTag("Car"))
        {
            if (other.GetComponent<Car_Controller>().player == null)
            {
                return;
            }
            garageText.gameObject.SetActive(true);
            isPlayerInGarage = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            garageText.gameObject.SetActive(false);
            isPlayerInGarage = false;
        }

        if (other.CompareTag("Car"))
        {
            if (other.GetComponent<Car_Controller>().player == null)
            {
                return;
            }
            garageText.gameObject.SetActive(false);
            isPlayerInGarage = false;
        }
    }

    private void Update()
    {
        if (!isPlayerInGarage)
        {
            return;
        }
        else if(player.actions.Player.interact.IsPressed())
        {
            Time.timeScale = 0f;
            SceneManager.LoadScene("Menu de pause");
        }
    }
}
