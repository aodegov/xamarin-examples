﻿using System.Dynamic;
using System.Threading.Tasks;

namespace WebRTCme
{
    public interface IRTCTrackEvent : INativeObject
    {
        IRTCRtpReceiver Receiver { get; }

        IMediaStream[] Streams { get; }

        IMediaStreamTrack Track { get; }

        IRTCRtpTransceiver Transceiver { get; }
    }
}