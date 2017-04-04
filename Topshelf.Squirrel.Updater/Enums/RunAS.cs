namespace Topshelf.Squirrel.Updater
{
    public enum RunAS
    {
        // Run Service as LocalSystem Account
        LocalSystem,

        // Run Service as LocalService Account
        LocalService,

        // Run Service as NetworkService Account
        NetworkService,

        // Prompt for Credentials during install
        PromptForCredentials,

        // Run Service as Specific user
        SpecificUser
    }
}
