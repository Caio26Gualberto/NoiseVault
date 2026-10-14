using NoiseVault.Application.Mentions.Dtos;

namespace NoiseVault.Application.Mentions.Interfaces
{
    public interface IMentionAppService
    {
        Task<Dictionary<string, MentionDto>> Resolve(IEnumerable<string?> texts);
        Task Notify(string text, string? previousText, MentionTarget target, int actorUserId, IReadOnlyCollection<int> alreadyNotifiedUserIds);
        List<MentionDto> Pick(string? text, IReadOnlyDictionary<string, MentionDto> resolved);
    }
}
