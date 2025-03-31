using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class MatrixMatcher : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TextAsset modelJsonFile;
    [SerializeField] private TextAsset spaceJsonFile;
    [SerializeField] private string outputJsonPath = "Assets/FoundOffsets.json";
    
    [Header("Visualization")]
    [SerializeField] private GameObject modelMatrixPrefab;
    [SerializeField] private GameObject spaceMatrixPrefab;
    [SerializeField] private GameObject matchedMatrixPrefab;
    [SerializeField] private float visualizationScale = 0.1f;
    [SerializeField] private float visualizationSpeed = 1.0f;
    

    private List<Matrix4x4> modelMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> spaceMatrices = new List<Matrix4x4>();
    private HashSet<Vector3Int> spaceMatrixPositions = new HashSet<Vector3Int>();
    private List<Vector3Int> foundOffsets = new List<Vector3Int>();
    

    private Dictionary<Vector3Int, GameObject> visualObjects = new Dictionary<Vector3Int, GameObject>();
    private bool isSearching = false;

    [SerializeField] private UnityEngine.UI.Text statusText;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private UnityEngine.UI.Slider progressSlider;

    private void Start()
    {
        LoadMatricesFromJson();
        SetupUI();
    }

    private void SetupUI()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartMatrixSearch);
        }
        
        if (statusText != null)
        {
            statusText.text = $"Loaded {modelMatrices.Count} model matrices and {spaceMatrices.Count} space matrices";
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 0;
        }
    }

    private void LoadMatricesFromJson()
    {
        try
        {
          
            if (modelJsonFile != null)
            {
                MatrixData modelData = JsonConvert.DeserializeObject<MatrixData>(modelJsonFile.text);
                modelMatrices = ConvertToMatrix4x4List(modelData.matrices);
                Debug.Log($"Loaded {modelMatrices.Count} model matrices");
            }
            
          
            if (spaceJsonFile != null)
            {
                MatrixData spaceData = JsonConvert.DeserializeObject<MatrixData>(spaceJsonFile.text);
                spaceMatrices = ConvertToMatrix4x4List(spaceData.matrices);
                
            
                foreach (Matrix4x4 matrix in spaceMatrices)
                {
                    Vector3Int position = ExtractPositionFromMatrix(matrix);
                    spaceMatrixPositions.Add(position);
                }
                
                Debug.Log($"Loaded {spaceMatrices.Count} space matrices");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading matrices: {e.Message}");
        }
    }

    private List<Matrix4x4> ConvertToMatrix4x4List(float[][,] rawMatrices)
    {
        List<Matrix4x4> result = new List<Matrix4x4>();
        
        foreach (float[,] rawMatrix in rawMatrices)
        {
            Matrix4x4 matrix = new Matrix4x4();
            
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    matrix[row, col] = rawMatrix[row, col];
                }
            }
            
            result.Add(matrix);
        }
        
        return result;
    }

    private Vector3Int ExtractPositionFromMatrix(Matrix4x4 matrix)
    {
      
        return new Vector3Int(
            Mathf.RoundToInt(matrix.m03),
            Mathf.RoundToInt(matrix.m13),
            Mathf.RoundToInt(matrix.m23)
        );
    }

    public void StartMatrixSearch()
    {
        if (!isSearching)
        {
            isSearching = true;
            StartCoroutine(FindMatrixOffsets());
        }
    }

    private IEnumerator FindMatrixOffsets()
    {
        if (statusText != null)
        {
            statusText.text = "Searching for valid offsets...";
        }
        
   
        foundOffsets.Clear();
        ClearVisualization();
        
      
        List<Vector3Int> modelPositions = modelMatrices.Select(ExtractPositionFromMatrix).ToList();
        
 
        Vector3Int minModelPos = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxModelPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        
        foreach (Vector3Int pos in modelPositions)
        {
            minModelPos.x = Mathf.Min(minModelPos.x, pos.x);
            minModelPos.y = Mathf.Min(minModelPos.y, pos.y);
            minModelPos.z = Mathf.Min(minModelPos.z, pos.z);
            
            maxModelPos.x = Mathf.Max(maxModelPos.x, pos.x);
            maxModelPos.y = Mathf.Max(maxModelPos.y, pos.y);
            maxModelPos.z = Mathf.Max(maxModelPos.z, pos.z);
        }

        Vector3Int modelSize = maxModelPos - minModelPos;
        

        int searchRange = 100; 
        
        int totalOffsets = (searchRange * 2) * (searchRange * 2) * (searchRange * 2);
        int processedOffsets = 0;
        
  
        VisualizeMatrices(modelPositions, modelMatrixPrefab, Color.blue);
        
  
        for (int x = -searchRange; x <= searchRange; x++)
        {
            for (int y = -searchRange; y <= searchRange; y++)
            {
                for (int z = -searchRange; z <= searchRange; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    bool isValidOffset = true;
                    
                
                    foreach (Vector3Int modelPos in modelPositions)
                    {
                        Vector3Int offsetPos = modelPos + offset;
                        
                        if (!spaceMatrixPositions.Contains(offsetPos))
                        {
                            isValidOffset = false;
                            break;
                        }
                    }
                    
                    if (isValidOffset)
                    {
                        foundOffsets.Add(offset);
                        Debug.Log($"Found valid offset: {offset}");
                        
                   
                        VisualizeMatch(modelPositions, offset);
                        
                
                        yield return new WaitForSeconds(0.1f);
                    }
                    
                    processedOffsets++;
                    
                 
                    if (progressSlider != null && totalOffsets > 0)
                    {
                        progressSlider.value = (float)processedOffsets / totalOffsets;
                    }
                    
               
                    if (processedOffsets % 1000 == 0)
                    {
                        if (statusText != null)
                        {
                            statusText.text = $"Searching... {processedOffsets}/{totalOffsets} offsets checked. Found: {foundOffsets.Count}";
                        }
                        yield return null;
                    }
                }
            }
        }
        

        ExportResultsToJson();
        
        if (statusText != null)
        {
            statusText.text = $"Search complete! Found {foundOffsets.Count} valid offsets.";
        }
        
        isSearching = false;
    }

    private void VisualizeMatrices(List<Vector3Int> positions, GameObject prefab, Color color)
    {
        foreach (Vector3Int pos in positions)
        {
            GameObject obj = Instantiate(prefab, new Vector3(pos.x, pos.y, pos.z) * visualizationScale, Quaternion.identity);
            Renderer renderer = obj.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            
            visualObjects[pos] = obj;
        }
    }

    private void VisualizeMatch(List<Vector3Int> modelPositions, Vector3Int offset)
    {
        foreach (Vector3Int modelPos in modelPositions)
        {
            Vector3Int offsetPos = modelPos + offset;
            GameObject obj = Instantiate(matchedMatrixPrefab, new Vector3(offsetPos.x, offsetPos.y, offsetPos.z) * visualizationScale, Quaternion.identity);
            
    
            StartCoroutine(PulseObjectScale(obj, 1.0f, 1.5f, 0.5f));
        }
    }

    private IEnumerator PulseObjectScale(GameObject obj, float minScale, float maxScale, float duration)
    {
        float elapsed = 0;
        Vector3 originalScale = obj.transform.localScale;
        
        while (elapsed < duration)
        {
            float scale = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(elapsed / duration * 2, 1));
            obj.transform.localScale = originalScale * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        obj.transform.localScale = originalScale;
        

        Destroy(obj, 2.0f);
    }

    private void ClearVisualization()
    {
        foreach (GameObject obj in visualObjects.Values)
        {
            Destroy(obj);
        }
        
        visualObjects.Clear();
    }

    private void ExportResultsToJson()
    {
        OffsetResults results = new OffsetResults
        {
            offsets = foundOffsets.Select(v => new Vector3Serializable { x = v.x, y = v.y, z = v.z }).ToArray(),
            count = foundOffsets.Count,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        string json = JsonConvert.SerializeObject(results, Formatting.Indented);
        File.WriteAllText(outputJsonPath, json);
        
        Debug.Log($"Exported {foundOffsets.Count} offsets to {outputJsonPath}");
    }
}


[Serializable]
public class MatrixData
{
    public float[][,] matrices;
}

[Serializable]
public class OffsetResults
{
    public Vector3Serializable[] offsets;
    public int count;
    public string timestamp;
}

[Serializable]
public class Vector3Serializable
{
    public int x;
    public int y;
    public int z;
}
