namespace Project01_movie_lease_system.Models;

public class MovieImageUploadSetting
{
    public string UploadPath { get; set; } 
    public string[] AllowedExtensions { get; set; }
    public long MaxSizeInBytes { get; set; }
}
