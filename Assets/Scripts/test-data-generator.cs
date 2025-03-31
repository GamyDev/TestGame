using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;


public class TestDataGenerator : MonoBehaviour
{
    [SerializeField] private int numberOfModelMatrices = 100;
    [SerializeField] private int numberOfSpaceMatrices = 5000;
    [SerializeField] private int modelMatrixSpread = 50;
    [SerializeField] private int spaceMatrixSpread = 200;
    [SerializeField] private int numberOfValidOffsets = 10;
    [SerializeField] private string modelOutputPath = "Assets/Resources/model.json";
    [SerializeField] private string spaceOutputPath = "Assets/Resources/space.json";
    
    [Header("Generation Settings")]
    [SerializeField] private bool includeRotations = false;
    [SerializeField] private bool includeScaling = false;
    [SerializeField] private int seed = 42;
    
    private System.Random random;
    
    public void GenerateTestData()
    {
        random = new System.Random(seed);
        
        Debug.Log("Generating test data...");
        

        List<Matrix4x4> modelMatrices = GenerateModelMatrices();
        

        List<Matrix4x4> spaceMatrices = GenerateSpaceMatrices(modelMatrices);
        

        SaveMatricesToJson(modelMatrices, modelOutputPath);
        SaveMatricesToJson(spaceMatrices, spaceOutputPath);
        
        Debug.Log($"Test data generation complete. Generated {modelMatrices.Count} model matrices and {spaceMatrices.Count} space matrices");
        Debug.Log($"Files saved to {modelOutputPath} and {spaceOutputPath}");
    }
    
    private List<Matrix4x4> GenerateModelMatrices()
    {
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        

        HashSet<Vector3Int> usedPositions = new HashSet<Vector3Int>();
        
        for (int i = 0; i < numberOfModelMatrices; i++)
        {

            Vector3Int position;
            do
            {
                position = new Vector3Int(
                    random.Next(-modelMatrixSpread, modelMatrixSpread),
                    random.Next(-modelMatrixSpread, modelMatrixSpread),
                    random.Next(-modelMatrixSpread, modelMatrixSpread)
                );
            } while (usedPositions.Contains(position));
            
            usedPositions.Add(position);

            Matrix4x4 matrix = CreateMatrixFromPosition(position);
            matrices.Add(matrix);
        }
        
        return matrices;
    }
    
    private List<Matrix4x4> GenerateSpaceMatrices(List<Matrix4x4> modelMatrices)
    {
        List<Matrix4x4> spaceMatrices = new List<Matrix4x4>();
        HashSet<Vector3Int> usedPositions = new HashSet<Vector3Int>();
        

        List<Vector3Int> validOffsets = GenerateValidOffsets();
        
        foreach (Vector3Int offset in validOffsets)
        {
            foreach (Matrix4x4 modelMatrix in modelMatrices)
            {
                Vector3Int originalPos = ExtractPositionFromMatrix(modelMatrix);
                Vector3Int offsetPos = originalPos + offset;
                
                Matrix4x4 offsetMatrix = CreateMatrixFromPosition(offsetPos);
                spaceMatrices.Add(offsetMatrix);
                usedPositions.Add(offsetPos);
            }
        }
        

        int remainingMatrices = numberOfSpaceMatrices - spaceMatrices.Count;
        for (int i = 0; i < remainingMatrices; i++)
        {
        
            Vector3Int position;
            do
            {
                position = new Vector3Int(
                    random.Next(-spaceMatrixSpread, spaceMatrixSpread),
                    random.Next(-spaceMatrixSpread, spaceMatrixSpread),
                    random.Next(-spaceMatrixSpread, spaceMatrixSpread)
                );
            } while (usedPositions.Contains(position));
            
            usedPositions.Add(position);
            
    
            Matrix4x4 matrix = CreateMatrixFromPosition(position);
            spaceMatrices.Add(matrix);
        }
        
        return spaceMatrices;
    }
    
    private List<Vector3Int> GenerateValidOffsets()
    {
        List<Vector3Int> offsets = new List<Vector3Int>();
        HashSet<Vector3Int> usedOffsets = new HashSet<Vector3Int>();
        
        for (int i = 0; i < numberOfValidOffsets; i++)
        {
           
            Vector3Int offset;
            do
            {
                offset = new Vector3Int(
                    random.Next(-spaceMatrixSpread/2, spaceMatrixSpread/2),
                    random.Next(-spaceMatrixSpread/2, spaceMatrixSpread/2),
                    random.Next(-spaceMatrixSpread/2, spaceMatrixSpread/2)
                );
            } while (usedOffsets.Contains(offset));
            
            usedOffsets.Add(offset);
            offsets.Add(offset);
            
            Debug.Log($"Generated valid offset: {offset}");
        }
        
        return offsets;
    }
    
    private Matrix4x4 CreateMatrixFromPosition(Vector3Int position)
    {
        Matrix4x4 matrix = Matrix4x4.identity;
        
  
        matrix.m03 = position.x;
        matrix.m13 = position.y;
        matrix.m23 = position.z;
   
        if (includeRotations)
        {
            Quaternion rotation = Quaternion.Euler(
                random.Next(0, 360),
                random.Next(0, 360),
                random.Next(0, 360)
            );
            
            Matrix4x4 rotMatrix = Matrix4x4.Rotate(rotation);
            matrix = matrix * rotMatrix;
        }
     
        if (includeScaling)
        {
            float scale = (float)random.NextDouble() * 2f + 0.5f;
            matrix.m00 = scale;
            matrix.m11 = scale;
            matrix.m22 = scale;
        }
        
        return matrix;
    }
    
    private Vector3Int ExtractPositionFromMatrix(Matrix4x4 matrix)
    {
        return new Vector3Int(
            Mathf.RoundToInt(matrix.m03),
            Mathf.RoundToInt(matrix.m13),
            Mathf.RoundToInt(matrix.m23)
        );
    }
    
    private void SaveMatricesToJson(List<Matrix4x4> matrices, string filePath)
    {
  
        float[][,] serializedMatrices = new float[matrices.Count][,];
        
        for (int i = 0; i < matrices.Count; i++)
        {
            Matrix4x4 matrix = matrices[i];
            float[,] serializedMatrix = new float[4,4];
            
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    serializedMatrix[row, col] = matrix[row, col];
                }
            }
            
            serializedMatrices[i] = serializedMatrix;
        }
        
    
        var dataWrapper = new
        {
            matrices = serializedMatrices
        };
        

        string json = JsonConvert.SerializeObject(dataWrapper, Formatting.Indented);
        

        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
  
        File.WriteAllText(filePath, json);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestDataGenerator))]
public class TestDataGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TestDataGenerator generator = (TestDataGenerator)target;
        
        if (GUILayout.Button("Generate Test Data"))
        {
            generator.GenerateTestData();
        }
    }
}
#endif
