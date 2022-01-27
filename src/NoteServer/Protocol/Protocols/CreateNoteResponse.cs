using ProtoBuf;

namespace NoteServer.Protocols;

[ProtoContract(ImplicitFields =ImplicitFields.AllPublic)]
public class CreateNoteResponse
{
    public Guid NoteId{ get;set; }

    public bool Status{ get;set; }
}
