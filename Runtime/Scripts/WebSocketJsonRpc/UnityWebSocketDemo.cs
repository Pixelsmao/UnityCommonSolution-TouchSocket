using Assets.UnityTouchSocket.RpcClientServer;
using System;
using TouchSocket.Core;
using TouchSocket.Rpc;
using TouchSocket.Sockets;
using UnityEngine;
using UnityRpcProxy;

namespace UnityWebSocket.Demo
{
    public class UnityWebSocketDemo : MonoBehaviour
    {
        public string address = "ws://127.0.0.1:7707";
        public bool logMessage = true;
        public string sendText = "Hello World!";
        private float fps = 0;
        private int frame = 0;
        private Color green = new Color(0.1f, 1, 0.1f);
        private string log = "";
        private UnityWebSocketJsonRpcClient m_jsonRpcClient;
        private int receiveCount;
        private Color red = new Color(1f, 0.1f, 0.1f);
        private Vector2 scrollPos;
        private int sendCount;
        private float time = 0;
        private Color wait = new Color(0.7f, 0.3f, 0.3f);


        private void AddLog(string str)
        {
            if (!this.logMessage)
                return;
            if (str.Length > 100)
                str = str.Substring(0, 100) + "...";
            this.log += str + "\n";
            if (this.log.Length > 22 * 1024)
            {
                this.log = this.log.Substring(this.log.Length - 22 * 1024);
            }
            this.scrollPos.y = int.MaxValue;
        }

        private async void OnApplicationQuit()
        {
            //if (socket != null && socket.ReadyState != WebSocketState.Closed)
            //{
            //    socket.CloseAsync();
            //}

            var socket = this.m_jsonRpcClient;
            if (socket != null)
            {
                await socket.CloseAsync();
            }
        }

        private async void OnGUI()
        {
            var scale = Screen.width / 800f;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(0, 0, 0),
                Quaternion.identity,
                new Vector3(scale, scale, 1)
            );
            var width = GUILayout.Width(Screen.width / scale - 10);

            var state = this.m_jsonRpcClient == null ? false : this.m_jsonRpcClient.Online;

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "SDK Version: " + Settings.VERSION,
                GUILayout.Width(Screen.width / scale - 100)
            );
            GUI.color = this.green;
            GUILayout.Label($"FPS: {this.fps:F2}", GUILayout.Width(80));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("State: ", GUILayout.Width(36));
            GUI.color =
                state ? this.red : this.green;
            GUILayout.Label($"{state}", GUILayout.Width(120));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUI.enabled = !state;
            GUILayout.Label("Address: ", width);
            this.address = GUILayout.TextField(this.address, width);

            GUILayout.BeginHorizontal();
            GUI.enabled = !state;
            if (GUILayout.Button(!state ? "Connecting..." : "Connect"))
            {
                try
                {
                    //把线程区域设为中文
                    //Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("zh-CN");
                    var container = new Container();

                    container.AddRpcServerProvider();

                    var store = new RpcStore(container);
                    store.RegisterServer<ReverseJsonRpcServer>();
                    this.AddLog("RegisterServer");
                }
                catch (Exception ex)
                {

                    this.AddLog("error:" + ex.Message);
                }


                this.m_jsonRpcClient = new UnityWebSocketJsonRpcClient();
                await this.m_jsonRpcClient.SetupAsync(new TouchSocket.Core.TouchSocketConfig()
                    .ConfigureContainer(a =>
                    {
                        a.AddEasyLogger(logger =>
                        {
                            Debug.Log(logger);
                        });

                        a.AddRpcStore(store =>
                        {
                            Debug.Log("AddRpcStore");
                            store.RegisterServer<ReverseJsonRpcServer>();
                        });
                    })
                      .SetRemoteIPHost(this.address));

                this.AddLog(string.Format("Connecting..."));
                await this.m_jsonRpcClient.ConnectAsync();
                this.AddLog(string.Format("Connected"));

                //try
                //{
                //    WaitDataAsync.Reset();
                //    var status = await this.WaitDataAsync.WaitAsync(5000);
                //    //await this.m_asyncAutoResetEvent.WaitOneAsync();
                //    AddLog("run:" + status);
                //}
                //catch (Exception ex)
                //{
                //    AddLog(ex.Message);
                //}

                //AddLog("run:");
            }

