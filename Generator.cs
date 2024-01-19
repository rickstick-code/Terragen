using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Unity.VisualScripting;
using UnityEditor;
using System.Linq;


/**************************************************************************
                Date Last Changed: 19.01.2024 - Version 2.4

Authors: Frederick Van Bockryck, Jakob Schöllauf, Florian Waltersdorfer

This C# script enables the quick and easy generation of terrain in Unity
based on hightmaps. Additionally it allows the aplication of a custom
texture and material to the terrain as well as populating the terrain with
objects designated by the user.

For detailed instructions on utilizing the generator, please refer to the
documentation available under: https://github.com/rickstick-code/Terragen
**************************************************************************/
public class Generator : MonoBehaviour
{

    // This class enhances the user experience in the Unity Editor
    #if UNITY_EDITOR
    [CustomEditor(typeof(Generator))]
    public class GeneratorEditor : Editor
    {
        // Serialized property for natural objects list
        private SerializedProperty naturalObjects;

        private void OnEnable()
        {
            // Initialize the naturalObjects property
            naturalObjects = serializedObject.FindProperty("naturalObjects");
        }

        public override void OnInspectorGUI()
        {
            // Base GUI for the Inspector
            base.OnInspectorGUI();
            serializedObject.Update();

            // Get the attributes of the Generator.cs script
            Generator generator = (Generator)target;

            // Header for General Settings
            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

            // Start tracking changes made in the UI
            EditorGUI.BeginChangeCheck();

            // Toggles for individual setting groups
            EditorGUIUtility.labelWidth = 240;
            EditorGUI.indentLevel++;

            EditorGUILayout.Space(5);
            generator.showDebugging = EditorGUILayout.Toggle("Show Debugging Messages", generator.showDebugging);
            generator.buildAfterChange = EditorGUILayout.Toggle("Regenerate With Inspector Change", generator.buildAfterChange);
            generator.showHeightmapSettings = EditorGUILayout.Toggle("Show Heightmap Settings", generator.showHeightmapSettings);

            EditorGUILayout.Space(5);
            generator.useCustomTexture = EditorGUILayout.Toggle("Use Custom Texture", generator.useCustomTexture);
            generator.useCustomMaterial = EditorGUILayout.Toggle("Use Custom Material", generator.useCustomMaterial);
            generator.useCustomObjects = EditorGUILayout.Toggle("Use Natural Objects", generator.useCustomObjects);

            EditorGUI.indentLevel--;


            // Heightmap settings
            if (generator.showHeightmapSettings)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Heightmap Settings", EditorStyles.boldLabel);

                // Individual settings for the heightmap
                EditorGUIUtility.labelWidth = 150;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.heightMapPath = EditorGUILayout.DelayedTextField("Path", generator.heightMapPath);
                generator.maximumHeight = EditorGUILayout.DelayedIntField("Maximum Height", generator.maximumHeight);
                generator.scaleHeight = EditorGUILayout.Slider("Scale Height", generator.scaleHeight, 0f, 1f);
                generator.invertHeight = EditorGUILayout.Toggle("Invert Height", generator.invertHeight);


                EditorGUI.indentLevel--;
            }

            // Texture settings
            if (generator.useCustomTexture)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);

                // Individual settings for the custom texture
                EditorGUIUtility.labelWidth = 110;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.textureMapPath = EditorGUILayout.DelayedTextField("Path", generator.textureMapPath);
                generator.rotateXDegrees = (Grad)EditorGUILayout.EnumPopup("Rotate Texture", generator.rotateXDegrees);
                generator.mirrorTexture = EditorGUILayout.Toggle("Mirror Texture", generator.mirrorTexture);

