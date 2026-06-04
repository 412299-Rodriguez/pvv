namespace PvvConfig.Application.DTOs;

public record LoginResponseDto(string Token, DateTime ExpiresAt, Guid CompanyId);