            GUI.enabled = state;
            if (GUILayout.Button(!state ? "Closing..." : "Close"))
            {
                this.AddLog(string.Format("Closing..."));
                await this.m_jsonRpcClient.CloseAsync();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Text: ");
            this.sendText = GUILayout.TextArea(this.sendText, GUILayout.MinHeight(50), width);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send") && !string.IsNullOrEmpty(this.sendText))
            {
                try
                {
                    this.AddLog(string.Format("Sending"));
                    var ss = await this.m_jsonRpcClient.JsonRpc_LoginAsync(new MyLoginModel()
                    {
                        Account = "123",
                        Password = "abc"
                    });
                    //await socket.SendAsync(sendText);
                    this.AddLog(string.Format("Rev: {0}", ss));
                    this.sendCount += 1;
                }
                catch (System.Exception ex)
                {
                    this.AddLog(ex.Message);
                }

            }
            if (GUILayout.Button("Send Bytes") && !string.IsNullOrEmpty(this.sendText))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(this.sendText);
                //socket.SendAsync(bytes);
                this.AddLog(string.Format("Send Bytes ({1}): {0}", this.sendText, bytes.Length));
                this.sendCount += 1;
            }
            if (GUILayout.Button("Send x100") && !string.IsNullOrEmpty(this.sendText))
            {
                for (var i = 0; i < 100; i++)
                {
                    var text = (i + 1).ToString() + ". " + this.sendText;
                    //socket.SendAsync(text);
                    this.AddLog(string.Format("Send: {0}", text));
                    this.sendCount += 1;
                }
            }
            if (GUILayout.Button("Send Bytes x100") && !string.IsNullOrEmpty(this.sendText))
            {
                for (var i = 0; i < 100; i++)
                {
                    var text = (i + 1).ToString() + ". " + this.sendText;
                    var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                    //socket.SendAsync(bytes);
                    this.AddLog(string.Format("Send Bytes ({1}): {0}", text, bytes.Length));
                    this.sendCount += 1;
                }
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            this.logMessage = GUILayout.Toggle(this.logMessage, "Log Message");
            GUILayout.Label(string.Format("Send Count: {0}", this.sendCount));
            GUILayout.Label(string.Format("Receive Count: {0}", this.receiveCount));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear"))
            {
                this.log = "";
                this.receiveCount = 0;
                this.sendCount = 0;

                //this.m_asyncAutoResetEvent.Set();
                //this.WaitDataAsync.Set();
            }

            this.scrollPos = GUILayout.BeginScrollView(
                this.scrollPos,
                GUILayout.MaxHeight(Screen.height / scale - 270),
                width
            );
            GUILayout.Label(this.log);
            GUILayout.EndScrollView();
        }
        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            this.AddLog(string.Format("Closed: StatusCode: {0}, Reason: {1}", e.StatusCode, e.Reason));
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            this.AddLog(string.Format("Error: {0}", e.Message));
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                this.AddLog(string.Format("Receive Bytes ({1}): {0}", e.Data, e.RawData.Length));
            }
            else if (e.IsText)
            {
                this.AddLog(string.Format("Receive: {0}", e.Data));
            }
            this.receiveCount += 1;
        }

        private void Socket_OnOpen(object sender, OpenEventArgs e)
        {
            this.AddLog(string.Format("Connected: {0}", this.address));
        }
        private void Update()
        {
            this.frame += 1;
            this.time += Time.deltaTime;
            if (this.time >= 0.5f)
            {
                this.fps = this.frame / this.time;
                this.frame = 0;
                this.time = 0;
            }
        }
    }
}