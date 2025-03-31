using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MatrixVisualizer : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject modelMatrixPrefab;
    [SerializeField] private GameObject spaceMatrixPrefab;
    [SerializeField] private GameObject matchedMatrixPrefab;
    
    [Header("Visualization Settings")]
    [SerializeField] private float visualScale = 0.5f;
    [SerializeField] private float animationSpeed = 1.0f;
    [SerializeField] private int maxVisibleSpaceMatrices = 1000; 
    [SerializeField] private bool showSpaceMatrices = true;
    [SerializeField] private Color modelColor = new Color(0.2f, 0.4f, 1.0f, 0.8f);
    [SerializeField] private Color spaceColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
    [SerializeField] private Color matchColor = new Color(0.0f, 1.0f, 0.0f, 0.8f);
  
    private Dictionary<Vector3Int, GameObject> modelObjects = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> spaceObjects = new Dictionary<Vector3Int, GameObject>();
    private List<GameObject> matchObjects = new List<GameObject>();

    [SerializeField] private float cameraOrbitSpeed = 10f;
    [SerializeField] private float cameraDistance = 50f;
    private Transform cameraTransform;
    
    private void Start()
    {
        cameraTransform = Camera.main.transform;
        SetupInitialCameraPosition();
    }
    
    private void Update()
    {
      
        if (cameraTransform != null)
        {
            cameraTransform.RotateAround(Vector3.zero, Vector3.up, cameraOrbitSpeed * Time.deltaTime);
        }
    }
    
    private void SetupInitialCameraPosition()
    {
        if (cameraTransform != null)
        {
            cameraTransform.position = new Vector3(0, 10, -cameraDistance);
            cameraTransform.LookAt(Vector3.zero);
        }
    }
    

    public void VisualizeModelMatrices(List<Matrix4x4> matrices)
    {
        ClearModelObjects();
        
        int count = 0;
        foreach (var matrix in matrices)
        {
            Vector3Int position = ExtractPositionFromMatrix(matrix);
            GameObject obj = CreateMatrixObject(modelMatrixPrefab, position, modelColor);
            modelObjects[position] = obj;
            count++;
        }
        
        Debug.Log($"Visualized {count} model matrices");
    }
    

    public void VisualizeSpaceMatrices(List<Matrix4x4> matrices)
    {
        if (!showSpaceMatrices)
        {
            return;
        }
        
        ClearSpaceObjects();

        int maxToShow = Mathf.Min(maxVisibleSpaceMatrices, matrices.Count);
        int skip = matrices.Count / maxToShow;
        
        int count = 0;
        for (int i = 0; i < matrices.Count; i += skip)
        {
            if (count >= maxToShow) break;
            
            Vector3Int position = ExtractPositionFromMatrix(matrices[i]);
            GameObject obj = CreateMatrixObject(spaceMatrixPrefab, position, spaceColor);
            spaceObjects[position] = obj;
            count++;
        }
        
        Debug.Log($"Visualized {count} of {matrices.Count} space matrices");
    }
    

    public void VisualizeMatch(List<Vector3Int> modelPositions, Vector3Int offset)
    {
        foreach (Vector3Int modelPos in modelPositions)
        {
            Vector3Int offsetPos = modelPos + offset;
            GameObject obj = CreateMatrixObject(matchedMatrixPrefab, offsetPos, matchColor);
            

            StartCoroutine(AnimateMatchEffect(obj));
            
            matchObjects.Add(obj);
        }
    }
    

    public void ClearAllVisualizations()
    {
        ClearModelObjects();
        ClearSpaceObjects();
        ClearMatchObjects();
    }
    

    public void ClearMatchObjects()
    {
        foreach (GameObject obj in matchObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        matchObjects.Clear();
    }
    
    private void ClearModelObjects()
    {
        foreach (GameObject obj in modelObjects.Values)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        modelObjects.Clear();
    }
    
    private void ClearSpaceObjects()
    {
        foreach (GameObject obj in spaceObjects.Values)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        spaceObjects.Clear();
    }
    
    private GameObject CreateMatrixObject(GameObject prefab, Vector3Int position, Color color)
    {
        Vector3 worldPos = new Vector3(position.x, position.y, position.z) * visualScale;
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propBlock);
        }
        
        return obj;
    }

    private IEnumerator AnimateMatchEffect(GameObject obj)
    {
        if (obj == null) yield break; 

        float duration = 1.0f / animationSpeed;
        float maxScale = 1.5f;

        Vector3 originalScale = obj.transform.localScale;
        Vector3 targetScale = originalScale * maxScale;

        float elapsed = 0;

        while (elapsed < duration / 2)
        {
            if (obj == null) yield break; 
            obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / (duration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }


        elapsed = 0;
        while (elapsed < duration / 2)
        {
            if (obj == null) yield break; 
            obj.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / (duration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null)
            obj.transform.localScale = originalScale;
    }


    private Vector3Int ExtractPositionFromMatrix(Matrix4x4 matrix)
    {
        return new Vector3Int(
            Mathf.RoundToInt(matrix.m03),
            Mathf.RoundToInt(matrix.m13),
            Mathf.RoundToInt(matrix.m23)
        );
    }
}
