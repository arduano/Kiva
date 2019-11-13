using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDIAudioFramework
{
    static class MIDIAudio
    {
        static BlockingCollection<uint> eventQueue = new BlockingCollection<uint>();


    }
}