                EditorGUI.indentLevel--;
            }

            // Material settings
            if (generator.useCustomMaterial)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);

                // Individual settings for the custom material
                EditorGUIUtility.labelWidth = 120;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.customMaterial = (Material)EditorGUILayout.ObjectField("Custom Material", generator.customMaterial, typeof(Material), true);

                EditorGUI.indentLevel--;
            }

            // Natural object settings
            if (generator.useCustomObjects)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Natural Objects Settings", EditorStyles.boldLabel);     

                // Individual settings for natural objects
                EditorGUIUtility.labelWidth = 120;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.objectsMapPath = EditorGUILayout.DelayedTextField("Path For Map", generator.objectsMapPath);
                generator.objectDensity = EditorGUILayout.Slider("Object Density", generator.objectDensity, 0.10f, 5f);

                // An option to collapse and add natural objects
                GUILayout.BeginHorizontal();
                generator.showIndividualObjects = EditorGUILayout.Foldout(generator.showIndividualObjects, "Objects", true);
                if (GUILayout.Button("Add Object"))
                {
                    AddElement();
                }
                GUILayout.EndHorizontal();


                if (generator.showIndividualObjects)
                {
                    EditorGUILayout.Space(20);
                    for (int i = 0; i < naturalObjects.arraySize; i++)
                    {
                        SerializedProperty element = naturalObjects.GetArrayElementAtIndex(i);

                        GUILayout.BeginHorizontal();

                        EditorGUILayout.PropertyField(element.FindPropertyRelative("name"), new GUIContent("Name"));
                        if (GUILayout.Button("Remove"))
                        {
                            RemoveElement(i);
                        }

                        GUILayout.EndHorizontal();

                        EditorGUILayout.PropertyField(element.FindPropertyRelative("mappedColor"), new GUIContent("Mapped Color"));
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("colorTolerance"), new GUIContent("Color Tolerance"));

                        // Prefab settings
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("usePrefab"), new GUIContent("Use Prefab"));
                        if (element.FindPropertyRelative("usePrefab").boolValue)
                        {
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("prefab"), new GUIContent(""));
                        }
                        GUILayout.EndHorizontal();

                        // Texture settings
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("useTexture"), new GUIContent("Use Texture"));
                        if (element.FindPropertyRelative("useTexture").boolValue)
                        {
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("texture"), new GUIContent(""));
                        }
                        GUILayout.EndHorizontal();


                        EditorGUILayout.Space(20);            
                    }
                }

                EditorGUI.indentLevel--;
            }

            // Save the state of serialized objects
            serializedObject.ApplyModifiedProperties();

            // Check for changes and regenerate if needed
            if (EditorGUI.EndChangeCheck() && generator.buildAfterChange)
            {
                generator.Generate();
            }

            // Credits Section
            EditorGUILayout.Space(50);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

            EditorGUILayout.Space(10);

            GUIStyle creditStyle = new GUIStyle(EditorStyles.label);
            creditStyle.fontSize = 10;

            // Header for Credits
            creditStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Credits", creditStyle);

            // Main body of Credits
            creditStyle.wordWrap = true;
            creditStyle.fontStyle = FontStyle.Italic;
            EditorGUILayout.LabelField("This script was created as part of a lecture at the University of Applied Sciences FH JOANNEUM. " +
                "The authors were Frederick Van Bockryck, Jakob Schöllauf, and Florian Waltersdorfer under the guidance of Dipl.-Päd. Gerhard Sprung, MSc.", creditStyle);

            EditorGUILayout.Space(20);
        }

        // Helper method to add a new natural object to the list
        private void AddElement()
        {
            Generator script = (Generator)target;

            if (script.naturalObjects == null)
            {
                script.naturalObjects = new List<Generator.Element>();
            }

            script.naturalObjects.Add(new Generator.Element());
        }

        // Helper method to remove a natural object from the list
        private void RemoveElement(int index)
        {
            Generator script = (Generator)target;

            if (index >= 0 && index < script.naturalObjects.Count)
            {
                script.naturalObjects.RemoveAt(index);
            }
        }
    }
