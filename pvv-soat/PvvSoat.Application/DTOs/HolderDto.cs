using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.DTOs;

public record HolderDto(
    Guid HolderId,
    string DNI,
    string FirstName,
    string LastName,
    string Email,
    string Phone)
{
    public static HolderDto FromEntity(Holder holder) => new(
        holder.HolderId,
        holder.DNI,
        holder.FirstName,
        holder.LastName,
        holder.Email,
        holder.Phone);
}
