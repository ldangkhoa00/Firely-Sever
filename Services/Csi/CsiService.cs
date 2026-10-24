using HospitalUsers.Data;
using HospitalUsers.DTOs.Csi;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Services.Csi
{
    // This service performs CSI query validation, room authorization and DB reads.
    // Authentication and the Doctor/Nurse role gate remain on CsiController.
    public sealed class CsiService : ICsiService
    {
        private readonly AppDbContext _context;

        public CsiService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CsiRoomResult> GetRoomAsync(
            CsiRoomQueryDto query,
            string? staffId,
            bool isNurse,
            CancellationToken cancellationToken = default)
        {
            // Preserve the previous order: validate before checking nurse room.
            string room = query.RoomNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(room) ||
                room.Length > CsiRoomQueryDto.MaxRoomLength)
            {
                return CsiRoomResult.InvalidRoom();
            }

            if (query.Limit < 1 || query.Limit > CsiRoomQueryDto.MaxLimit)
            {
                return CsiRoomResult.InvalidLimit();
            }

            // A Nurse may read ONLY their currently assigned room in the DB.
            // Never trust an outdated ManagedRoom claim. If a token has multiple
            // roles, the controller passes isNurse=true and this restriction wins.
            if (isNurse)
            {
                if (string.IsNullOrWhiteSpace(staffId))
                {
                    return CsiRoomResult.AccessDenied();
                }

                string? managedRoom = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.StaffId == staffId && u.Status == "ACTIVE")
                    .Select(u => u.ManagedRoom)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(managedRoom) ||
                    !string.Equals(managedRoom.Trim(), room,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return CsiRoomResult.AccessDenied();
                }
            }

            // Existing development policy: Doctor may query any room.
            // This must be reconsidered before real patient data is deployed.
            var records = await _context.CsiRecords
                .AsNoTracking()
                .Where(r => r.RoomNumber == room)
                .OrderByDescending(r => r.RecordedAt)
                .ThenByDescending(r => r.Id)
                .Take(query.Limit)
                .ToListAsync(cancellationToken);

            return CsiRoomResult.Succeeded(records);
        }
    }
}