#endif
    // All neccessary variables (Variables must be serialized or public in order to keep their changed state in the custom inspector)
    [HideInInspector]
    public bool showDebugging = true;

    [HideInInspector]
    public bool 
        showHeightmapSettings = true, 
        useCustomTexture = true,
        useCustomMaterial = true,
        useCustomObjects = true;

    [HideInInspector]
    public string 
        heightMapPath = @"C:\Users\Mustermann\Pictures\heightmap.png",
        textureMapPath = @"C:\Users\Mustermann\Pictures\texture.png",
        objectsMapPath = @"C:\Users\Mustermann\Pictures\objects_mapping.png";

    [HideInInspector]
    public Material customMaterial;

    [HideInInspector]
    public bool 
        invertHeight = false,
        mirrorTexture = false,
        buildAfterChange = false,
        showIndividualObjects = false;

    [HideInInspector]
    public int maximumHeight = 300;

    [HideInInspector]
    public float 
        scaleHeight = 1,
        objectDensity = 1;

    [HideInInspector]
    public Grad rotateXDegrees;

    [HideInInspector]
    public List<Element> naturalObjects;

    // Enum defining rotation angles
    public enum Grad
    {
        _0 = 0,
        _90 = 1,
        _180 = 2,
        _270 = 3
    }

    [Serializable]
    public class Element
    {
        public string name;
        public Color mappedColor;
        [Range(1.0f, 1.5f)]
        public float colorTolerance = 1.01f;

        public bool usePrefab = false;
        public GameObject prefab;
        public bool useTexture = false;
        public Texture2D texture;
    }


    // If this option is turned on debug messages will be displayed
    private void Debugger(string text)
    {
        if (showDebugging)
        {
            Debug.Log(text);
        }
    }

    // This is a normal start function that gets called the first time the script is loaded
    private void Start()
    {
        Generate();
    }

    [ContextMenu("Generate Again")]
    void Generate()
    {
        Debugger("### Start Generating ###");

        // Destroy old Terrain and TerrainCollider components as well as all natural objects
        DestroyImmediate(GetComponent<Terrain>());
        DestroyImmediate(GetComponent<TerrainCollider>());
        foreach(Transform tmp in transform) { Destroy(tmp.gameObject); };

        // Generate terrain and apply settings based on user-defined parameters
        Handler(heightMapPath, maximumHeight, invertHeight, scaleHeight, rotateXDegrees, mirrorTexture, useCustomMaterial,
            customMaterial, useCustomTexture, textureMapPath, useCustomObjects, naturalObjects, objectsMapPath, objectDensity);

        Debugger("Finished Generating");
    }

    // This method handles the entire generation process and can be called manually from another script with all available settings
    public void Handler(string heightMapPath, int maximumHeight = 300, bool invertHeight = false, float scaleHeight = 1, Grad rotateXDegrees = 0, bool mirrorTexture = false,
        bool useCustomMaterial = false, Material customMaterial = null, bool useCustomTexture = false, string textureMapPath = "",
        bool useCustomObjects = false, List<Element> naturalObjects = null,  string objectsMapPath = "", float objectDensity = 0.25f)
    {
        try
        {
            // Load the heightmap texture from the specified path
            Texture2D heightmap = LoadHeightmap(heightMapPath);

            // Create terrain and apply Objects
            Terrain terrain = CreateTerrain(heightmap, maximumHeight);

            ApplyHeight(terrain, heightmap, invertHeight, scaleHeight);
            ApplyMaterial(terrain, useCustomMaterial, customMaterial);
            ApplyTexture(terrain, useCustomTexture, textureMapPath, rotateXDegrees, mirrorTexture);
            ApplyObjects(terrain, useCustomObjects, naturalObjects, objectsMapPath, objectDensity);

        }
        catch (Exception exception)
        {
            // Display an error message if an exception occurs during terrain generation
            Debugger(exception.Message);
        }
        
    }

    // Method to load the heightmap texture from a specified file path
    private Texture2D LoadHeightmap(string heightMapPath)
    {
        Debugger("### Load Heightmap ###");

        // Check if the heightmap path is empty
        if (heightMapPath == "")
        {
            throw new Exception("The Heightmap Path is empty. \nEnvironment can not be generated.");
        }

        // Check if a file exists at the given path
        if (!File.Exists(heightMapPath))
        {
            throw new Exception($"The Heightmap at the path \"{heightMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initialize Texture2D object for the heightmap (width and height will be overwritten later)
        Texture2D heightmap = new Texture2D(1, 1);

        // The raw bytes of the heightmap image are read and converted for better handling of
        // certain aspects (compression, width, height, bit depth, etc.)
        heightmap.LoadImage(File.ReadAllBytes(heightMapPath));
        return MakeItSquare(heightmap, Mathf.NextPowerOfTwo(Mathf.Max(heightmap.width, heightmap.height)));
    }

    // Method to create the Terrain object and TerrainData
    private Terrain CreateTerrain(Texture2D heightmap, int maximumHeight)
    {
        Debugger("### Create Terrain ###");

        // Create the Terrain Object
        Terrain terrain = gameObject.AddComponent<Terrain>();

        // Create the Terrain Data
        TerrainData terrainData = new TerrainData();

        // Set the Terrain Attributes
        terrainData.heightmapResolution = heightmap.width + 1;
        terrainData.size = new Vector3(heightmap.width, maximumHeight, heightmap.height);

        // Assign the Terrain Data to the Terrain Object
        terrain.terrainData = terrainData;

        return terrain;
    }

    // Method to apply height values to the Terrain based on a heightmap
    private void ApplyHeight(Terrain terrain, Texture2D heightmap, bool invertHeight, float scaleHeight)
    {
        Debugger("### Apply Heightmap ###");

        // Initiliaze a 2D array for the height data
        float[,] data = new float[heightmap.width, heightmap.height];

        // Loops through all the Pixels in the heightmap
        for (int i = 0; i < heightmap.width; i++)
        {
            for (int y = 0; y < heightmap.height; y++)
            {
                // Read the value of the heightmap at a certain pixel and store it in the array
                data[i, y] = Mathf.Clamp01((invertHeight ? (1 - heightmap.GetPixel(i, y).grayscale) :
                    (heightmap.GetPixel(i, y).grayscale)) * scaleHeight);
            }
        }
        // Assign the height data to the Terrain Object
        terrain.terrainData.SetHeights(0, 0, data);

        // Create the TerrainCollider and adapt it to the current terrain
        TerrainCollider collider = terrain.AddComponent<TerrainCollider>();
        collider.terrainData = terrain.terrainData;

        Debugger("Heightmap was applied.");
    }

    // Method to apply the material to the Terrain
    private void ApplyMaterial(Terrain terrain, bool useCustomMaterial, Material customMaterial)
    {
        Debugger("### Apply Material ###");

        // Check for custom materials
        if (useCustomMaterial)
        {
            if (customMaterial != null)
            {
                terrain.materialTemplate = customMaterial;
                return;
            }
            throw new Exception("The Custom Material is empty. \nTurn off Custom Material or provide Material.");
        }
        // Use a default material if custom material is not specified
        terrain.materialTemplate = new Material(Shader.Find("Specular"));

        Debugger("Material was applied.");
    }

    // Method to apply the texture to the Terrain
    private void ApplyTexture(Terrain terrain, bool useCustomTexture, string textureMapPath, Grad rotateXDegrees, bool mirrorTexture)
    {
        Debugger("### Apply Texture ###");

        // Check if custom texture is enabled
        if (!useCustomTexture) { return; }

        // Check if texture path is empty
        if (textureMapPath == "")
        {
            throw new Exception("The Texture Path is empty. \nEnvironment can not be generated.");
        }

        // Check if a file exists at the given path
        if (!File.Exists(textureMapPath))
        {
            throw new Exception($"The Texture at the path \"{textureMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initilize Texture2D object of the texture (height and width will be overwritten later)
        Texture2D texture = new Texture2D(1, 1);

        // Read raw bytes of the texture image and convert for better handling
        texture.LoadImage(File.ReadAllBytes(textureMapPath));

        // Apply the rotated and mirrored texture to the Terrain object
        terrain.materialTemplate.mainTexture = RotateTexture(MirrorTexture(MakeItSquare(texture, terrain.terrainData.heightmapResolution), mirrorTexture), rotateXDegrees);

        Debugger("Texture was applied.");
    }

    // Method to apply natural objects to the Terrain
    private void ApplyObjects(Terrain terrain, bool useCustomObjects, List<Element> naturalObjects, string objectsMapPath, float objectDensity)
    {
        Debugger("### Apply Objects ###");

        // Check if custom objects are enabled
        if (!useCustomObjects) { return; }

        // Check if the objectmap path is empty
        if (objectsMapPath == "")
        {
            throw new Exception("The Objects Path is empty. \nEnvironment can not be generated.");
        }

        // Check if there is a file at the given path
        if (!File.Exists(objectsMapPath))
        {
            throw new Exception($"The Texture at the path \"{objectsMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initilize Texture2D object of the texture (height and width will be overwritten later)
        Texture2D objectMaptexture = new Texture2D(1, 1);

        // Read raw bytes of the object map image and convert for better handling
        objectMaptexture.LoadImage(File.ReadAllBytes(objectsMapPath));
        objectMaptexture = RotateTexture(MirrorTexture(MakeItSquare(objectMaptexture, terrain.terrainData.heightmapResolution), mirrorTexture), rotateXDegrees);

        // Calculate the tile size based on the density
        int tileSize = (int)Math.Round(objectMaptexture.height / (100 * objectDensity));

        // Get the main texture from the Terrain material
        Texture2D terrainTexture = (Texture2D)terrain.materialTemplate.mainTexture;

        // Loop through all pixels and insert a given object
        for (int i = 0; i < objectMaptexture.height; i += tileSize)
        {
            for (int y = 0; y < objectMaptexture.width; y += tileSize)
            {
                // Insert the object into the Terrain and update the texture
                Tuple<bool, Texture2D> tmp = InsertObject(terrain, terrainTexture, naturalObjects, i, y, tileSize, objectMaptexture);
                if (tmp.Item1) { terrainTexture = tmp.Item2; }
            }
        }

        // Apply the updated texture to the Terrain object
        terrain.materialTemplate.mainTexture = terrainTexture;

        Debugger("Objects were applied.");
    }


    // Method to make a texture square
    private Texture2D MakeItSquare(Texture2D texture, int size)
    {
        // Create a new texture with the calculated size
        Texture2D resizedTexture = new Texture2D(size, size);

        // Copy the pixels from the original texture to the resized texture
        for (int i = 0; i < size; i++)
        {
            for (int y = 0; y < size; y++)
            {
                // Get the color of the pixel from the original texture
                Color color = texture.GetPixel(
                    Mathf.FloorToInt((float)i / size * texture.width),
                    Mathf.FloorToInt((float)y / size * texture.height));

                // Set the color of the pixel in the resized texture
                resizedTexture.SetPixel(i, y, color);
            }
        }

        // Apply changes to the resized texture
        resizedTexture.Apply();
        return resizedTexture;
    }

    // Method to rotate a texture by a certain degree
    private Texture2D RotateTexture(Texture2D texture, Grad rotateXDegrees)
    {
        // Check if rotation is needed
        if (rotateXDegrees != 0)
        {
            Debugger("### Rotate Texture ###");

            // Iterate through the rotation of 90 degrees as needed
            for (int x = (int)rotateXDegrees; x > 0; x--)
            {
                // Get the original pixels from the texture
                Color32[] originalPixels = texture.GetPixels32();
                Color32[] rotatedPixels = new Color32[originalPixels.Length];

                // Loop through all pixels
                for (int i = 0; i < texture.width; ++i)
                {
                    for (int y = 0; y < texture.height; ++y)
                    {
                        // Rotate each pixel by 90 degrees and save it at the new position
                        rotatedPixels[(y + 1) * texture.width - i - 1] = originalPixels[originalPixels.Length - 1 - (i * texture.height + y)];
                    }
                }

                // Set the rotated pixels to the new texture
                texture.SetPixels32(rotatedPixels);
                texture.Apply();
            }

            Debugger($"Texture was rotated.");
        }
        return texture;
    }

    // Method to mirror a texture horizontally
    private Texture2D MirrorTexture(Texture2D texture, bool mirrorTexture)
    {
        // Check if mirroring is needed
        if (!mirrorTexture) { return texture; }
        Debugger("### Mirror Texture ###");

        // Get the original pixels from the texture
        Color[] originalPixels = texture.GetPixels();
        Color[] mirroredPixels = new Color[originalPixels.Length];

        // Loop through all pixels
        for (int i = 0; i < texture.width; i++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                // Mirror each pixel and save it at the new position
                mirroredPixels[i * texture.width + texture.width - y - 1] = originalPixels[i * texture.width + y];
            }
        }

        // Set the mirrored pixels to the new texture
        texture.SetPixels(mirroredPixels);
        texture.Apply();


        Debugger("Texture was mirrored.");
        return texture;
    }

    // Method to insert objects into the Terrain based on an object map
    private Tuple<bool, Texture2D> InsertObject(Terrain terrain, Texture2D terrainTexture, List<Element> naturalObjects, int start_i, int start_y, int tileSize, Texture2D objectMaptexture)
    {
        // Dictionary to count the occurrences of colors within a tile
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
        bool wasTerrainTextureChanged = false;

        // Loop through the given tile and count which color appears the most
        for (int i = start_i; i < Mathf.Min(start_i + tileSize, objectMaptexture.width); i++)
        {
            for (int y = start_y; y < Mathf.Min(start_y + tileSize, objectMaptexture.height); y++)
            {
                // Get the color of each pixel within the tile
                Color pixelColor = objectMaptexture.GetPixel(start_y + y, start_i + i);

                // Update the color count in the dictionary
                if (colorCount.ContainsKey(pixelColor))
                {
                    colorCount[pixelColor]++;
                }
                else
                {
                    colorCount[pixelColor] = 1;
                }
            }
        }
        // Get the color that appears the most in the tile
        var mainColor = colorCount.OrderByDescending(count => count.Value).FirstOrDefault().Key;

        // Iterate through each natural object to check for a match with the main color
        foreach (Element naturalObject in naturalObjects)
        {
            if (IsEqualColor(mainColor, naturalObject.mappedColor, naturalObject.colorTolerance))
            {
                // Check if the object has as a prefab to be instantiated
                if (naturalObject.usePrefab)
                {
                    // Create and name the object
                    GameObject natObject = Instantiate(naturalObject.prefab, transform);
                    natObject.name = "GeneratedObjectTerraGen_" + naturalObject.name;

                    // Choose a random position within the tile
                    int v = UnityEngine.Random.Range(start_i, start_i + tileSize);
                    int w = UnityEngine.Random.Range(start_y, start_y + tileSize);

                    // Set the position of the object and adjust its height based on the terrain
                    natObject.transform.position = new Vector3(v, terrain.terrainData.GetHeight(v, w), w);
                }

                // Check if the object has as a texture to be applied
                if (naturalObject.useTexture)
                {
                    // Apply a brush stroke to the terrain texture using the natural object's texture
                    terrainTexture = ApplyBrushStroke(new Vector2(start_i, start_y), tileSize, terrainTexture, naturalObject.texture);
                    wasTerrainTextureChanged = true;
                }
            }
        }

        // Return a tuple indicating whether the terrain texture was changed and the updated texture
        return Tuple.Create(wasTerrainTextureChanged, terrainTexture);
    }

    // Method to apply a brush stroke to a texture
    private Texture2D ApplyBrushStroke(Vector2 position, int radius, Texture2D mainTexture, Texture2D drawTexture)
    {
        // Convert the position to pixel coordinates
        int centerX = Mathf.RoundToInt(position.x);
        int centerY = Mathf.RoundToInt(position.y);

        // Iterate through the pixels within the specified radius
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                // Check if the current pixel is within the circle
                if (Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2) <= Mathf.Pow(radius, 2))
                {
                    // Without Blending
                    //mainTexture.SetPixel(x, y, drawTexture.GetPixel(x - (centerX - radius), y - (centerY - radius)));

                    // Get the color of the pixel from the drawTexture
                    Color drawColor = drawTexture.GetPixel(x - (centerX - radius), y - (centerY - radius));

                    // Blend the colors and set the result in the main texture
                    Color mainColor = mainTexture.GetPixel(x, y);
                    Color blendedColor = Color.Lerp(mainColor, drawColor, 1);
                    mainTexture.SetPixel(x, y, blendedColor);
                }
            }
        }

        // Apply changes and return the resulting texture
        mainTexture.Apply();
        return mainTexture;
    }

    // Method to check if two colors are equal within a tolerance
    private bool IsEqualColor(Color color1, Color color2, float tolerance)
    {
        return Vector4.SqrMagnitude((Vector4)color1 - (Vector4)color2) < tolerance;
    }

    // Debugging Tool to save a texture
    public static void SaveTextureToPNG(Texture2D texture, string filePath)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Texture saved to: " + filePath);
    }
}