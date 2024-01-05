using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Unity.VisualScripting;
using UnityEditor;
using System.Linq;


/**************************************************************************
             Date Last Changed: 05.01.2024 - Version 2.2

Author: Frederick Van Bockryck, Jakob Schöllauf, Florian Waltersdorfer

TODO - The next few Lines should have a description of what the script does and 
how it works. I still have to do that.

**************************************************************************/
public class Generator : MonoBehaviour
{

    // This Class Enables A Better User Expierence In The Editor
    #if UNITY_EDITOR
    [CustomEditor(typeof(Generator))]
    public class GeneratorEditor : Editor
    {
        private SerializedProperty naturalObjects;

        private void OnEnable()
        {
            naturalObjects = serializedObject.FindProperty("naturalObjects");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            // Get The Attributes Of The Main Script Available
            Generator generator = (Generator)target;

            // This Is The Header
            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

            // Start Tracking Of Changes
            EditorGUI.BeginChangeCheck();

            // These Are The Toogles for Individual Setting Groups
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


            // These Are The Heightmap Settings
            if (generator.showHeightmapSettings)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // This Is The Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Heightmap Settings", EditorStyles.boldLabel);

                // These Are the Individual Settings
                EditorGUIUtility.labelWidth = 150;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.heightMapPath = EditorGUILayout.DelayedTextField("Path", generator.heightMapPath);
                generator.maximumHeight = EditorGUILayout.DelayedIntField("Maximum Height", generator.maximumHeight);
                generator.scaleHeight = EditorGUILayout.Slider("Scale Height", generator.scaleHeight, 0f, 1f);
                generator.invertHeight = EditorGUILayout.Toggle("Invert Height", generator.invertHeight);


                EditorGUI.indentLevel--;
            }

            // These Are The Texture Settings
            if (generator.useCustomTexture)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // This Is The Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);

                // These Are the Individual Settings
                EditorGUIUtility.labelWidth = 110;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.textureMapPath = EditorGUILayout.DelayedTextField("Path", generator.textureMapPath);
                generator.rotateXDegrees = (Grad)EditorGUILayout.EnumPopup("Rotate Texture", generator.rotateXDegrees);
                generator.mirrorTexture = EditorGUILayout.Toggle("Mirror Texture", generator.mirrorTexture);

                EditorGUI.indentLevel--;
            }

            // These Are The Material Settings
            if (generator.useCustomMaterial)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // This Is The Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);

                // These Are the Individual Settings
                EditorGUIUtility.labelWidth = 120;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.customMaterial = (Material)EditorGUILayout.ObjectField("Custom Material", generator.customMaterial, typeof(Material), true);

