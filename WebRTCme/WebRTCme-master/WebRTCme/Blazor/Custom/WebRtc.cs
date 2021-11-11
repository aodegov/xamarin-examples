﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.JSInterop;


namespace WebRTCme
{
    internal class WebRtc : IWebRtc
    {
        public static IWebRtc Create() => new WebRtc();

        public void Dispose()
        {
        }

#if NETSTANDARD
        public IWindow Window(IJSRuntime jsRuntime) => throw new NotImplementedException();
#else
        public IWindow Window(IJSRuntime jsRuntime) => WebRTCme.Bindings.Blazor.Api.Window.Create(jsRuntime);
#endif
    }
}
