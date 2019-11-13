using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MIDIAudioFramework
{
    public enum MIDICommonDevices
    {
        KDMAPI = -2,
        BASSDirect = -1,
        MSGS = 0
    }

    static class MIDIAudio
    {
        static BlockingCollection<uint> eventQueue = new BlockingCollection<uint>();

        public static int? CurrentOpenDevice = null;

        static Task senderThread = null;
        static CancellationTokenSource senderCancel = null;

        static void CloseCurrentSender()
        {
            if (senderCancel != null) senderCancel.Cancel();
            if (senderThread != null) senderThread.GetAwaiter().GetResult();
        }

        public static void OpenDevice(int device)
        {
            CloseCurrentSender();
            if(device == (int)MIDICommonDevices.BASSDirect)
            {
                senderCancel = new CancellationTokenSource();
                senderThread = Task.Run(BASSDirectSenderLoop);
            }
        }

        static void BASSDirectSenderLoop()
        {
            try
            {
                var currentDevice = CurrentOpenDevice;
                BASSMIDI.InitBASS();
                foreach (var e in eventQueue.GetConsumingEnumerable(senderCancel.Token))
                {
                    BASSMIDI.SendEvent(e);
                    if (CurrentOpenDevice != currentDevice) break;
                }
            }
            catch { }
            BASSMIDI.DisposeBASS();
        }
    }
}
