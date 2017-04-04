using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
