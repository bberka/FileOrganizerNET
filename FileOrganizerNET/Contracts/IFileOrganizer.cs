using FileOrganizerNET.Models.Config;
using FileOrganizerNET.Models.Result;

namespace FileOrganizerNET.Contracts;

public interface IFileOrganizer
{
    OrganizationResult Organize(string targetPath, OrganizerConfig config, bool isRecursive = false, bool isDryRun = false,
        bool enableDuplicateCheck = false);
}