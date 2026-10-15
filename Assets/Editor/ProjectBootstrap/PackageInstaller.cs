using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace JayCareerBootstrap
{
    /// <summary>Installs the single assignment dependency through Unity Package Manager.</summary>
    public static class PackageInstaller
    {
        static readonly string[] PackagesToAdd = { "com.unity.cinemachine@3.1.5" };
        const double TimeoutSeconds = 600;
        static AddRequest request;
        static double deadline;

        // Invoke with -executeMethod JayCareerBootstrap.PackageInstaller.Install (without -quit).
        public static void Install()
        {
            Debug.Log("[JayCareerBootstrap] Installing: " + string.Join(", ", PackagesToAdd));
            request = Client.Add(PackagesToAdd[0]);
            deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (request == null) return;
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup <= deadline) return;
                EditorApplication.update -= Poll;
                Debug.LogError("[JayCareerBootstrap] Timed out waiting for Unity Package Manager.");
                EditorApplication.Exit(2);
                return;
            }

            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
            {
                Debug.Log("[JayCareerBootstrap] Resolved: " + request.Result.name + "@" + request.Result.version);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[JayCareerBootstrap] Install failed: " + request.Error.message);
                EditorApplication.Exit(1);
            }
        }
    }
}
