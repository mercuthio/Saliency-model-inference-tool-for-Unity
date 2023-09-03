#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Rendering;
using System.Diagnostics;
using UnityEditor.Media;
using Unity.Collections;
using System.Collections;
using System.Linq;

[ExecuteInEditMode]
public class Salience : MonoBehaviour
{
    // Configuration variables
    public static int width = 1024;
    public static int height = 512;                             // resolution of the pictures and videos
    public static string path = "";                             // path where the pictures will be saved
    public static string assetsPath = "";                       // path to the assets folder (used by the threads that cannot get to Application.datapath)
    public static int fps = 8;                                  // framerate of the videos
    public static int model_picture = 1;                        // model that will be used to process the salience of the pictures
    public static int model_video = 1;                          // model that will be used to process the salience of the videos
    public static int RTsalience_active = 0;                    // Indicates whether the real time salience is active
    public static int createSphereSalience = 1;                 // Indicates whether the sphere with the salience is created after calculating the salience of a picture
    public static int reproduceSalienceVideo = 1;               // Indicates whether the salience video will be reproduces automatically after generating the salience
    public static int useGpu = 0;                               // Indicates whether GPU will be used in salience prediction

    // Video variables
    public static Salience instance;                        // Instance for coroutine executions
    public static bool stopCoroutine = false;
    public static bool recording = false;

    // Conection with python variables
    public static SocketFloat socket = new SocketFloat { };     
    public static PythonRunner pyRunner = new PythonRunner { }; // Instance of PythonRunner class to connect the model with unity

    // Real time salience variables
    public static int numThreads = 1;

    static Salience()
    {
        getPreferences();
        UpdateChecks();
    }

    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        getPreferences();

        // If game is stopped executes OnGameQuit
        EditorApplication.playModeStateChanged += OnGameQuit;
        EditorApplication.update += OnEditorUpdate;

