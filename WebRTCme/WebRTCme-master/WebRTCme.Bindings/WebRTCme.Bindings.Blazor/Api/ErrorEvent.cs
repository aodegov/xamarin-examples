﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebRTCme.Bindings.Blazor.Interops;
using WebRTCme.Bindings.Blazor.Extensions;
using WebRTCme;

namespace WebRTCme.Bindings.Blazor.Api
{
    internal class ErrorEvent : ApiBase, IErrorEvent
    {
        public static IErrorEvent Create(IJSRuntime jsRuntime, JsObjectRef jsObjectRefRtcStatsReport) => 
            new ErrorEvent(jsRuntime, jsObjectRefRtcStatsReport);

        private ErrorEvent(IJSRuntime jsRuntime, JsObjectRef jsObjectRef) : base(jsRuntime, jsObjectRef) { }

        public string Message => GetNativeProperty<string>("message");

        public string FileName => GetNativeProperty<string>("fileName");

        public int LineNo => GetNativeProperty<int>("lineNo");

        public int ColNo => GetNativeProperty<int>("colNo");

        ////public object Error => throw new NotImplementedException();
    }
}
