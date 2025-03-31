using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;


public class MatrixDataLoader : MonoBehaviour
{

    public static MatrixDataLoader Instance { get; private set; }


    public event Action<List<Matrix4x4>> OnModelMatricesLoaded;
    public event Action<List<Matrix4x4>> OnSpaceMatricesLoaded;
    public event Action<string> OnError;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void LoadModelMatrices(string filePath)
    {
        try
        {
            string jsonText = File.ReadAllText(filePath);
            var matrices = ParseMatricesFromJson(jsonText);
            Debug.Log($"Loaded {matrices.Count} model matrices from {filePath}");
            OnModelMatricesLoaded?.Invoke(matrices);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error loading model matrices: {ex.Message}";
            Debug.LogError(errorMsg);
            OnError?.Invoke(errorMsg);
        }
    }


    public void LoadSpaceMatrices(string filePath)
    {
        try
        {
            string jsonText = File.ReadAllText(filePath);
            var matrices = ParseMatricesFromJson(jsonText);
            Debug.Log($"Loaded {matrices.Count} space matrices from {filePath}");
            OnSpaceMatricesLoaded?.Invoke(matrices);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error loading space matrices: {ex.Message}";
            Debug.LogError(errorMsg);
            OnError?.Invoke(errorMsg);
        }
    }

    private List<Matrix4x4> ParseMatricesFromJson(string jsonText)
    {

        var matrixData = JsonConvert.DeserializeObject<MatrixJsonData>(jsonText);

        if (matrixData == null || matrixData.matrices == null)
        {
            throw new FormatException("Invalid JSON format or missing matrices array");
        }

        List<Matrix4x4> result = new List<Matrix4x4>();

        foreach (var jsonMatrix in matrixData.matrices)
        {
            if (jsonMatrix == null || jsonMatrix.GetLength(0) != 4 || jsonMatrix.GetLength(1) != 4)
            {
                Debug.LogWarning("Found invalid matrix in JSON (not 4x4), skipping");
                continue;
            }

            Matrix4x4 matrix = new Matrix4x4();

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    matrix[row, col] = jsonMatrix[row, col];
                }
            }

            result.Add(matrix);
        }

        return result;
    }

    public void SaveOffsetsToJson(List<Vector3Int> offsets, string filePath)
    {
        try
        {
            var offsetsData = new OffsetJsonData
            {
                offsets = new List<Vector3JsonData>(),
                count = offsets.Count,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var offset in offsets)
            {
                offsetsData.offsets.Add(new Vector3JsonData
                {
                    x = offset.x,
                    y = offset.y,
                    z = offset.z
                });
            }

            string json = JsonConvert.SerializeObject(offsetsData, Formatting.Indented);
            File.WriteAllText(filePath, json);

            Debug.Log($"Successfully saved {offsets.Count} offsets to {filePath}");
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error saving offsets to JSON: {ex.Message}";
            Debug.LogError(errorMsg);
            OnError?.Invoke(errorMsg);
        }
    }
}

[Serializable]
public class MatrixJsonData
{
    public float[][,] matrices;
}

[Serializable]
public class OffsetJsonData
{
    public List<Vector3JsonData> offsets;
    public int count;
    public string timestamp;
}

[Serializable]
public class Vector3JsonData
{
    public int x;
    public int y;
    public int z;
}