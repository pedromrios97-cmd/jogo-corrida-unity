using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LOading : MonoBehaviour
{
    public TextMeshProUGUI txt;

    private void Start()
    {
        CarregarNivel("Game");
    }

    public void CarregarNivel(string nomeDaCena)
    {
        StartCoroutine(CarregarEmSegundoPlano(nomeDaCena));
    }

    IEnumerator CarregarEmSegundoPlano(string nomeDaCena)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeDaCena);
        operacao.allowSceneActivation = false;

        float progressoVisual = 0f;

        while (!operacao.isDone)
        {
            float progressoAlvo = Mathf.Clamp01(operacao.progress / 0.9f);

            while (progressoVisual < progressoAlvo)
            {
                progressoVisual = Mathf.MoveTowards(progressoVisual, progressoAlvo, Time.deltaTime * 0.5f);

                if (txt != null)
                {
                    txt.text = $"Carregando {(progressoVisual * 100f):F0}%";
                }

                yield return null;
            }

            if (operacao.progress >= 0.9f && progressoVisual >= 0.99f)
            {
                if (txt != null)
                {
                    txt.text = "Carregando 100%";
                }

                yield return new WaitForSeconds(0.2f);

                Application.backgroundLoadingPriority = ThreadPriority.Normal;

                operacao.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}