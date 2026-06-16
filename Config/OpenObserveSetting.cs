namespace Tool.Config;

public class OpenObserveSetting
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Org { get; set; } = "default";
    public string Stream { get; set; } = "default";
}
