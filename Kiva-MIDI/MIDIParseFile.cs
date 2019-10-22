using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    public class NoteLink
    {
        public Note note;
        public NoteLink next;
    }

    class MIDIParseFile : MIDIFile
    {
        public MIDIParseFile(string path, MIDILoaderSettings settings, CancellationToken cancel)
            : base(path, settings, cancel)
        {
        }

        public override void Parse()
        {
            Open();
            FirstPassParse();

        }
    }
}
