using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    sealed public partial class SymbioteInterface : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Symbiote Interface Plugin";
        public const string Guid = "org.lordashes.plugins.symbioteinterface";
        public const string Version = "1.0.0.0";

        // Configuration
        public string RequestResult { get; set; }
        private ConfigEntry<KeyboardShortcut> trigger { get; set; }

        private static SymbioteInterface _self = null;


        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            _self = this;

            trigger = Config.Bind("Setting", "Test Trigger", new KeyboardShortcut(KeyCode.X, new KeyCode[] { KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.LeftAlt} ));

            UnityEngine.Debug.Log(Name+": Active.");
        }

        void Update()
        {
            if(trigger.Value.IsUp())
            {
                TestMode();
            }
        }

        public void SendMessageToSymbiote(string message, bool bruetForce = false)
        {
            _self.StartCoroutine(SendMessageToSymbioteAsync(message, "onPluginRequest", null, bruetForce));
        }

        public void SendMessageToSymbiote(string message, string subscription, bool bruetForce = false)
        {
            _self.StartCoroutine(SendMessageToSymbioteAsync(message, subscription, null, bruetForce));
        }

        public void SendMessageToSymbiote(string message, Action<string, string, string> callback, bool bruetForce = false)
        {
            _self.StartCoroutine(SendMessageToSymbioteAsync(message, "onPluginRequest", callback, bruetForce));
        }

        public void SendMessageToSymbiote(string message, string subscription, Action<string, string, string> callback, bool bruetForce = false)
        {
            _self.StartCoroutine(SendMessageToSymbioteAsync(message, subscription, callback, bruetForce));
        }

        private IEnumerator SendMessageToSymbioteAsync(string message, string subscription, Action<string, string, string> callback, bool bruteForce = false)
        {
            bool subscribed = false;

            if (!subscribed)
            {
                try
                {
                    // Find Active Symbiote
                    Symbiotes.Symbiote symbiote = Symbiotes.SymbiotesManager.Symbiotes.Where(s => Symbiotes.SymbiotesManager.IsSymbioteRunning(s)).ElementAt(0);

                    Symbiotes.Subscription[] subscriptions = symbiote.Manifest.GetSubscriptions();

                    Dictionary<string, object> level1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(System.IO.File.ReadAllText(symbiote.RootPath + "\\manifest.json"));
                    if (level1.ContainsKey("api"))
                    {
                        Dictionary<string, object> level2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(level1["api"]));
                        if (level2.ContainsKey("subscriptions"))
                        {
                            Dictionary<string, object> level3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(level2["subscriptions"]));
                            foreach (KeyValuePair<string, object> key in level3)
                            {
                                Dictionary<string, string> level4 = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(key.Value));
                                foreach (KeyValuePair<string, string> item in level4)
                                {
                                    Debug.Log(Name + ": Symbiote " + symbiote.Manifest.Name + ": " + key.Key + " => " + item.Key + " => " + item.Value);
                                    if (item.Key == subscription && item.Value == subscription) { subscribed = true; }
                                }
                            }
                        }
                    }
                }
                catch(Exception x)
                {
                    // No Symbiotes Active
                }
            }

            if (subscribed || bruteForce)
            {
                Debug.Log(Name + ": Sending Plugin Messages '" + message + "' To '" + subscription + "' (Subscribed: "+subscribed+", Brute Force: " + bruteForce + ")");

                foreach (Vuplex.WebView.Internal.BaseWebView view in GameObject.FindObjectsByType(typeof(Vuplex.WebView.Internal.BaseWebView), FindObjectsSortMode.None))
                {
                    RequestResult = "In Progress";
                    string javascript = subscription + "(\"" + message + "\");";
                    Debug.Log(Name + ": WebView " + view.name + " " + view.Title + " : Javascript: " + javascript);
                    try
                    {
                        view.ExecuteJavaScript(javascript, (result) =>
                        {
                            RequestResult = (Convert.ToString(result).Trim() == "" || Convert.ToString(result).Trim() == "undefined") ? "Done" : result;
                        });
                    }
                    catch (Exception x)
                    {
                        RequestResult = x.Message;
                    }
                    while (RequestResult == "In Progress")
                    {
                        yield return new WaitForSeconds(0.25f);
                    }
                    if (callback != null) { callback(view.name, view.Title, RequestResult); }
                }
            }
            else
            {
                Debug.Log(Name + ": Ignoring Plugin Messages '" + message + "' To '" + subscription + "' (Subscribed: " + subscribed + ", Brute Force: " + bruteForce + ")");
            }
        }

        private void TestMode()
        {
            Debug.Log(Name + ": Test Mode");
            SystemMessage.AskForTextInput("Test Mode", "Symbiote Method Colon Message", "OK", (raw) => 
            {
                string method = raw.Substring(0, raw.IndexOf(":"));
                string message = raw.Substring(raw.IndexOf(":")+1);
                SendMessageToSymbiote(message, method, (n, t, m) =>
                {
                    ChatManager.SendChatMessageToBoard("[" + t + "]\r\nName: " + n + "\r\nContent: " + m, LocalPlayer.Id.Value);
                });
            }, null, "Cancel", null, "onPluginRequest:Hello");
        }
    }
}
