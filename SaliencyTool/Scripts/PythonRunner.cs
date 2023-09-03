using System.Diagnostics;
using System;
using UnityEngine;

public class PythonRunner
{
    public string pythonPath = "";
    public bool installedReq = false;

    public void getPythonPath()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("where", "python");
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        // First line is the python path
        string[] lines = output.Split('\n');
        pythonPath = lines[0].Trim();

    }

    public int installRequir()
    {
        string scriptFolderPath = Salience.assetsPath + "/SalienceTool";
        string requirementsPath = System.IO.Path.Combine(scriptFolderPath, "requirements.txt");

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.Arguments = string.Format(" \"{0}\" \"{1}\" \"{2}\" ", "install", "-r", requirementsPath);
        startInfo.FileName = "pip";
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        // Uncomment for showing errors in terminal
        // startInfo.RedirectStandardError = true;
        // startInfo.UseShellExecute = false;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        // Salience.ShowError(process.StandardError.ReadToEnd());
        process.WaitForExit();

        return process.ExitCode;
    }

    public int startProcess(string arguments)
    {

        if (pythonPath == "")
            getPythonPath();

        if (!installedReq)
            if (installRequir() == 1)
                return 1;

        installedReq = true;
        
        ProcessStartInfo startInfo = new ProcessStartInfo(pythonPath);
        startInfo.Arguments = arguments;

        // startInfo.RedirectStandardError = true;
        // startInfo.UseShellExecute = false;

        // Avoid openning a window
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        // Salience.ShowError(process.StandardError.ReadToEnd());
        process.WaitForExit();

        return 0;
    }

    // ----------------------------- IMAGE PREDICTION --------------------------------------------------

    public void Start()
    {
        string scriptPath = Salience.assetsPath + "/SalienceTool/RedImage/connection.py";

        startProcess(scriptPath);
    }

    public int ExecuteNoSocketsImage(int op, string path, int param, string fileName, int useGpu)
    {
        string scriptPath = Salience.assetsPath + "/SalienceTool/RedImage/executeImage.py";
        string arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"", scriptPath, op, path, param, fileName, useGpu);

        return startProcess(arguments);
    }

    // ----------------------------- VIDEO PREDICTION --------------------------------------------------

    public int ExecuteNoSocketsVideo(string path, int op, int useGpu)
    {
        string scriptPath = Salience.assetsPath + "/SalienceTool/RedVideo/executeVideo.py";
        string arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", scriptPath, path, op, useGpu);

        return startProcess(arguments);
    }

}
