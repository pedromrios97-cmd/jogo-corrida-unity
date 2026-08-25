using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LOanding : MonoBehaviour
{
    public TextMeshProUGUI txt;

    private void Start()
    {
        CarregarNivel("game");
    }

    public void CarregarNivel(string nomeDaCena)
    {
        StartCoroutine(CarregarEmSegundoPlano(nomeDaCena));
    }

    IEnumerator CarregarEmSegundoPlano(string nomeDaCena)
    {
        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeDaCena);

        // O Unity carrega até 90% e deixa os últimos 10% para ativar a cena
        while (!operacao.isDone)
        {
            // progress vai de 0 a 0.9
            float progresso = Mathf.Clamp01(operacao.progress / 0.9f);

            // Multiplica por 100 para ter o valor em porcentagem
            float porcentagem = progresso * 100f;

            Debug.Log($"Carregando: {porcentagem:F0}%");
            txt.text = $"Carregando {porcentagem:F0}%";

            yield return null;
        }
    }
}
