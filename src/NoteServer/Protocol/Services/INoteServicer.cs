using Kadder.Utilies;
using NoteServer.Protocols;

namespace NoteServer.Services;

public interface INoteServicer:IMessagingServicer
{
    Task<CreateNoteResponse> CreateAsync(CreateNoteRequest request);
}
