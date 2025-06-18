using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.JsonRpc;
using TouchSocket.Rpc;
using TouchSocket.Sockets;
using UnityEngine;
using UnityWebSocket;


public interface IUnityWebSocketJsonRpcClient : IWebSocketJsonRpcClient
{
    WebSocket WebSocket { get; }
}

public class UnityWebSocketJsonRpcClient : SetupConfigObject, IUnityWebSocketJsonRpcClient
{
    private readonly JsonRpcActor m_jsonRpcActor;
    private WebSocket m_webSocket;

    public UnityWebSocketJsonRpcClient()
    {
        // 在WebGL中，如果不设置为true，会导致await后的代码不在主线程执行。
        EasyTask.ContinueOnCapturedContext = true;

        this.SerializerConverter.Add(new JsonStringToClassSerializerFormatter<JsonRpcActor>());
        this.m_jsonRpcActor = new JsonRpcActor()
        {
            SendAction = this.SendAction,
            SerializerConverter = this.SerializerConverter,
            RpcDispatcher = new TouchSocket.Rpc.ImmediateRpcDispatcher<JsonRpcActor, IJsonRpcCallContext>(),
        };
    }

    /// <summary>
    /// JsonRpc的调用键。
    /// </summary>
    public ActionMap ActionMap => this.m_jsonRpcActor.ActionMap;

    /// <inheritdoc/>
    public bool Online => this.m_webSocket?.ReadyState == WebSocketState.Open;

    public IPHost RemoteIPHost => this.Config.GetValue(TouchSocketConfigExtension.RemoteIPHostProperty);

    /// <inheritdoc/>
    public TouchSocketSerializerConverter<string, JsonRpcActor> SerializerConverter { get; } = new TouchSocketSerializerConverter<string, JsonRpcActor>();

    public WebSocket WebSocket => this.m_webSocket;

    public Task CloseAsync(string msg)
    {
        this.m_webSocket?.CloseAsync();
        return EasyTask.CompletedTask;
    }

    #region JsonRpcActor

    private Task SendAction(ReadOnlyMemory<byte> memory)
    {
        var webSocket = this.m_webSocket;
        if (webSocket is not null)
        {
            var str = memory.Span.ToString(Encoding.UTF8);
            webSocket.SendAsync(str);
        }
        return EasyTask.CompletedTask;
    }

    #endregion JsonRpcActor

    public async Task ConnectAsync(int millisecondsTimeout, CancellationToken token)
    {
        if (this.Online)
        {
            return;
        }

        var webSocket = this.m_webSocket;
        webSocket?.CloseAsync();

        webSocket = new WebSocket(this.RemoteIPHost.ToString());

        webSocket.OnMessage += this.WebSocket_OnMessage;

        var tcs = new TaskCompletionSource<string>();
        webSocket.OnOpen += (sender, e) =>
        {
            tcs.SetResult("success");
            //Debug.Log("WebSocket Open");
        };

        webSocket.OnError += (sender, e) =>
        {
            tcs.SetException(e.Exception ?? new Exception(e.Message));
            Debug.Log(e.Message);
        };

        webSocket.ConnectAsync();

        await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));

        this.m_webSocket = webSocket;
    }

    /// <inheritdoc/>
    public async Task<object> InvokeAsync(string invokeKey, Type returnType, IInvokeOption invokeOption, params object[] parameters)
    {
        return await this.m_jsonRpcActor.InvokeAsync(invokeKey, returnType, invokeOption, parameters);
    }

    /// <inheritdoc/>
    protected override void LoadConfig(TouchSocketConfig config)
    {
        base.LoadConfig(config);
        Debug.Log(this.Logger.ToString());
        this.m_jsonRpcActor.Logger = this.Logger;
        this.m_jsonRpcActor.Resolver = this.Resolver;
        var rpcServerProvider = this.Resolver.Resolve<IRpcServerProvider>();

        //Debug.Log("rpcServerProvider:" + rpcServerProvider?.ToString());
        if (rpcServerProvider is not null)
        {
            this.m_jsonRpcActor.SetRpcServerProvider(rpcServerProvider);
        }
    }

    private async void WebSocket_OnMessage(object sender, MessageEventArgs e)
    {
        try
        {
            //Debug.Log($"{e.IsText},{e.Data}");
            if (e.IsText)
            {
                var jsonMemory = new ReadOnlyMemory<byte>(e.RawData);

                if (jsonMemory.IsEmpty)
                {
                    return;
                }

                var callContext = new UnityWebSocketJsonRpcCallContext(this);
                await this.m_jsonRpcActor.InputReceiveAsync(jsonMemory, callContext);
                //Debug.Log($"InputReceiveAsync leave");
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    #region Class
    private class UnityWebSocketJsonRpcCallContext : JsonRpcCallContextBase
    {
        public UnityWebSocketJsonRpcCallContext(object caller)
        {
            this.Caller = caller;
        }
    }
    #endregion
}