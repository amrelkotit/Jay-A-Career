using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

/// <summary>Default editor-assembly entry point for resolving the Cinemachine dependency.</summary>
public static class PackageInstallerEntry
{
    static AddRequest request;
    static double deadline;

    public static void InstallCinemachine()
    {
        Debug.Log("[JayCareerBootstrap] Starting Cinemachine 3.1.5 Package Manager install.");
        request = Client.Add("com.unity.cinemachine@3.1.5");
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (request == null || !request.IsCompleted)
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                EditorApplication.update -= Poll;
                Debug.LogError("[JayCareerBootstrap] Package Manager timed out.");
                EditorApplication.Exit(2);
            }
            return;
        }

        EditorApplication.update -= Poll;
        if (request.Status == StatusCode.Success)
        {
            Debug.Log("[JayCareerBootstrap] Resolved " + request.Result.name + "@" + request.Result.version);
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[JayCareerBootstrap] " + request.Error.message);
            EditorApplication.Exit(1);
        }
    }
}
