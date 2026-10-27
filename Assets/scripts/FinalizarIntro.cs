
using UnityEngine;
using System.Collections;

public class FinalizarIntro : MonoBehaviour
{
    [Header("Câmera normal")]
    [SerializeField] private GameObject cameraNormal;

    [Header("Controle do carro")]
    [SerializeField] private Behaviour controleCarro;

    [Header("Tempo da cutscene")]
    [SerializeField] private float duracaoCutscene = 7f;

    private IEnumerator Start()
    {
        if (controleCarro != null)
            controleCarro.enabled = false;

        yield return new WaitForSeconds(duracaoCutscene);

        if (cameraNormal != null)
            cameraNormal.SetActive(true);

        if (controleCarro != null)
            controleCarro.enabled = true;

        gameObject.SetActive(false);
    }
}
