using ProtoBuf;

namespace NoteServer.Protocols;

[ProtoContract(ImplicitFields =ImplicitFields.AllPublic)]
public class CreateNoteRequest
{
    public Guid MemberId{ get;set; }

    public string Title { get; set; } = null!;

    public string Content{ get;set; } = null!;

    public DateTime CreateTime{ get;set; }

    public decimal Words{ get;set; }
}
