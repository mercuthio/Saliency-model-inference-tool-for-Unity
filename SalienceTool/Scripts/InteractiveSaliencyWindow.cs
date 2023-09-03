using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

public class RealTimeSalienceWindow : EditorWindow
{
    Camera cam;
    public RenderTexture cubemapLeftEye;
    public RenderTexture equirect;
    public Texture2D texture;
    public static RealTimeSalienceWindow window;

    public List<Thread> threads;
    static object lockObject = new object();
    public static int numThreads;
    public static bool stopThreads;                         // Stops the threads
    public bool newImage = false;                           // Indicates the threads if there is an image to process
    public int dispThreads = 0;                             // Indicates the number of available threads
    public int threadsToStop = 0;                           // Indicates how many threads have to stop executing
    public bool finishExecution = false;                    // Used by the threads to finish the program

    public byte[] bytes_original_image;                     // Used by the threads, image that was processed by the thread that just ended
    public byte[] bytes_predicted_image;                    // Used by the threads, generated image by the thread

    // -------------------------------- CREATE WINDOW FUNCTIONS -----------------------------------------

    public static int ShowWIndow()
    {
        if (window == null)
        {
            window = GetWindow<RealTimeSalienceWindow>("Interactive salience", true);
            window.minSize = new Vector2(1024, 512);
            window.maxSize = new Vector2(1024, 512);
            return window.Initialize();
        }
        return 1;
    }

    // In case the mode changed (window is null) obtains the window previously created
    public static void GetPreviousWindow()
    {
        RealTimeSalienceWindow[] windows = Resources.FindObjectsOfTypeAll<RealTimeSalienceWindow>();
        foreach (RealTimeSalienceWindow win in windows)
        {
            if (win != null && win.titleContent.text == "Interactive salience")
            {
                // Kill previous threads
                stopThreads = true;
                Thread.Sleep(2600);

                window = win;
                window.Initialize();
                break;
            }
        }
    }

