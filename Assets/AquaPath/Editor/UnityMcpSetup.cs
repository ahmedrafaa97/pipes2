using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

namespace AquaPath.Editor
{
    [InitializeOnLoad]
    public static class UnityMcpSetup
    {
        static UnityMcpSetup() { EditorApplication.delayCall += Connect; }

        [MenuItem("Aqua Path/Connect Unity MCP")]
        public static async void Connect()
        {
            if (Application.isBatchMode) return;
            try
            {
                var config = EditorConfigurationCache.Instance;
                config.SetUseHttpTransport(true);
                config.SetHttpTransportScope("local");
                config.SetHttpBaseUrl("http://127.0.0.1:8080");
                config.SetUvxPathOverride(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.local/bin/uvx.exe");
                EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
                var server = MCPServiceLocator.Server;
                if (!server.IsLocalHttpServerReachable())
                {
                    Debug.Log("[Aqua MCP] Waiting for the local Unity MCP server on port 8080.");
                    return;
                }
                if (!MCPServiceLocator.Bridge.IsRunning)
                    await MCPServiceLocator.Bridge.StartAsync();
                var check = await MCPServiceLocator.Bridge.VerifyAsync();
                Debug.Log("[Aqua MCP] " + (check.Success ? "Connected and verified for pips2." : check.Message));
            }
            catch (Exception e) { Debug.LogWarning("[Aqua MCP] " + e.Message); }
        }
    }
}