                EditorGUI.indentLevel--;
            }

            // These Are The Natural Objects Settings
            if (generator.useCustomObjects)
            {
                EditorGUILayout.Space(30);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

                // This Is The Header
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Natural Objects Settings", EditorStyles.boldLabel);     

                // These Are the Individual Settings
                EditorGUIUtility.labelWidth = 120;
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);
                generator.objectsMapPath = EditorGUILayout.DelayedTextField("Path For Map", generator.objectsMapPath);
                generator.objectDensity = EditorGUILayout.Slider("Object Density", generator.objectDensity, 0.10f, 5f);

                // Option To Collapse And Add Natural Objects
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

                        // Settings For Prefab
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("usePrefab"), new GUIContent("Use Prefab"));
                        if (element.FindPropertyRelative("usePrefab").boolValue)
                        {
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("prefab"), new GUIContent(""));
                        }
                        GUILayout.EndHorizontal();

                        // Settings For Texture
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

            // Save The State Of Serialized Objects
            serializedObject.ApplyModifiedProperties();

            // Check For Changes
            if (EditorGUI.EndChangeCheck() && generator.buildAfterChange)
            {
                generator.Generate();
            }

            // This Is The Credits
            EditorGUILayout.Space(50);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(300)), new Color(0.5f, 0.5f, 0.5f, 1));

            EditorGUILayout.Space(10);

            GUIStyle creditStyle = new GUIStyle(EditorStyles.label);
            creditStyle.fontSize = 10;

            // This Is The Header 
            creditStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Credits", creditStyle);

            // This Is The Main Body
            creditStyle.wordWrap = true;
            creditStyle.fontStyle = FontStyle.Italic;
            EditorGUILayout.LabelField("This script was created as part of a lecture at the University of Applied Sciences FH JOANNEUM. " +
                "The authors were Van Bockryck, Jakob Schöllauf, and Florian Waltersdorfer under the guidance of Dipl.-Päd. Gerhard Sprung, MSc.", creditStyle);

            EditorGUILayout.Space(20);
        }

        private void AddElement()
        {
            Generator script = (Generator)target;

            if (script.naturalObjects == null)
            {
                script.naturalObjects = new List<Generator.Element>();
            }

            script.naturalObjects.Add(new Generator.Element());
        }

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
    // All Neccessary Variables (The Have To Be Serialized Or Public To Keep Their Changed State In The Custom Inspector)
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


    // If Given Option Is Turned On Debug Messages Will Be Displayed
    private void Debugger(string text)
    {
        if (showDebugging)
        {
            Debug.Log(text);
        }
    }

    // The Normal Start Function That Gets Called the First Time The Script Gets Loaded
    private void Start()
    {
        Generate();
    }

    [ContextMenu("Generate Again")]
    void Generate()
    {
        Debugger("### Start Generating ###");

        // Destroys Old Components
        DestroyImmediate(GetComponent<Terrain>());
        DestroyImmediate(GetComponent<TerrainCollider>());

        // Destroy All Natural Objects
        foreach(Transform tmp in transform) { Destroy(tmp.gameObject); };

        // Generates Everything
        Handler(heightMapPath, maximumHeight, invertHeight, scaleHeight, rotateXDegrees, mirrorTexture, useCustomMaterial,
            customMaterial, useCustomTexture, textureMapPath, useCustomObjects, naturalObjects, objectsMapPath, objectDensity);

        Debugger("Finished Generating");
    }

    // This Method Handles The Complete Generation And Can Be Called Manually From Another Script With Every Setting Available
    public void Handler(string heightMapPath, int maximumHeight = 300, bool invertHeight = false, float scaleHeight = 1, Grad rotateXDegrees = 0, bool mirrorTexture = false,
        bool useCustomMaterial = false, Material customMaterial = null, bool useCustomTexture = false, string textureMapPath = "",
        bool useCustomObjects = false, List<Element> naturalObjects = null,  string objectsMapPath = "", float objectDensity = 0.25f)
    {
        try
        {
            Texture2D heightmap = LoadHeightmap(heightMapPath);
            Terrain terrain = CreateTerrain(heightmap, maximumHeight);

            ApplyHeight(terrain, heightmap, invertHeight, scaleHeight);
            ApplyMaterial(terrain, useCustomMaterial, customMaterial);
            ApplyTexture(terrain, useCustomTexture, textureMapPath, rotateXDegrees, mirrorTexture);
            ApplyObjects(terrain, useCustomObjects, naturalObjects, objectsMapPath, objectDensity);

        }
        catch (Exception exception)
        {
            // Displays the Given Message In Case Of An Error
            Debugger(exception.Message);
        }
        
    }

    private Texture2D LoadHeightmap(string heightMapPath)
    {
        Debugger("### Load Heightmap ###");

        // Checks If Heightmap Path Is Empty
        if (heightMapPath == "")
        {
            throw new Exception("The Heightmap Path is empty. \nEnvironment can not be generated.");
        }

        // Checks If There Is a File At The Given Path
        if (!File.Exists(heightMapPath))
        {
            throw new Exception($"The Heightmap at the path \"{heightMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initilize Texture2D Object Of Our Heightmap (Height And Width Will Be Overwritten Later)
        Texture2D heightmap = new Texture2D(1, 1);

        // We Read In The Raw Bytes Of The Heightmap Image And Convert It To Be Able To Handle
        // Certain Aspects Better (Compression, Width, Height, Bit Depth, Etc.)
        heightmap.LoadImage(File.ReadAllBytes(heightMapPath));
        return MakeItSquare(heightmap, Mathf.NextPowerOfTwo(Mathf.Max(heightmap.width, heightmap.height)));
    }


    private Terrain CreateTerrain(Texture2D heightmap, int maximumHeight)
    {
        Debugger("### Create Terrain ###");

        // Create The Terrain Object
        Terrain terrain = gameObject.AddComponent<Terrain>();

        // Create Terrain Data
        TerrainData terrainData = new TerrainData();

        // Set The Terrain Attributes
        terrainData.heightmapResolution = heightmap.width + 1;
        terrainData.size = new Vector3(heightmap.width, maximumHeight, heightmap.height);

        // Assigns The Terrain Data Object
        terrain.terrainData = terrainData;

        
        return terrain;
    }

    private void ApplyHeight(Terrain terrain, Texture2D heightmap, bool invertHeight, float scaleHeight)
    {
        Debugger("### Apply Heightmap ###");

        // Initiliaze a 2D Array for the Heightdata
        float[,] data = new float[heightmap.width, heightmap.height];

        // Loops through all the Pixels in the Heightmap
        for (int i = 0; i < heightmap.width; i++)
        {
            for (int y = 0; y < heightmap.height; y++)
            {
                // Reads out the Value of the Heightmap at a Certain Pixel and 
                // puts it in to the Heightdata Array (Inverts the given Pixel if Neccesary)
                data[i, y] = Mathf.Clamp01((invertHeight ? (1 - heightmap.GetPixel(i, y).grayscale) :
                    (heightmap.GetPixel(i, y).grayscale)) * scaleHeight);
            }
        }
        // Assigns Height Data
        terrain.terrainData.SetHeights(0, 0, data);

        // Create TerrainCollider and Adapt it to the Current Terrain
        TerrainCollider collider = terrain.AddComponent<TerrainCollider>();
        collider.terrainData = terrain.terrainData;

        Debugger("Heightmap was applied.");
    }

    private void ApplyMaterial(Terrain terrain, bool useCustomMaterial, Material customMaterial)
    {
        Debugger("### Apply Material ###");

        // Checking for custom Materials
        if (useCustomMaterial)
        {
            if (customMaterial != null)
            {
                terrain.materialTemplate = customMaterial;
                return;
            }
            throw new Exception("The Custom Material is empty. \nTurn off Custom Material or provide Material.");
        }
        terrain.materialTemplate = new Material(Shader.Find("Specular"));

        Debugger("Material was applied.");
    }

    private void ApplyTexture(Terrain terrain, bool useCustomTexture, string textureMapPath, Grad rotateXDegrees, bool mirrorTexture)
    {
        Debugger("### Apply Texture ###");

        // Check If Custom Texture Is Enabled
        if (!useCustomTexture) { return; }

        // Checks If Texture Path Is Empty
        if (textureMapPath == "")
        {
            throw new Exception("The Texture Path is empty. \nEnvironment can not be generated.");
        }

        // Checks If There Is a File At The Given Path
        if (!File.Exists(textureMapPath))
        {
            throw new Exception($"The Texture at the path \"{textureMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initilize Texture2D Object Of Our Texture (Height And Width Will Be Overwritten Later)
        Texture2D texture = new Texture2D(1, 1);

        // We Read In The Raw Bytes Of The Heightmap Image And Convert It To Be Able To Handle
        // Certain Aspects Better (Compression, Width, Height, Bit Depth, Etc.)
        texture.LoadImage(File.ReadAllBytes(textureMapPath));

        terrain.materialTemplate.mainTexture = RotateTexture(MirrorTexture(MakeItSquare(texture, terrain.terrainData.heightmapResolution), mirrorTexture), rotateXDegrees);

        Debugger("Texture was applied.");
    }

    private void ApplyObjects(Terrain terrain, bool useCustomObjects, List<Element> naturalObjects, string objectsMapPath, float objectDensity)
    {
        Debugger("### Apply Objects ###");

        // Check If Custom Texture Is Enabled
        if (!useCustomObjects) { return; }

        // Checks If Texture Path Is Empty
        if (objectsMapPath == "")
        {
            throw new Exception("The Objects Path is empty. \nEnvironment can not be generated.");
        }

        // Checks If There Is a File At The Given Path
        if (!File.Exists(objectsMapPath))
        {
            throw new Exception($"The Texture at the path \"{objectsMapPath}\" does not exist. \nEnvironment can not be generated.");
        }

        // Initilize Texture2D Object Of Our Texture (Height And Width Will Be Overwritten Later)
        Texture2D objectMaptexture = new Texture2D(1, 1);

        // We Read In The Raw Bytes Of The Object Map Image And Convert It To Be Able To Handle
        // Certain Aspects Better (Compression, Width, Height, Bit Depth, Etc.)
        objectMaptexture.LoadImage(File.ReadAllBytes(objectsMapPath));
        objectMaptexture = RotateTexture(MirrorTexture(MakeItSquare(objectMaptexture, terrain.terrainData.heightmapResolution), mirrorTexture), rotateXDegrees);

        // Calculates The Tile Size Based On The Density
        int tileSize = (int)Math.Round(objectMaptexture.height / (100 * objectDensity));

        Texture2D terrainTexture = (Texture2D)terrain.materialTemplate.mainTexture;

        // Loop through all Pixels And Insert A given Object
        for (int i = 0; i < objectMaptexture.height; i += tileSize)
        {
            for (int y = 0; y < objectMaptexture.width; y += tileSize)
            {
                Tuple<bool, Texture2D> tmp = InsertObject(terrain, terrainTexture, naturalObjects, i, y, tileSize, objectMaptexture);
                if (tmp.Item1) { terrainTexture = tmp.Item2; }
            }
        }
        terrain.materialTemplate.mainTexture = terrainTexture;

        Debugger("Objects were applied.");
    }


    //Makes The Texture Square
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

    // Rotates the given Texture a certain Amount of Degrees
    private Texture2D RotateTexture(Texture2D texture, Grad rotateXDegrees)
    {
        if (rotateXDegrees != 0)
        {
            Debugger("### Rotate Texture ###");

            // Goes Through The Rotation Of 90 Degrees The Needed Amount
            for (int x = (int)rotateXDegrees; x > 0; x--)
            {
                Color32[] originalPixels = texture.GetPixels32();
                Color32[] rotatedPixels = new Color32[originalPixels.Length];

                // Loop through all Pixels
                for (int i = 0; i < texture.width; ++i)
                {
                    for (int y = 0; y < texture.height; ++y)
                    {
                        // Take a Pixel, Rotate it 90 degrees and Safes it at the new Position
                        rotatedPixels[(y + 1) * texture.width - i - 1] = originalPixels[originalPixels.Length - 1 - (i * texture.height + y)];
                    }
                }

                // Set the turned pixels to the new texture
                texture.SetPixels32(rotatedPixels);
                texture.Apply();
            }

            Debugger($"Texture was rotated.");
        }
        return texture;
    }

    private Texture2D MirrorTexture(Texture2D texture, bool mirrorTexture)
    {
        if (!mirrorTexture) { return texture; }
        Debugger("### Mirror Texture ###");

        Color[] originalPixels = texture.GetPixels();
        Color[] mirroredPixels = new Color[originalPixels.Length];

        // Loop through all Pixels
        for (int i = 0; i < texture.width; i++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                // Take a Pixel, Mirrors it and Safes it at the new Position
                mirroredPixels[i * texture.width + texture.width - y - 1] = originalPixels[i * texture.width + y];
            }
        }

        // Set the mirrored pixels to the new texture
        texture.SetPixels(mirroredPixels);
        texture.Apply();


        Debugger("Texture was mirrored.");
        return texture;
    }

    private Tuple<bool, Texture2D> InsertObject(Terrain terrain, Texture2D terrainTexture, List<Element> naturalObjects, int start_i, int start_y, int tileSize, Texture2D objectMaptexture)
    {
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
        bool wasTerrainTextureChanged = false;

        // Loop Through The Given Tile And Count Which Color Appears The Most
        for (int i = start_i; i < Mathf.Min(start_i + tileSize, objectMaptexture.width); i++)
        {
            for (int y = start_y; y < Mathf.Min(start_y + tileSize, objectMaptexture.height); y++)
            {
                Color pixelColor = objectMaptexture.GetPixel(start_y + y, start_i + i);

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
        var mainColor = colorCount.OrderByDescending(count => count.Value).FirstOrDefault().Key;


        foreach (Element naturalObject in naturalObjects)
        {
            if (IsEqualColor(mainColor, naturalObject.mappedColor, naturalObject.colorTolerance))
            {
                if (naturalObject.usePrefab)
                {
                    // Create An Name The Object
                    GameObject object = Instantiate(naturalObject.prefab, transform);
                    object.name = "GeneratedObjectTerraGen_" + naturalObject.name;

                    // Choose A Random Position Within Tile
                    int v = UnityEngine.Random.Range(start_i, start_i + tileSize);
                    int w = UnityEngine.Random.Range(start_y, start_y + tileSize);

                    // Insert Object In That Position And Give It The Right Height
                    object.transform.position = new Vector3(v, terrain.terrainData.GetHeight(v, w), w);
                }

                if (naturalObject.useTexture)
                {
                    terrainTexture = ApplyBrushStroke(new Vector2(start_i, start_y), tileSize, terrainTexture, naturalObject.texture);
                    wasTerrainTextureChanged = true;
                }
            }
        }

        return Tuple.Create(wasTerrainTextureChanged, terrainTexture);
    }

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


    private bool IsEqualColor(Color color1, Color color2, float tolerance)
    {
        return Vector4.SqrMagnitude((Vector4)color1 - (Vector4)color2) < tolerance;
    }

    // Debugging Tool
    public static void SaveTextureToPNG(Texture2D texture, string filePath)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Texture saved to: " + filePath);
    }
}