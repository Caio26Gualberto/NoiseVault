using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.PhotoTags.Interfaces;
using NoiseVault.Application.PhotoTags.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Archive;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.PhotoTags
{
    public class PhotoTagRepository : Repository<PhotoTag>, IPhotoTagRepository
    {
        public PhotoTagRepository(AppDbContext context) : base(context)
        {
        }

        public Task<PhotoTarget?> FindTargetAsync(int mediaId) =>
            _context.Set<MediaUpload>()
                .AsNoTracking()
                .Where(m => m.Id == mediaId)
                .Select(m => new PhotoTarget
                {
                    MediaId = m.Id,
                    Kind = m.Kind,
                    Status = m.Status,
                    PostId = m.Attachment != null && !m.Attachment.Post!.IsDeleted && m.Attachment.Post.PublishedAt != null
                        ? m.Attachment.PostId
                        : null,
                    ArchiveEntryId = m.ArchiveAttachment != null && !m.ArchiveAttachment.ArchiveEntry!.IsDeleted && m.ArchiveAttachment.ArchiveEntry.Status == EArchiveEntryStatus.Published
                        ? m.ArchiveAttachment.ArchiveEntryId
                        : null,
                    ContentAuthorUserId = m.Attachment != null
                        ? m.Attachment.Post!.AuthorUserId
                        : m.ArchiveAttachment != null ? m.ArchiveAttachment.ArchiveEntry!.AuthorUserId : null,
                })
                .FirstOrDefaultAsync();

        public Task<List<PhotoTagRow>> ListByMediaAsync(IReadOnlyCollection<int> mediaIds) =>
            Project(Visible().Where(t => mediaIds.Contains(t.MediaUploadId)))
                .OrderBy(t => t.MediaUploadId)
                .ThenBy(t => t.X)
                .ThenBy(t => t.Id)
                .ToListAsync();

        public Task<PhotoTagRow?> FindRowAsync(int tagId) =>
            Project(Visible().Where(t => t.Id == tagId)).FirstOrDefaultAsync();

        private IQueryable<PhotoTag> Visible() =>
            _dbSet.AsNoTracking().Where(t => !t.IsDeleted && (t.TaggedUserId == null || t.TaggedUser!.IsActive));

        private static IQueryable<PhotoTagRow> Project(IQueryable<PhotoTag> query) =>
            query.Select(t => new PhotoTagRow
            {
                Id = t.Id,
                MediaUploadId = t.MediaUploadId,
                X = t.X,
                Y = t.Y,
                TaggedUserId = t.TaggedUserId,
                TaggedUserName = t.TaggedUser != null ? t.TaggedUser.Name : null,
                TaggedUserNickname = t.TaggedUser != null ? t.TaggedUser.Nickname : null,
                TaggedUserAvatarKey = t.TaggedUser != null ? t.TaggedUser.AvatarKey : null,
                Name = t.Name,
                TaggedByUserId = t.TaggedByUserId,
                ContentAuthorUserId = t.MediaUpload!.Attachment != null
                    ? t.MediaUpload.Attachment.Post!.AuthorUserId
                    : t.MediaUpload.ArchiveAttachment != null ? t.MediaUpload.ArchiveAttachment.ArchiveEntry!.AuthorUserId : null,
                InArchive = t.MediaUpload.ArchiveAttachment != null,
                PostId = t.MediaUpload.Attachment != null ? t.MediaUpload.Attachment.PostId : null,
                ArchiveEntryId = t.MediaUpload.ArchiveAttachment != null ? t.MediaUpload.ArchiveAttachment.ArchiveEntryId : null,
            });
    }
}
