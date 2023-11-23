using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.CompilerServices;
using System;


/**************************************************************************
             Date Last Changed: 23.10.2023 - Version 2.1

Author: Frederick Van Bockryck, Jakob Schöllauf, Florian Waltersdorfer

TODO - The next few Lines should have a description of what the script does and 
how it works. I still have to do that.

**************************************************************************/
public class Generator : MonoBehaviour{

    // TODO - Fill in Tooltips
    // Inputs Essential Elements
    [Header("Essentials")]
    [Space(5)]
    [Tooltip("Example Tooltip")]
    public string heightmapPath;

    // Inputs Optional Elements
    [Space(20)]
    [Header("Non Essentials (Check for Corresponding Settings)")]
    [Space(5)]
    [Tooltip("Example Tooltip")]
    public Material customMaterial;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public string texturePath;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public string naturalObjectsMapPath;
    [Space(10)]
    [Tooltip("Example Tooltip")]
    public List<Element> environmentObjects;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public GameObject virtualCamera;

    // Inputs for Given Settings
    [Space(20)]
    [Header("Settings")]
    [Space(5)]
    [Tooltip("Example Tooltip")]
    public bool debuggingMessages = true;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public bool usingCustomTexture;
    [Tooltip("Example Tooltip")]
    public bool usingCostumMaterial;
    [Tooltip("Example Tooltip")]
    public bool usingCostumEnvironmentObjects;
    [Tooltip("Example Tooltip")]
    public bool generateNaturalObjects;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public Grad turnTextureXDegrees;
    public enum Grad
    {
        _0 = 0,
        _90 = 1,
        _180 = 2,
        _240 = 3
    }

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public int maximumHeight = 100;
    [Range(0, 1)]
    [Tooltip("Example Tooltip")]
    public float scaleHeight = 1;
    [Tooltip("Example Tooltip")]
    public bool invertHeight;

    [Space(10)]
    [Tooltip("Example Tooltip")]
    public bool placeVirtualCamera;


    // Element Object
    [Serializable]
    public struct Element
    {
        public string name;
        public Color mappedColor;
        public GameObject prefab;
    }


    // Nessecary private Variables
    private Texture2D heightmap;
    private Terrain terrain;
    private TerrainData terrainData;
    private Material material;
    private Texture2D texture;
    private Texture2D naturalObjectsMap;

    // If Given Option is Turned On Debug Messages Will Be Displayed
    private void Debugger(string text)
    {
        if (debuggingMessages)
        {
            Debug.Log(text);
        }
    }

    // The Normal Start Function that gets Called the First Time the Script gets Loaded
    private void Start()
    {
        Generation(); 
    }

    // Starts the Creation of the Environment if there is a valid Heightmap
    private void Generation()
    {
        if (GetHeightAndWidth())
        {
            CreateBasicMap();
            GenerateNaturalObjects();
        }     
    }

    // Reads in the Height and Width (Based on the Heightmap)
    private bool GetHeightAndWidth()
    {
        // Checks if Heightmap Path is Empty
        if (heightmapPath == "")
        {
            Debugger("The Heightmap Path is empyty. \nEvironment can not be Generated.");
            return false;
        }

        // Checks if Heightmap File Exists
        if (File.Exists(heightmapPath))
        {
            // Initilize a heightmap Texture
            // (The Width and Height in this Constructure can be Anything it will get Overwritten by LoadImage)
            heightmap = new Texture2D(1, 1);

            // Gets the Raw Data of the Heightimage
            byte[] heigthmapRawBytes = File.ReadAllBytes(heightmapPath);

            // The Data gets converted to an Texture so we can read out Certain Information (e.g Width, Height, etc)
            // and Additionally we Work Around the Compression of the Raw Byte Values
            heightmap.LoadImage(heigthmapRawBytes);

            Debugger($"Terrain Data - Height: {heightmap.height}, Width: {heightmap.width}");
            return true;
        }
        else
        {
            Debugger($"The Heightmap File at the path \"{heightmapPath}\" does not exist. " +
                $"\nEvironment can not be Generated.");
            return false;
        }
    }

    // Creates the Basic Layout of the Environment
    private void CreateBasicMap()
    {
        // Create the Terrain Object
        terrain = gameObject.AddComponent<Terrain>();

        // Create Terrain Data
        terrainData = new TerrainData();

        // Set the Terrain Attributes
        terrainData.size = new Vector3(heightmap.width, maximumHeight, heightmap.height);
        terrainData.heightmapResolution = heightmap.width + 1;

        ApplyHeightmap();
        HandleMaterialAndTexture();

        // Assigns the Terrain Data Object
        terrain.terrainData = terrainData;        
    }

    // Gets and Sets Heightmap
    private void ApplyHeightmap()
    {
        // Initiliaze a 2D Array for the Heightdata
        float[,] data = new float[heightmap.height, heightmap.width];

        // Loops through all the Pixels in the Heightmap
        for (int i = 0; i < heightmap.height; i++)
        {
            for (int y = 0; y < heightmap.width; y++)
            {
                // Reads out the Value of the Heightmap at a Certain Pixel and 
                // puts it in to the Heightdata Array (Inverts the given Pixel if Neccesary)
                if (invertHeight)
                {
                    data[i, y] = 1 - heightmap.GetPixel(i, y).grayscale * scaleHeight;
                }
                else
                {
                    data[i, y] = heightmap.GetPixel(i, y).grayscale * scaleHeight;
                }
            }
        }
        // Assigns Height Data
        terrainData.SetHeights(0, 0, data);
    }

