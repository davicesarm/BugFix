using UnityEngine;
using Vuforia; // Importante para acessar classes do Vuforia
using System.Collections.Generic;

// para herdar de DefaultObserverEventHandler
public class DetectCards : DefaultObserverEventHandler
{
    // Variáveis para os Prefabs de imagens/modelos 3D
    public GameObject imagePrefab1;
    public GameObject imagePrefab2;
    // ... adicionar mais conforme necessário

    private GameObject currentDisplayedContent; // Para controlar o que está sendo exibido

    // Dicionário para mapear IDs de VuMark para seus Prefabs
    private Dictionary<string, GameObject> vumarkContentMap;

    protected override void Start()
    {
        base.Start(); // Chamar o Start do pai (DefaultObserverEventHandler)

        vumarkContentMap = new Dictionary<string, GameObject>();
        // Exemplo de mapeamento (faria isso para todas as suas cartas)
        vumarkContentMap.Add("1", imagePrefab1);
        vumarkContentMap.Add("2", imagePrefab2);
        // ... todos os seus mapeamentos aqui
    }

    // Este método é chamado quando o VuMark é detectado (ou recupera o rastreamento)
    // Ele substitui a lógica que estaria em OnVuMarkDetected
    protected override void OnTrackingFound()
    {
        // base.OnTrackingFound(); // Chame o método pai

        VuMarkBehaviour vumarkBehaviour = GetComponent<VuMarkBehaviour>();

        if (vumarkBehaviour != null)
        {
            string vumarkId = vumarkBehaviour.InstanceId.NumericValue.ToString(); // Obtém o ID do VuMark como string
            Debug.Log("VuMark detectado com ID: " + vumarkId);

            // Se já tem conteúdo exibido, destrua-o antes de exibir o novo
            if (currentDisplayedContent != null)
            {
                Destroy(currentDisplayedContent);
            }

            // Verifica se o ID do VuMark está no seu dicionário
            if (vumarkContentMap.ContainsKey(vumarkId))
            {
                GameObject contentToDisplay = vumarkContentMap[vumarkId];

                // Instancia o conteúdo como filho do VuMark, para que ele siga o marcador
                // O 'transform' aqui é o transform do GameObject onde este script está (o próprio VuMark)
                currentDisplayedContent = Instantiate(contentToDisplay, transform);
                // currentDisplayedContent.transform.localPosition = Vector3.zero;
                // currentDisplayedContent.transform.localRotation = Quaternion.identity;
                // currentDisplayedContent.transform.localScale = Vector3.one;
                currentDisplayedContent.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // 1/5 = 0.2
            }
            else
            {
                Debug.LogWarning("ID de VuMark não mapeado: " + vumarkId);
            }
        }
        else
        {
            Debug.LogError("VuMarkBehaviour não encontrado no GameObject detectado.");
        }
    }

    // Este método é chamado quando o VuMark é perdido (sai do campo de visão)
    // Ele substitui a lógica que estaria em OnVuMarkLost
    protected override void OnTrackingLost()
    {
        base.OnTrackingLost(); // Chame o método pai

        Debug.Log("VuMark perdido.");
        if (currentDisplayedContent != null)
        {
            Destroy(currentDisplayedContent); // Destrói o conteúdo quando o VuMark é perdido
            currentDisplayedContent = null;
        }
    }
}