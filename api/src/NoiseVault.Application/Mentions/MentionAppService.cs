using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Mentions.Dtos;
using NoiseVault.Application.Mentions.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Mentions;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Mentions
{
    public class MentionAppService : IMentionAppService
    {
        private readonly IPublicProfileAppService _publicProfileAppService;
        private readonly IBandAppService _bandAppService;
        private readonly INotificationAppService _notificationAppService;

        public MentionAppService(IPublicProfileAppService publicProfileAppService, IBandAppService bandAppService, INotificationAppService notificationAppService)
        {
            _publicProfileAppService = publicProfileAppService;
            _bandAppService = bandAppService;
            _notificationAppService = notificationAppService;
        }

        public async Task<Dictionary<string, MentionDto>> Resolve(IEnumerable<string?> texts)
        {
            var handles = texts.SelectMany(MentionRules.Handles).ToHashSet(StringComparer.Ordinal);

            if (handles.Count == 0)
                return [];

            var people = await _publicProfileAppService.SummariesByNicknames(handles.ToList());
            var bands = await _bandAppService.SummariesBySlugs(handles.Where(h => !people.ContainsKey(h)).ToList());

            var resolved = new Dictionary<string, MentionDto>(StringComparer.Ordinal);

            foreach (var (handle, person) in people)
                resolved[handle] = new MentionDto { Handle = handle, Kind = EMentionKind.Person, Id = person.Id, Name = person.Name, AvatarUrl = person.AvatarUrl };

            foreach (var (handle, band) in bands)
                resolved[handle] = new MentionDto { Handle = handle, Kind = EMentionKind.Band, Id = band.Id, Name = band.Name, AvatarUrl = band.AvatarUrl };

            return resolved;
        }

        public List<MentionDto> Pick(string? text, IReadOnlyDictionary<string, MentionDto> resolved) =>
            MentionRules.Handles(text)
                .Where(resolved.ContainsKey)
                .Select(h => resolved[h])
                .ToList();

        public async Task Notify(string text, string? previousText, MentionTarget target, int actorUserId, IReadOnlyCollection<int> alreadyNotifiedUserIds)
        {
            var previous = MentionRules.Handles(previousText);
            var added = MentionRules.Handles(text).Where(h => !previous.Contains(h)).ToList();

            if (added.Count == 0)
                return;

            var resolved = await Resolve([string.Join(' ', added.Select(h => "@" + h))]);
            var skip = new HashSet<int>(alreadyNotifiedUserIds) { actorUserId };

            var people = resolved.Values.Where(m => m.Kind == EMentionKind.Person && skip.Add(m.Id)).Select(m => m.Id).ToList();

            if (people.Count > 0)
                await _notificationAppService.NotifyMany(people, Input(target.InComment ? ENotificationKind.MentionedInComment : ENotificationKind.MentionedInPost, actorUserId, target, null));

            foreach (var band in resolved.Values.Where(m => m.Kind == EMentionKind.Band))
            {
                var owners = (await _bandAppService.MemberUserIds(band.Id, EBandMemberRole.Owner)).Where(skip.Add).ToList();

                if (owners.Count > 0)
                    await _notificationAppService.NotifyMany(owners, Input(target.InComment ? ENotificationKind.BandMentionedInComment : ENotificationKind.BandMentionedInPost, actorUserId, target, band.Id));
            }
        }

        private static NotificationInput Input(ENotificationKind kind, int actorUserId, MentionTarget target, int? bandId) =>
            new(kind, actorUserId, PostId: target.PostId, EventId: target.EventId, ArchiveEntryId: target.ArchiveEntryId, BandId: bandId);
    }
}
