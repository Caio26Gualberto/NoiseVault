using Microsoft.EntityFrameworkCore;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public partial class DevSeed
    {
        private const string SoloMarkerSlug = "okamoto";

        private async Task<bool> EnsureSoloArtistsAsync(Dictionary<string, User> people, CancellationToken cancellationToken)
        {
            if (await _context.Bands.AnyAsync(b => b.Slug == SoloMarkerSlug, cancellationToken))
                return false;

            var now = DateTime.UtcNow;
            var jorge = people["jorge.okamoto"];
            var bia = people["bia.santana"];

            var okamoto = NewBand("Okamoto", SoloMarkerSlug, RibeiraoPires, 2012, false,
                "Guitarra instrumental de Ribeirão Pires. Heavy e hard rock dos anos 80 com pedal de delay demais e banda de apoio que muda a cada show.",
                [EMusicGenre.Heavy, EMusicGenre.HardRock, EMusicGenre.ClassicRock],
                [(jorge, "Guitarra", EBandMemberRole.Owner, now.AddYears(-13))],
                "https://instagram.com/okamoto.guitarra", "https://youtube.com/@okamotoguitarra");
            okamoto.Kind = EBandKind.Solo;

            var biaSolo = NewBand("Bia Santana", "bia-santana", SantoAndre, 2023, false,
                "Projeto solo de darkwave e doom atmosférico. Teclado, voz e muita reverb, gravado em casa em Santo André.",
                [EMusicGenre.Gothic, EMusicGenre.Doom, EMusicGenre.Symphonic],
                [(bia, "Teclado e voz", EBandMemberRole.Owner, now.AddYears(-2))],
                null, "https://youtube.com/@biasantana");
            biaSolo.Kind = EBandKind.Solo;

            _context.Bands.AddRange(okamoto, biaSolo);
            await _context.SaveChangesAsync(cancellationToken);

            okamoto.AvatarKey = DevSeedMedia.ImageKey($"bands/{okamoto.Id}/avatar", "avatar");
            biaSolo.AvatarKey = DevSeedMedia.ImageKey($"bands/{biaSolo.Id}/avatar", "avatar");
            await _context.SaveChangesAsync(cancellationToken);

            var pogo = await _context.Bands.FirstOrDefaultAsync(b => b.Slug == "pogo-mortal", cancellationToken);
            var ferrugem = await _context.Bands.FirstOrDefaultAsync(b => b.Slug == MarkerSlug, cancellationToken);
            var admin = people["admin"];

            var partnerships = new List<BandPartnership>
            {
                NewPartnership(okamoto, biaSolo, jorge, EBandPartnershipStatus.Accepted, bia, now.AddMonths(-3)),
            };

            if (pogo != null)
                partnerships.Add(NewPartnership(pogo, okamoto, people["marina.k"], EBandPartnershipStatus.Accepted, jorge, now.AddMonths(-1)));

            BandPartnership? pending = null;

            if (ferrugem != null)
            {
                pending = NewPartnership(biaSolo, ferrugem, bia, EBandPartnershipStatus.Pending, null, null);
                partnerships.Add(pending);
            }

            _context.BandPartnerships.AddRange(partnerships);
            await _context.SaveChangesAsync(cancellationToken);

            if (pending != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Kind = ENotificationKind.BandPartnershipInvited,
                    ActorUserId = bia.Id,
                    BandId = biaSolo.Id,
                    BandPartnershipId = pending.Id,
                    CreatedAt = now.AddHours(-6),
                    LastActivityAt = now.AddHours(-6),
                });
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        private static BandPartnership NewPartnership(Band requester, Band target, User invitedBy, EBandPartnershipStatus status, User? respondedBy, DateTime? respondedAt) => new()
        {
            RequesterBandId = requester.Id,
            TargetBandId = target.Id,
            LowBandId = Math.Min(requester.Id, target.Id),
            HighBandId = Math.Max(requester.Id, target.Id),
            InvitedByUserId = invitedBy.Id,
            RespondedByUserId = respondedBy?.Id,
            Status = status,
            ExpiresAt = (respondedAt ?? DateTime.UtcNow).AddDays(14),
            RespondedAt = respondedAt,
        };
    }
}