    public int Initialize()
    {
        if (Salience.path == "")
            Salience.ChooseDirectory();

        if (Salience.path == "")
        {
            // Folder selection cancelled
            window.Close();
            return 1;
        }
        else
        {
            cam = Camera.main;

            cubemapLeftEye = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Default);
            cubemapLeftEye.dimension = TextureDimension.Cube;
            equirect = new RenderTexture(1024, 512, 24, RenderTextureFormat.Default);
            texture = new Texture2D(equirect.width, equirect.height);

            // execute UpdateWindow on every Update
            EditorApplication.update += UpdateWindow;

            // Starts the salience threads
            stopThreads = false;
            numThreads = Salience.numThreads;
            dispThreads = numThreads;
            threads = new List<Thread>();
            string timeSTamp;
            for (int i = 0; i < numThreads; i++)
            {
                timeSTamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                Thread thread = new Thread(() => threadSalience(Salience.path + "/RT_Images_Folder" + "\\\\", timeSTamp));
                threads.Add(thread);
                thread.Start();

                AddNewImage();
                Thread.Sleep(400);
            }
        }
        return 0;
    }

    // ---------------------------------- UPDATE WINDOW FUNCTIONS ---------------------------------------------

    public void threadSalience(string folder, string id)
    {
        byte[] original;
        while (!stopThreads)
        {
            if (newImage)
            {

                // Saves the original image
                lock (lockObject)
                {
                    newImage = false;
                    dispThreads--;
                    original = File.ReadAllBytes(Salience.path + "/RT_Images_Folder/RT_Img.png");
                }

                // Calculates salience
                int output = Salience.pyRunner.ExecuteNoSocketsImage(1, folder, 0, "z_" + id, Salience.useGpu);

                if (File.Exists(Salience.path + "/RT_Images_Folder/" + "Error_GPU.txt"))
                {
                    Salience.ShowError("CUDA version not compatible (CUDA 11.1 or above)");
                    finishExecution = true;
                }

                if (output != 0)
                {
                    Salience.ShowError("Error installing model dependencies");
                    finishExecution = true;
                }

                // Updates the frame
                lock (lockObject)
                {
                    // Updates the bytes of the images
                    bytes_original_image = original;

                    // In case threads directory is eliminated and threads are finishing execution
                    if (File.Exists(Salience.path + "/RT_Images_Folder" + "\\" + "z_" + id + ".png"))
                        bytes_predicted_image = File.ReadAllBytes(Salience.path + "/RT_Images_Folder" + "\\" + "z_" + id + ".png");

                    dispThreads++;
                }

            }

            lock (lockObject)
            {
                if (threadsToStop > 0)
                {
                    dispThreads--;
                    threadsToStop--;
                    break;
                }
            }
            Thread.Sleep(10);
        }
    }

    public void UpdateWindow()
    {
        if (finishExecution)
            Salience.RTsalience();

        if (dispThreads > 0)
        {
            // Asks for new image
            AddNewImage();

            // In the first iteration there will not be any predicted image
            if (bytes_predicted_image != null)
            {
                Texture2D texture_original = new Texture2D(texture.width, texture.height);
                Texture2D texture_pred = new Texture2D(texture.width, texture.height);

                lock (lockObject)
                {
                    texture_original.LoadImage(bytes_original_image);
                    texture_pred.LoadImage(bytes_predicted_image);
                }

                texture = Salience.MixSalience(texture_original, texture_pred);
                Repaint();
            }
        }
    }

    // Calculates salience of the variable texture
    public void AddNewImage()
    {
        // Takes picture
        texture.Reinitialize(equirect.width, equirect.height);
        cam.RenderToCubemap(cubemapLeftEye, 63, Camera.MonoOrStereoscopicEye.Mono);
        cubemapLeftEye.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Mono);
        
        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

        // Saves image
        byte[] bytes = texture.EncodeToPNG();

        // Creates folder for generated images in case it does not exist
        string folderPath = Salience.path + "/RT_Images_Folder";
        if (!Directory.Exists(folderPath) && !stopThreads)
            Directory.CreateDirectory(folderPath);

        // Saves the image
        if (!stopThreads)
            File.WriteAllBytes(folderPath + "/RT_Img.png", bytes);
        newImage = true;
    }

    public void OnGUI()
    {
        if (texture != null)
            GUI.DrawTexture(new Rect(0, 0, position.width, position.height), texture, ScaleMode.ScaleToFit);
    }

    public void UpdateThreads()
    {
        int newNumThreads = Salience.numThreads;
        if (newNumThreads > numThreads)
        {
            string timeStamp;
            for (int i = 0; i < newNumThreads - numThreads; i++)
            {
                timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                Thread thread = new Thread(() => threadSalience(Salience.path + "/RT_Images_Folder" + "\\\\", timeStamp));
                threads.Add(thread);
                thread.Start();
                AddNewImage();
                //Thread.Sleep(400);
            }
        }
        else if (numThreads > newNumThreads)
            threadsToStop = numThreads - newNumThreads;
        numThreads = newNumThreads;
    }

    // ------------------------------- FINISH WINDOW FUNCTIONS -------------------------------------------

    public void OnDestroy()
    {
        FinishExecution();
    }

    public void FinishExecution()
    {
        if (Directory.Exists(Salience.path + " /RT_Images_Folder"))
        {
            Directory.Delete(Salience.path + " /RT_Images_Folder", true);
            File.Delete(Salience.path + " /RT_Images_Folder.meta");
        }

        // Stops the threads
        stopThreads = true;

        // update value and check in menu
        Salience.RTsalience_active = 0;
        Salience.savePreferences();

        EditorApplication.update -= UpdateWindow;
    }

    public static void CloseWindow()
    {
        if (window != null)
        {
            window.Close();
        }
    }

}