        // Find window when changed to game mode and RT salience is active
        if (RTsalience_active == 1)
            RealTimeSalienceWindow.GetPreviousWindow();
    }

    private static void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying) // Exiting play mode
        {
            if (RTsalience_active == 1)
                RealTimeSalienceWindow.GetPreviousWindow();

            EditorApplication.update -= OnEditorUpdate;
        }

    }

    // Called when changing to editor or game mode
    public static void OnGameQuit(PlayModeStateChange stateChange)
    {
        if (stateChange == PlayModeStateChange.ExitingPlayMode)
        {
            // Finish exectution of the coroutine in case the game mode is changed to editor mode
            stopCoroutine = true;
            savePreferences();
        }  
        else if (stateChange == PlayModeStateChange.ExitingEditMode)
        {
            savePreferences();
        }
    }

    // Creates an Instance to execute a static function with a coroutine
    public static void SetInstance()
    {
        if (!GameObject.Find("CoroutineHandler"))
        {
            GameObject _ = new GameObject("CoroutineHandler");
        }

        if (instance == null)
        {
            GameObject coroutineObject = GameObject.Find("CoroutineHandler");
            instance = coroutineObject.AddComponent<Salience>();
        }
    }

    // ---------------------------------- PREFERENCES -----------------------------------

    public static void savePreferences()
    {
        PlayerPrefs.SetString("path", path);
        PlayerPrefs.SetInt("width", width);
        PlayerPrefs.SetInt("height", height);
        PlayerPrefs.SetInt("fps", fps);
        PlayerPrefs.SetInt("model_picture", model_picture);
        PlayerPrefs.SetInt("model_video", model_video);
        PlayerPrefs.SetInt("RTsalience_active", RTsalience_active);
        PlayerPrefs.SetInt("numThreads", numThreads);
        PlayerPrefs.SetInt("showSphereSalience", createSphereSalience);
        PlayerPrefs.SetInt("reproduceSalienceVideo", reproduceSalienceVideo);
        PlayerPrefs.SetInt("useGpu", useGpu);
        PlayerPrefs.Save();
        UpdateChecks();
    }

    public static void getPreferences()
    {
        assetsPath = Application.dataPath;

        if (PlayerPrefs.HasKey("path"))
        {
            path = PlayerPrefs.GetString("path");
        }
        if (PlayerPrefs.HasKey("width"))
        {
            width = PlayerPrefs.GetInt("width");
        }
        if (PlayerPrefs.HasKey("height"))
        {
            height = PlayerPrefs.GetInt("height");
        }
        if (PlayerPrefs.HasKey("fps"))
        {
            fps = PlayerPrefs.GetInt("fps");
        }
        if (PlayerPrefs.HasKey("model_picture"))
        {
            model_picture = PlayerPrefs.GetInt("model_picture");
        }
        if (PlayerPrefs.HasKey("model_video"))
        {
            model_video = PlayerPrefs.GetInt("model_video");
        }
        if (PlayerPrefs.HasKey("RTsalience_active"))
        {
            RTsalience_active = PlayerPrefs.GetInt("RTsalience_active");               
        }
        if (PlayerPrefs.HasKey("numThreads"))
        {
            numThreads = PlayerPrefs.GetInt("numThreads");
        }
        if (PlayerPrefs.HasKey("showSphereSalience"))
        {
            createSphereSalience = PlayerPrefs.GetInt("showSphereSalience");
        }
        if (PlayerPrefs.HasKey("reproduceSalienceVideo"))
        {
            reproduceSalienceVideo = PlayerPrefs.GetInt("reproduceSalienceVideo");
        }
        if (PlayerPrefs.HasKey("useGpu"))
        {
            useGpu = PlayerPrefs.GetInt("useGpu");
        }
        UpdateChecks();

    }

    public static void UpdateChecks()
    {
        switch (width)
        {
            case 1024:
                CheckResolution(true, false);
                break;
            case 2048:
                CheckResolution(false, true);
                break;
        }

        switch (fps)
        {
            case 8:
                CheckFps(true, false, false);
                break;
            case 30:
                CheckFps(false, true, false);
                break;
            case 45:
                CheckFps(false, false, true);
                break;
        }

        switch (model_video)
        {
            case 1:
                CheckMoVideo(true);
                break;
        }

        switch (model_picture)
        {
            case 1:
                CheckMoPicture(true);
                break;
        }

        if (RTsalience_active == 1)
            CheckRTsalience(true);
        else
            CheckRTsalience(false);

        switch (numThreads)
        {
            case 1:
                CheckNumthreads(true, false, false);
                break;
            case 2:
                CheckNumthreads(false, true, false);
                break;
            case 3:
                CheckNumthreads(false, false, true);
                break;
        }

        if (createSphereSalience == 1)
            CheckCreateSalienceSphere(true);
        else
            CheckCreateSalienceSphere(false);

        if (reproduceSalienceVideo == 1)
            CheckReproduceSalienceVideo(true);
        else
            CheckReproduceSalienceVideo(false);

        if (useGpu == 1)
            CheckUseGpu(true);
        else
            CheckUseGpu(false);
    }

    public static void CheckResolution(bool _1024, bool _2048)
    {
        Menu.SetChecked("Saliency/Configuration/Resolution/1024x512", _1024);
        Menu.SetChecked("Saliency/Configuration/Resolution/2048x1024", _2048);
    }

    public static void CheckFps(bool _8, bool _30, bool _45)
    {
        Menu.SetChecked("Saliency/Configuration/Video fps/8 fps", _8);
        Menu.SetChecked("Saliency/Configuration/Video fps/30 fps", _30);
        Menu.SetChecked("Saliency/Configuration/Video fps/45 fps", _45);
    }

    public static void CheckMoVideo(bool SST)
    {
        Menu.SetChecked("Saliency/Configuration/Video model/SST-sal", SST);
    }

    public static void CheckMoPicture(bool CNN)
    {
        Menu.SetChecked("Saliency/Configuration/Picture model/CNN-360", CNN);
    }

    public static void CheckRTsalience(bool RT)
    {
        Menu.SetChecked("Saliency/Process Saliency/Interactive Salience", RT);
    }
    
    public static void CheckNumthreads(bool _1, bool _2, bool _3)
    {
        Menu.SetChecked("Saliency/Configuration/Interactive Threads/1 thread", _1);
        Menu.SetChecked("Saliency/Configuration/Interactive Threads/2 threads", _2);
        Menu.SetChecked("Saliency/Configuration/Interactive Threads/3 threads", _3);
    }

    public static void CheckCreateSalienceSphere(bool Show)
    {
        Menu.SetChecked("Saliency/Configuration/Create Saliency Sphere", Show);
    }

    public static void CheckReproduceSalienceVideo(bool Repr)
    {
        Menu.SetChecked("Saliency/Configuration/Reproduce Saliency Video", Repr);
    }

    public static void CheckUseGpu(bool gpu)
    {
        Menu.SetChecked("Saliency/Configuration/Use GPU", gpu);
    }

    // ---------------------------------- CONFIGURATION ------------------------------------

    [MenuItem("Saliency/Configuration/Saving directory", priority = 0)]
    public static void ChooseDirectory()
    {
        // Opens the file explorer to obtain the path to save the image
        string initialPath = Application.dataPath;
        path = EditorUtility.OpenFolderPanel("Choose a path to save the pictures", initialPath, "");
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Resolution/1024x512", priority = 51)]
    public static void chooseRes_1024x512()
    {
        width = 1024;
        height = 512;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Resolution/2048x1024", priority = 52)]
    public static void chooseRes_2048x1024()
    {
        width = 2048;
        height = 1024;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Video fps/8 fps", priority = 53)]
    public static void ChooseFps_8()
    {
        fps = 8;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Video fps/30 fps", priority = 54)]
    public static void ChooseFps_30()
    {
        fps = 30;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Video fps/45 fps", priority = 55)]
    public static void ChooseFps_45()
    {
        fps = 45;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Interactive Threads/1 thread", priority = 58)]
    public static void ChooseThreads1()
    {
        numThreads = 1;
        if (RTsalience_active == 1)
            RealTimeSalienceWindow.window.UpdateThreads();
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Interactive Threads/2 threads", priority = 59)]
    public static void ChooseThreads2()
    {
        numThreads = 2;
        if (RTsalience_active == 1)
            RealTimeSalienceWindow.window.UpdateThreads();
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Interactive Threads/3 threads", priority = 60)]
    public static void ChooseThreads3()
    {
        numThreads = 3;
        if (RTsalience_active == 1)
            RealTimeSalienceWindow.window.UpdateThreads();
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Create Saliency Sphere", priority = 61)]
    public static void ChooseShowSalienceSphere()
    {
        createSphereSalience = createSphereSalience == 0 ? 1 : 0;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Reproduce Saliency Video", priority = 62)]
    public static void ChooseReproduceSalienceVideo()
    {
        reproduceSalienceVideo = reproduceSalienceVideo == 0 ? 1 : 0;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Use GPU", priority = 63)]
    public static void ChooseUseGpu()
    {
        useGpu = useGpu == 0 ? 1 : 0;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Video model/SST-sal", priority = 100)]
    public static void ChooseMoVideo_SSTsal()
    {
        model_video = 1;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Picture model/CNN-360", priority = 101)]
    public static void ChooseMoPicture_CNN360()
    {
        model_picture = 1;
        savePreferences();
    }

    [MenuItem("Saliency/Configuration/Restart Configuration", priority = 151)]
    public static void RestartConfiguration()
    {
        width = 1024;
        height = 512;
        path = "";
        fps = 8;
        model_picture = 1; 
        model_video = 1; 
        RTsalience_active = 0;
        numThreads = 1;
        createSphereSalience = 1;
        reproduceSalienceVideo = 1;
        useGpu = 0;
        savePreferences();
    }

    // ---------------------------------- TAKE PICTURE --------------------------------------

    // Takes a panoramic picture from the main cammera view
    public static Texture2D take360screnshoot(string path, bool save)
    {
        RenderTexture cubemapLeftEye = new RenderTexture((int)width / 2, height, 24, RenderTextureFormat.Default);
        cubemapLeftEye.dimension = TextureDimension.Cube;
        RenderTexture equirect = new RenderTexture(width, height, 24, RenderTextureFormat.Default);

        // Renders cubeMap picture with all the faces
        Camera cam = Camera.main;
        cam.RenderToCubemap(cubemapLeftEye, 63, Camera.MonoOrStereoscopicEye.Mono);

        // Transforms the cubemap to equirectangular
        cubemapLeftEye.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Mono);

        // Converts texture to PNG
        Texture2D tex = new Texture2D(equirect.width, equirect.height);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        if (save)
        {
            byte[] bytes = tex.EncodeToPNG();

            // Saves image in given path
            File.WriteAllBytes(path, bytes);
        }

        return tex;

    }

    [MenuItem("Saliency/Take picture _F12", priority = 102)]
    public static void Take()
    {
        if (path == "")
        {
            ChooseDirectory();
        }

        if (string.IsNullOrEmpty(path))
        {
            print("Folder selection cancelled");
        }
        else
        {
            string filename = "Img_" + DateTime.Now.ToString("h'h'-m'm'-s's'") + ".png";
            string complete_path = path + "/" + filename;

            // Takes a picture and saves it in the given path
            take360screnshoot(complete_path, true);

            print("Picure saved in " + complete_path + ". Refresh to load image");
        }
    }

    // ---------------------------------- TAKE VIDEO ------------------------------------------

    // Executed by a coroutine creates a video
    public static IEnumerator TakeVideoCoroutine()
    {
        VP8EncoderAttributes vp8Attr = new VP8EncoderAttributes
        {
            keyframeDistance = 8
        };

        var videoAttr = new VideoTrackEncoderAttributes(vp8Attr)
        {
            frameRate = new MediaRational(fps),
            width = (uint)width,
            height = (uint)height
        };

        string filename = "Vid_" + DateTime.Now.ToString("h'h'-m'm'-s's'") + ".webm";
        string complete_path = path + "/" + filename;

        float frameTime = 1f / 20f;
        float time = 0f;

        print("Started Recording. Press F11 to finish recording");
        MediaEncoder encoder = new MediaEncoder(complete_path, videoAttr);
        while(true)
        {
            if (stopCoroutine)
            {
                stopCoroutine = false;
                break;
            }

            time += Time.deltaTime;

            if (time >= frameTime)
            {
                encoder.AddFrame(take360screnshoot("", false));
                time -= frameTime;
            }

            yield return null;
        }
        encoder.Dispose();
        recording = false;
        print("Video saved in " +  path + ". Refresh the window to see it.");
    }

    // Take Video is only available in game
    [MenuItem("Saliency/Take video (in game) _F11", true, priority = 101)]
    public static bool ValidateTakeVideo()
    {
        return Application.isPlaying;
    }

    [MenuItem("Saliency/Take video (in game) _F11", priority = 101)]
    public static void TakeVideo()
    {
        if (!recording)
        {
            recording = true;

            if (path == "")
            {
                ChooseDirectory();
            }

            if (string.IsNullOrEmpty(path))
            {
                print("Folder selection cancelled");
            }
            else
            {
                // Focus on game window
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                // Starts coroutine to record the video
                SetInstance();
                instance.StartCoroutine(TakeVideoCoroutine());
            }
        }
        else // Stop recording
        {
            stopCoroutine = true;
        }
    }

    // ---------------------------------- SALIENCY PREDICTION ----------------------------------

    // Sends a petition to the network, ans receives the path of the result
    public static string Send(string path, int param, int messType)
    {
        // Start python process
        pyRunner.Start();

        float[] floatArray = new float[path.Length + 2];

        // first component is 2 if directory prediction and 1 if single image
        floatArray[0] = messType;

        // Transforms the string to float[]
        for (int i = 0; i < path.Length; i++)
            floatArray[i + 1] = char.ToUpper(path[i]) - 'A' + 1;

        // Last component is the number of files
        floatArray[floatArray.Length - 1] = param;

        // Send request and receive path of the output
        floatArray = socket.ServerRequest(floatArray);

        string output_path = "";
        for (int i = 0; i < floatArray.Length; i++)
            output_path += (char)('A' + floatArray[i] - 1);

        return output_path;
    }

    [MenuItem("Saliency/Process Saliency/Image/One image", priority = 51)]
    public static void CalculateSalience_oneImage()
    {
        // Opens the file explorer to obtain the path to the image
        string image;
        if (path != "")
        {
            image = EditorUtility.OpenFilePanel("Choose a picture", path, "png,jpg");
        }
        else
        {
            string initialPath = Application.dataPath;
            image = EditorUtility.OpenFilePanel("Choose a picture", initialPath, "png,jpg");
        }

        if (string.IsNullOrEmpty(image))
        {
            print("Folder selection cancelled");
        }
        else
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            image = image.Replace('/', '\\');

            string folderPath = System.IO.Path.GetDirectoryName(image);
            string[] files = System.IO.Directory.GetFiles(folderPath);

            System.Collections.Generic.List<string> files_aux = new System.Collections.Generic.List<string>() { };
            for (int i = 0; i < files.Length; i++)
            {
                string ext = Path.GetExtension(files[i]);
                if (ext == ".png" || ext == ".jpg")
                    files_aux.Add(files[i]);

            }
            string[] files2 = files_aux.ToArray();
            
            // Obtains the position of the file in the directory
            int id = Array.IndexOf(files2, image);

            // Calculates the image saliency
            int output = pyRunner.ExecuteNoSocketsImage(1, folderPath + "\\\\", id, "", useGpu);
            
            stopwatch.Stop();
            showElapsedTime(stopwatch.Elapsed);

            string predicted_image = folderPath + "\\" + System.IO.Path.GetFileName(image) + "_predicted.png";
            if (output != 0)
                print("Error installing model dependencies");

            if (File.Exists(folderPath + "/Error_GPU.txt"))
            {
                print("ERROR. CUDA version not compatible (CUDA 11.1 or above)");
                File.Delete(folderPath + "/Error_GPU.txt");
                return;
            }
            else if (createSphereSalience == 1)
            {
                ShowSalience(image, predicted_image);
            }

            Texture2D texture_image = new Texture2D(width, height);
            Texture2D texture_image_predicted = new Texture2D(width, height);

            byte[] imageData = System.IO.File.ReadAllBytes(image);
            byte[] predImageData = System.IO.File.ReadAllBytes(predicted_image);

            texture_image.LoadImage(imageData);
            texture_image_predicted.LoadImage(predImageData);

            texture_image_predicted = MixSalience(texture_image, texture_image_predicted);

            imageData = texture_image_predicted.EncodeToPNG();

            File.WriteAllBytes(predicted_image, imageData);

        }
    }

    [MenuItem("Saliency/Process Saliency/Image/Directory", priority = 52)]
    public static void CalculateSalience_directoryImage()
    {
        // Opens the file explorer to obtain the path to the directory
        string folder;
        if (path != "")
        {
            folder = EditorUtility.OpenFolderPanel("Choose a directory", path, "");
        }
        else
        {
            string initialPath = Application.dataPath;
            folder = EditorUtility.OpenFolderPanel("Choose a directory", initialPath, "");
        }

        if (string.IsNullOrEmpty(folder))
        {
            print("Folder selection cancelled");
        }
        else
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string[] files = System.IO.Directory.GetFiles(folder);

            int num_files = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string ext = Path.GetExtension(files[i]);
                if (ext == ".png" || ext == ".jpg")
                    num_files += 1;
            }

            // Calculates the image saliency of all images from directory folder
            int output = pyRunner.ExecuteNoSocketsImage(2, folder + "/", num_files, "", useGpu);

            stopwatch.Stop();
            showElapsedTime(stopwatch.Elapsed);

            if (output == 1)
                print("Error installing model dependencies");

            if (File.Exists(folder + "/Error_GPU.txt"))
            {
                print("ERROR. CUDA version not compatible (CUDA 11.1 or above)");
                File.Delete(folder + "/Error_GPU.txt");
            }

            Texture2D texture_image = new Texture2D(width, height);
            Texture2D texture_image_predicted = new Texture2D(width, height);
            Texture2D aux = new Texture2D(width, height);
            byte[] imageData; byte[] predImageData;

            string[] result_files = System.IO.Directory.GetFiles(folder);
            for (int i = 0; i < result_files.Length; i++)
            {
                for (int e = 0; e < files.Length; e++)
                {
                    if (files[e] + "_predicted.jpg" == result_files[i])
                    {
                        imageData = System.IO.File.ReadAllBytes(files[e]);
                        predImageData = System.IO.File.ReadAllBytes(result_files[i]);

                        texture_image.LoadImage(imageData);
                        texture_image_predicted.LoadImage(predImageData);

                        aux = MixSalience(texture_image, texture_image_predicted);

                        imageData = aux.EncodeToPNG();
                        File.WriteAllBytes(result_files[i], imageData);

                        break;
                    }
                }
            }

        }
    }

    [MenuItem("Saliency/Process Saliency/Video/One video", priority = 53)]
    public static void CalculateSalience_oneVideo()
    {
        // Opens the file explorer to obtain the path to the image
        string video;
        if (path != "")
        {
            video = EditorUtility.OpenFilePanel("Choose a video", path, "webm,mp4");
        }
        else
        {
            string initialPath = Application.dataPath;
            video = EditorUtility.OpenFilePanel("Choose a video", initialPath, "webm,mp4");
        }

        if (string.IsNullOrEmpty(video))
        {
            print("Folder selection cancelled");
        }
        else
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            video = video.Replace('/', '\\');

            // Calculates the video saliency
            int output = pyRunner.ExecuteNoSocketsVideo(video, 1, useGpu);

            stopwatch.Stop();
            showElapsedTime(stopwatch.Elapsed);

            if (output == 0)
            {
                string folder = System.IO.Path.GetDirectoryName(video.Replace('\\', '/'));
                if (File.Exists(folder + "/Error.txt"))
                {
                    print("ERROR. The video is too short.");
                    File.Delete(folder + "/Error.txt");
                }
                else if (File.Exists(folder + "/Error_GPU.txt"))
                {
                    print("ERROR. CUDA version not compatible (CUDA 11.1 or above)");
                    File.Delete(folder + "/Error_GPU.txt");
                }
                else if (reproduceSalienceVideo == 1)
                    Application.OpenURL(video + "_predicted.avi"); // Opens the generated video in the system's video player
            }
            else
                print("Error installing model dependencies");
        }
    }

    [MenuItem("Saliency/Process Saliency/Video/Directory", priority = 54)]
    public static void CalculateSalience_directoryVideo()
    {
        // Opens the file explorer to obtain the path to the directory
        string folder;
        if (path != "")
        {
            folder = EditorUtility.OpenFolderPanel("Choose a directory", path, "");
        }
        else
        {
            string initialPath = Application.dataPath;
            folder = EditorUtility.OpenFolderPanel("Choose a directory", initialPath, "");
        }

        if (string.IsNullOrEmpty(folder))
        {
            print("Folder selection cancelled");
        }
        else
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            folder = folder.Replace('/', '\\');

            // Calculates the video saliency
            int output = pyRunner.ExecuteNoSocketsVideo(folder, 2, useGpu);

            stopwatch.Stop();
            showElapsedTime(stopwatch.Elapsed);

            if (output == 0)
            {
                string folder_aux = folder.Replace('\\', '/');
                if (File.Exists(folder + "/Error.txt"))
                {
                    print("ERROR. At leat one of the videos is too short.");
                    File.Delete(folder + "/Error.txt");

                }
            }
            else
                print("Error installing model dependencies");

        }
    }

    // ---------------------------------- SHOW SALIENCE --------------------------------------
    
    // Creates a sphere in the cammera and shows the original image and the salience in heatmap colors
    public static void ShowSalience(string imagePath, string imagePath2)
    {
        // Creates the sphere in the camera position
        Vector3 cameraPosition = Camera.main.transform.position;
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = cameraPosition;
        sphere.transform.localScale = new Vector3(2f, 2f, 2f);

        // Creates a material with the SalienceSphere shader
        Material salienceMaterial = new Material(Shader.Find("Hidden/SalienceSphere"));
        Renderer sphereRenderer = sphere.GetComponent<Renderer>();

        // Reads both images
        Texture2D Texture = ReadImage(imagePath);
        Texture2D SecTexture = ReadImage(imagePath2);

        // Assigns both textures to the shader
        salienceMaterial.SetTexture("_MainTex", Texture);
        salienceMaterial.SetTexture("_SecondTex", SecTexture);
        sphereRenderer.material = salienceMaterial;

        print("Created salience sphere in the camera.");
    }

    // Auxiliar function to read an image given a path
    public static Texture2D ReadImage(string imagePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        return texture;
    }

    // ----------------------------------- REAL TIME SALIENCE --------------------------------

    [MenuItem("Saliency/Interactive Saliency _F10", priority = 100)]
    public static void RTsalience()
    {
        if (RTsalience_active == 1)
            RealTimeSalienceWindow.CloseWindow(); // onDestroy sets RTsalience_active to 0
        else
        {
            if (RealTimeSalienceWindow.ShowWIndow() == 0)
                RTsalience_active = 1;
        }
        savePreferences();
    }

    // ----------------------------------- AUXILIAR FUNCTIONS--------------------------------

    public static void showElapsedTime(TimeSpan ts)
    {
        string format_time = string.Format("{0} min {1} s {2} ms", ts.Minutes, ts.Seconds, ts.Milliseconds);
        print("Time: " + format_time);
    }

    // Returns a texture with a mix of the original image and his salience in heat map format
    public static Texture2D MixSalience(Texture2D texture_image, Texture2D texture_image_predicted)
    {
        // Salience image's resolution is lower than original image one
        texture_image_predicted = Resize(texture_image_predicted, texture_image.width, texture_image.height);
        Color32[] pixels_orig = texture_image.GetPixels32();
        Color32[] pixels_pred = texture_image_predicted.GetPixels32();

        for (int i = 0; i < pixels_pred.Length; i++)
        {
            pixels_pred[i] = GrayToColorMap(pixels_pred[i]);

            // Lerp between two images, avoids creating multiple instances of Color32 by directly updating the channels
            byte r = (byte)Mathf.Lerp(pixels_orig[i].r, pixels_pred[i].r, 0.25f);
            byte g = (byte)Mathf.Lerp(pixels_orig[i].g, pixels_pred[i].g, 0.25f);
            byte b = (byte)Mathf.Lerp(pixels_orig[i].b, pixels_pred[i].b, 0.25f);
            byte a = (byte)Mathf.Lerp(pixels_orig[i].a, pixels_pred[i].a, 0.25f);

            pixels_pred[i] = new Color32(r, g, b, a);
        }

        texture_image_predicted.SetPixels32(pixels_pred);
        texture_image_predicted.Apply();

        return texture_image_predicted;

    }

    //Source: https://stackoverflow.com/questions/56949217/how-to-resize-a-texture2d-using-height-and-width
    public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24);
        RenderTexture.active = rt;

        // Resizes texture2D to rt size
        Graphics.Blit(texture2D, rt);

        // Fills result with the active renderTexture rt
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        return result;
    }

    public static Color GrayToColorMap(Color pixel)
    {
        float t = pixel.r;
        float exp = 6.0f;
        Color result;

        if (t <= 0.2f)
            result = Color.Lerp(Color.blue, Color.cyan, Mathf.Pow(t / 0.2f, exp));
        else if (t <= 0.4f)
            result = Color.Lerp(Color.cyan, Color.green, Mathf.Pow((t - 0.2f) / 0.2f, exp));
        else if (t <= 0.6f)
            result = Color.Lerp(Color.green, Color.yellow, Mathf.Pow((t - 0.4f) / 0.2f, exp));
        else if (t <= 0.75f)
            result = Color.Lerp(Color.yellow, new Color(1, 0.5f, 0), Mathf.Pow((t - 0.6f) / 0.2f, exp));
        else
            result = Color.Lerp(new Color(1, 0.5f, 0), Color.red, Mathf.Pow((t - 0.75f) / 0.2f, exp));

        return result;
    }

    public static void ShowError(string error)
    {
        print("ERROR." +  error);
    }

    public static string GetAssetsPath()
    {
        return Application.dataPath;
    }


}
#endif