    // Gets and Sets the Material based on Settings
    private void HandleMaterialAndTexture()
    {
        // Checking for custom Materials
        if (usingCostumMaterial)
        {
            material = customMaterial;
        }
        else
        {
            // Sets default Material
            material = new Material(Shader.Find("Specular")); // TODO - Check for better default Material
        }
        
        ApplyTexture();
        terrain.materialTemplate = material;
    }

    // Gets and Sets the Texture based on Settings
    private void ApplyTexture()
    {
        // Check if Custom Texture is enabled
        if (!usingCustomTexture) { return; }

        // Checks if Heightmap Path is Empty
        if (heightmapPath == "")
        {
            Debugger("The Texture Path is empyty. \nTexture can not be applied.");
            return;
        }

        // Checks if Texture exists
        if (File.Exists(texturePath))
        {
            // Initilize a Texture2D Object with the Environment Texture
            // (The Width and Height in this Constructure can be Anything it will get overwritten by LoadImage)
            texture = new Texture2D(1, 1);

            // Gets the Raw Data of the Environment Texture
            byte[] textureRawBytes = File.ReadAllBytes(texturePath);

            // The Data gets converted to an Texture so we can read out Certain Information (e.g Width, Height, Pixels, etc)
            // and Additionally we Work Around the Compression of the Raw Byte Values
            texture.LoadImage(textureRawBytes);

            // Checks if the Texture Size Differs from the Heightmap
            if (texture.width != heightmap.width || texture.height != heightmap.height)
            {
                Debugger("The Texture Size is different from the Heightmap size. Texture is going to be Cropped or Streched.");
                CropOrStretchTexture();
            }

            RotateTexture();

            // The Enviroment Texture gets applied on the Material
            material.mainTexture = texture;
        }
        else
        {
            Debugger($"The Texture File at the path \"{texturePath}\" does not exist. " +
                $"\nTexture can not be applied.");
            return;
        }
    }

    // Rotates the given Texture a certain Amount of Degrees
    private void RotateTexture()
    {
        // If no Roattion was Assigned Cancel this Function
        if(turnTextureXDegrees == 0){ return; }

        for (int i = (int)turnTextureXDegrees; i > 0 ; i--)
        {
            Color32[] originalPixels = texture.GetPixels32();
            Color32[] rotatedPixels = new Color32[originalPixels.Length];

            int w = texture.width;
            int h = texture.height;

            // Loop through all Pixels
            for (int x = 0; x < h; ++x)
            {
                for (int y = 0; y < w; ++y)
                {
                    // Take a Pixel, Rotate it 90 degrees and Safe it at the new Position
                    rotatedPixels[(y + 1) * h - x - 1] = originalPixels[originalPixels.Length - 1 - (x * w + y)];
                }
            }

            texture.SetPixels32(rotatedPixels);
            texture.Apply();
        }
    }


    // Fits the given Texture to the Heightmap
    private void CropOrStretchTexture()
    {

        Color[] c = texture.GetPixels (0, 0, Mathf.Clamp(heightmap.width, 0, texture.width), Mathf.Clamp(heightmap.width, 0, texture.height));

	    texture.SetPixels (c);
	    texture.Apply ();
        // TODO - Implement !!
        // Very Important since the size always missmatched in the current setup
    }

    // Manage All Nessecary Functions to Generate Objects and Put them on the Map
    private void GenerateNaturalObjects()
    {
        // Check if Custom Texture is enabled
        if (!generateNaturalObjects) { return; }

        LoadNaturalObjectMap();
        PopulateMap();
    }

    private void LoadNaturalObjectMap()
    {
        // Checks if Heightmap Path is Empty
        if (naturalObjectsMapPath == "")
        {
            Debugger("The Path for Generating Natural Objects is empyty. \nObjects can not be generated.");
            return;
        }

        // Checks if a Natural Object Map File exists
        if (File.Exists(naturalObjectsMapPath))
        {
            // Initilize a Map for Natural Objects
            // (The Width and Height in this Constructure can be Anything it will get Overwritten by LoadImage)
            naturalObjectsMap = new Texture2D(1, 1);

            // Gets the Raw Data of the Natural Object Map
            byte[] NaturalObjectsMapRawBytes = File.ReadAllBytes(naturalObjectsMapPath);

            // The Data gets converted to an Texture so we can read out Certain Information (e.g Pixels, etc)
            // and Additionally we Work Around the Compression of the Raw Byte Values
            naturalObjectsMap.LoadImage(NaturalObjectsMapRawBytes);
        }
        else
        {
            Debugger($"The File for Generating Natural Objects at the path \"{naturalObjectsMapPath}\" " +
                $"does not exist. \nTexture can not be applied.");
            return;
        }
    }

    private void PopulateMap()
    {
        // TODO - Implement
        // Take the "naturalObjectMap" and read it out pixel by pixel (Similiar to the function "ApplyHeightmap")
        // Every Pixel Color will be assigned to a given Gameobject.
        // Also find a better solution to provide Prefabs (Maybe just add a class in the same script with [Name, Assigned Color, Prefab])
    }

}