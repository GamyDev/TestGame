using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;


public class MatrixFinderController : MonoBehaviour
{
    [Header("Data Files")]
    [SerializeField] private string modelFilePath = "Assets/Resources/model.json";
    [SerializeField] private string spaceFilePath = "Assets/Resources/space.json";
    [SerializeField] private string outputFilePath = "Assets/Results/found_offsets.json";
    
    [Header("Dependencies")]
    [SerializeField] private MatrixVisualizer visualizer;
    
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button exportButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text statusText;
    [SerializeField] private Text statisticsText;
    [SerializeField] private Toggle showSpaceToggle;
    [SerializeField] private Dropdown algorithmDropdown;
    

    private List<Matrix4x4> modelMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> spaceMatrices = new List<Matrix4x4>();
    private HashSet<Vector3Int> spacePositions = new HashSet<Vector3Int>();
    private List<Vector3Int> modelPositions = new List<Vector3Int>();
    private List<Vector3Int> foundOffsets = new List<Vector3Int>();

    private bool isSearching = false;
    private bool shouldStopSearch = false;
    private int totalSearchSpace = 0;
    private int searchedCount = 0;
    private System.Diagnostics.Stopwatch searchTimer = new System.Diagnostics.Stopwatch();
    

    private enum SearchAlgorithm { BruteForce, HashBased, ModelBased }
    private SearchAlgorithm currentAlgorithm = SearchAlgorithm.HashBased;
    
    private void Start()
    {
        InitializeUI();
        

        MatrixDataLoader dataLoader = FindObjectOfType<MatrixDataLoader>();
        if (dataLoader == null)
        {
            dataLoader = gameObject.AddComponent<MatrixDataLoader>();
        }
        
        dataLoader.OnModelMatricesLoaded += OnModelMatricesLoaded;
        dataLoader.OnSpaceMatricesLoaded += OnSpaceMatricesLoaded;
        dataLoader.OnError += OnLoadError;
        

        UpdateStatus("Loading matrix data...");
        dataLoader.LoadModelMatrices(modelFilePath);
        dataLoader.LoadSpaceMatrices(spaceFilePath);
    }
    
