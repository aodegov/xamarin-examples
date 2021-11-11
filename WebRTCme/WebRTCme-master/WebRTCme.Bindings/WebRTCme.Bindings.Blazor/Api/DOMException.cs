﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using WebRTCme.Bindings.Blazor.Interops;

namespace WebRTCme.Bindings.Blazor.Api
{
    internal class DOMException : ApiBase, IDOMException
    {
        public static IDOMException Create(IJSRuntime jsRuntime, JsObjectRef jsObjectRefNativeDomException) =>
            new DOMException(jsRuntime, jsObjectRefNativeDomException);

        private DOMException(IJSRuntime jsRuntime, JsObjectRef jsObjectRef) : base(jsRuntime, jsObjectRef) { }


        public string Message => GetNativeProperty<string>("message");

        public string Name => GetNativeProperty<string>("name");
    }
}