    private void InitializeUI()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartSearch);
            
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopSearch);
            stopButton.interactable = false;
        }
        
        if (exportButton != null)
        {
            exportButton.onClick.AddListener(ExportResults);
            exportButton.interactable = false;
        }
        
        if (showSpaceToggle != null)
        {
            showSpaceToggle.onValueChanged.AddListener(OnToggleSpaceVisualization);
        }
        
        if (algorithmDropdown != null)
        {
            algorithmDropdown.ClearOptions();
            algorithmDropdown.AddOptions(new List<string> { "Brute Force", "Hash Based (Faster)", "Model Based" });
            algorithmDropdown.value = 1; 
            algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
        }
        
        UpdateStatus("Initializing...");
        UpdateStatistics(0, 0, 0, 0);
    }
    
    private void OnAlgorithmChanged(int index)
    {
        currentAlgorithm = (SearchAlgorithm)index;
        Debug.Log($"Algorithm changed to: {currentAlgorithm}");
    }
    
    private void OnToggleSpaceVisualization(bool showSpace)
    {
        if (visualizer != null)
        {

            visualizer.ClearAllVisualizations();
            

            if (modelMatrices.Count > 0)
            {
                visualizer.VisualizeModelMatrices(modelMatrices);
            }
            
            if (showSpace && spaceMatrices.Count > 0)
            {
                visualizer.VisualizeSpaceMatrices(spaceMatrices);
            }
        }
    }
    
    private void OnModelMatricesLoaded(List<Matrix4x4> matrices)
    {
        modelMatrices = matrices;
        modelPositions = modelMatrices.Select(ExtractPositionFromMatrix).ToList();
        
        UpdateStatus($"Loaded {matrices.Count} model matrices");
        
        if (visualizer != null)
        {
            visualizer.VisualizeModelMatrices(matrices);
        }
        
        CheckIfReadyToSearch();
    }
    
    private void OnSpaceMatricesLoaded(List<Matrix4x4> matrices)
    {
        spaceMatrices = matrices;
        

        spacePositions = new HashSet<Vector3Int>();
        foreach (var matrix in spaceMatrices)
        {
            spacePositions.Add(ExtractPositionFromMatrix(matrix));
        }
        
        UpdateStatus($"Loaded {matrices.Count} space matrices");

        if (showSpaceToggle != null && showSpaceToggle.isOn && visualizer != null)
        {
            visualizer.VisualizeSpaceMatrices(matrices);
        }
        
        CheckIfReadyToSearch();
    }
    
    private void OnLoadError(string errorMessage)
    {
        UpdateStatus($"Error: {errorMessage}");
    }
    
    private void CheckIfReadyToSearch()
    {
        if (modelMatrices.Count > 0 && spaceMatrices.Count > 0)
        {
            if (startButton != null)
            {
                startButton.interactable = true;
            }
            
            UpdateStatus("Ready to search. Press Start to begin.");
        }
    }
    
    public void StartSearch()
    {
        if (isSearching || modelMatrices.Count == 0 || spaceMatrices.Count == 0)
        {
            return;
        }
        
        isSearching = true;
        shouldStopSearch = false;
        foundOffsets.Clear();

        if (startButton != null) startButton.interactable = false;
        if (stopButton != null) stopButton.interactable = true;
        if (exportButton != null) exportButton.interactable = false;

        searchTimer.Restart();
        
        switch (currentAlgorithm)
        {
            case SearchAlgorithm.BruteForce:
                StartCoroutine(BruteForceSearch());
                break;
            case SearchAlgorithm.HashBased:
                StartCoroutine(HashBasedSearch());
                break;
            case SearchAlgorithm.ModelBased:
                StartCoroutine(ModelBasedSearch());
                break;
        }
    }
    
    public void StopSearch()
    {
        if (!isSearching) return;
        
        shouldStopSearch = true;
        UpdateStatus("Stopping search...");
    }
    
    private void SearchCompleted()
    {
        searchTimer.Stop();
        isSearching = false;
        

        if (startButton != null) startButton.interactable = true;
        if (stopButton != null) stopButton.interactable = false;
        if (exportButton != null) exportButton.interactable = foundOffsets.Count > 0;
        
        UpdateStatus($"Search completed. Found {foundOffsets.Count} valid offsets.");
        

        if (foundOffsets.Count > 0 && visualizer != null)
        {
            visualizer.ClearMatchObjects();
            visualizer.VisualizeMatch(modelPositions, foundOffsets[0]);
        }
    }
    
    public void ExportResults()
    {
        if (foundOffsets.Count == 0) return;
        

        var dataLoader = FindObjectOfType<MatrixDataLoader>();
        if (dataLoader != null)
        {
            dataLoader.SaveOffsetsToJson(foundOffsets, outputFilePath);
            UpdateStatus($"Exported {foundOffsets.Count} offsets to {outputFilePath}");
        }
        else
        {
            UpdateStatus("Error: DataLoader not found.");
        }
    }
    
    #region Search Algorithms
    
    private IEnumerator BruteForceSearch()
    {
        UpdateStatus("Starting brute force search...");
        
   
        Vector3Int minSpace = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxSpace = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        Vector3Int minModel = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxModel = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
   
        foreach (var pos in spacePositions)
        {
            minSpace.x = Mathf.Min(minSpace.x, pos.x);
            minSpace.y = Mathf.Min(minSpace.y, pos.y);
            minSpace.z = Mathf.Min(minSpace.z, pos.z);
            
            maxSpace.x = Mathf.Max(maxSpace.x, pos.x);
            maxSpace.y = Mathf.Max(maxSpace.y, pos.y);
            maxSpace.z = Mathf.Max(maxSpace.z, pos.z);
        }
        

        foreach (var pos in modelPositions)
        {
            minModel.x = Mathf.Min(minModel.x, pos.x);
            minModel.y = Mathf.Min(minModel.y, pos.y);
            minModel.z = Mathf.Min(minModel.z, pos.z);
            
            maxModel.x = Mathf.Max(maxModel.x, pos.x);
            maxModel.y = Mathf.Max(maxModel.y, pos.y);
            maxModel.z = Mathf.Max(maxModel.z, pos.z);
        }

        int xMin = minSpace.x - maxModel.x;
        int xMax = maxSpace.x - minModel.x;
        int yMin = minSpace.y - maxModel.y;
        int yMax = maxSpace.y - minModel.y;
        int zMin = minSpace.z - maxModel.z;
        int zMax = maxSpace.z - minModel.z;
        
        totalSearchSpace = (xMax - xMin + 1) * (yMax - yMin + 1) * (zMax - zMin + 1);
        searchedCount = 0;
        
        UpdateStatus($"Searching offsets in range x:{xMin}-{xMax}, y:{yMin}-{yMax}, z:{zMin}-{zMax}");
        
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    if (shouldStopSearch)
                    {
                        SearchCompleted();
                        yield break;
                    }
                    
                    Vector3Int offset = new Vector3Int(x, y, z);
                    bool isValidOffset = true;
                    
                   
                    foreach (var modelPos in modelPositions)
                    {
                        Vector3Int offsetPos = modelPos + offset;
                        if (!spacePositions.Contains(offsetPos))
                        {
                            isValidOffset = false;
                            break;
                        }
                    }
                    
                    if (isValidOffset)
                    {
                      
                        foundOffsets.Add(offset);
                        
                      
                        if (foundOffsets.Count <= 10 && visualizer != null)
                        {
                            visualizer.VisualizeMatch(modelPositions, offset);
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    
                    searchedCount++;
                    
              
                    if (searchedCount % 1000 == 0 || foundOffsets.Count % 10 == 0)
                    {
                        float elapsedSeconds = (float)searchTimer.Elapsed.TotalSeconds;
                        float searchesPerSecond = searchedCount / elapsedSeconds;
                        
                        UpdateProgress((float)searchedCount / totalSearchSpace);
                        UpdateStatistics(foundOffsets.Count, searchedCount, totalSearchSpace, searchesPerSecond);
                        
                   
                        yield return null;
                    }
                }
            }
        }
        
        SearchCompleted();
    }
    
    private IEnumerator HashBasedSearch()
    {
        UpdateStatus("Starting hash-based search...");
        

        Dictionary<Vector3Int, int> offsetCounts = new Dictionary<Vector3Int, int>();
        

        foreach (var modelPos in modelPositions)
        {
         
            int positionIndex = modelPositions.IndexOf(modelPos);
            UpdateProgress((float)positionIndex / modelPositions.Count);
            
            foreach (var spacePos in spacePositions)
            {
                if (shouldStopSearch)
                {
                    SearchCompleted();
                    yield break;
                }
                
       
                Vector3Int offset = spacePos - modelPos;
                
              
                if (offsetCounts.ContainsKey(offset))
                {
                    offsetCounts[offset]++;
                }
                else
                {
                    offsetCounts[offset] = 1;
                }
                
                searchedCount++;
                
            
                if (searchedCount % 100000 == 0)
                {
                    float elapsedSeconds = (float)searchTimer.Elapsed.TotalSeconds;
                    float searchesPerSecond = searchedCount / elapsedSeconds;
                    
                    UpdateStatistics(offsetCounts.Count, searchedCount, modelPositions.Count * spacePositions.Count, searchesPerSecond);
                    yield return null;
                }
            }
        }

        int validOffsetCount = 0;
        foreach (var kvp in offsetCounts)
        {
            if (kvp.Value == modelPositions.Count)
            {
                foundOffsets.Add(kvp.Key);
                validOffsetCount++;
                
         
                if (validOffsetCount <= 10 && visualizer != null)
                {
                    visualizer.VisualizeMatch(modelPositions, kvp.Key);
                    yield return new WaitForSeconds(0.1f);
                }
                
                if (validOffsetCount % 10 == 0)
                {
                    UpdateStatus($"Found {validOffsetCount} valid offsets...");
                    yield return null;
                }
            }
        }
        
        SearchCompleted();
    }
    
    private IEnumerator ModelBasedSearch()
    {
        UpdateStatus("Starting model-based search...");
        
 
        if (modelPositions.Count < 2)
        {
            UpdateStatus("Error: Need at least 2 model positions for model-based search.");
            SearchCompleted();
            yield break;
        }
        
   
        Vector3Int modelPos1 = modelPositions[0];
        Vector3Int modelPos2 = modelPositions[1];
        
   
        Vector3Int modelDisplacement = modelPos2 - modelPos1;

        List<Vector3Int> potentialOffsets = new List<Vector3Int>();
        
        foreach (var spacePos1 in spacePositions)
        {
            if (shouldStopSearch)
            {
                SearchCompleted();
                yield break;
            }
         
            Vector3Int requiredPos2 = spacePos1 + modelDisplacement;
            
            if (spacePositions.Contains(requiredPos2))
            {
             
                Vector3Int offset = spacePos1 - modelPos1;
                potentialOffsets.Add(offset);
            }
            
            searchedCount++;
      
            if (searchedCount % 1000 == 0)
            {
                float elapsedSeconds = (float)searchTimer.Elapsed.TotalSeconds;
                float searchesPerSecond = searchedCount / elapsedSeconds;
                
                UpdateProgress((float)searchedCount / spacePositions.Count);
                UpdateStatistics(potentialOffsets.Count, searchedCount, spacePositions.Count, searchesPerSecond);
                yield return null;
            }
        }
        
        UpdateStatus($"Found {potentialOffsets.Count} potential offsets, validating...");
        
      
        int testedOffsets = 0;
        foreach (var offset in potentialOffsets)
        {
            if (shouldStopSearch)
            {
                SearchCompleted();
                yield break;
            }
            
            bool isValidOffset = true;
            
         
            for (int i = 2; i < modelPositions.Count; i++)
            {
                Vector3Int offsetPos = modelPositions[i] + offset;
                if (!spacePositions.Contains(offsetPos))
                {
                    isValidOffset = false;
                    break;
                }
            }
            
            if (isValidOffset)
            {
                foundOffsets.Add(offset);
                
            
                if (foundOffsets.Count <= 10 && visualizer != null)
                {
                    visualizer.VisualizeMatch(modelPositions, offset);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            testedOffsets++;
            
    
            if (testedOffsets % 100 == 0 || foundOffsets.Count % 10 == 0)
            {
                float elapsedSeconds = (float)searchTimer.Elapsed.TotalSeconds;
                float searchesPerSecond = (searchedCount + testedOffsets) / elapsedSeconds;
                
                UpdateProgress((float)testedOffsets / potentialOffsets.Count);
                UpdateStatistics(foundOffsets.Count, testedOffsets, potentialOffsets.Count, searchesPerSecond);
                yield return null;
            }
        }
        
        SearchCompleted();
    }
    
    #endregion
    
    #region Utility Methods
    
    private Vector3Int ExtractPositionFromMatrix(Matrix4x4 matrix)
    {
        return new Vector3Int(
            Mathf.RoundToInt(matrix.m03),
            Mathf.RoundToInt(matrix.m13),
            Mathf.RoundToInt(matrix.m23)
        );
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }
    
    private void UpdateProgress(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }
    }
    
    private void UpdateStatistics(int foundCount, int searchedCount, int totalCount, float searchesPerSecond)
    {
        if (statisticsText != null)
        {
            string stats = $"Found: {foundCount} offsets\n" +
                           $"Searched: {searchedCount:N0} / {totalCount:N0}\n" +
                           $"Speed: {searchesPerSecond:N0}/s\n" +
                           $"Time: {searchTimer.Elapsed.TotalSeconds:N1}s";
            
            statisticsText.text = stats;
        }
    }
    
    #endregion
}
