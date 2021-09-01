using System;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AZCopyAzureFunction
{
    public static class AZCopyFunction
    {
        [FunctionName("RunAZCopyBackup")]
        public static void Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            foreach (string containerName in config["ContainersToSync"].Split(',').Select(x => x.Trim()))
            {
                log.LogInformation($"Syncing container {containerName}");
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = Path.Combine(context.FunctionAppDirectory, "azcopy.exe");
                process.StartInfo.Arguments = $"sync \"{config["Source"]}/{containerName}{config["SourceSASToken"]}\" \"{config["Destination"]}/{containerName}{config["DestinationSASToken"]}\" ";
                // Keep this False, is MUST !
                process.StartInfo.UseShellExecute = false;
                // Enabling Reading Application's Outputs
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                log.LogInformation(output);
                process.WaitForExit();

                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                if (process.ExitCode != 0)
                {
                    log.LogWarning($"Invalid exit code: {process.ExitCode}");
                }
            }
        }
    }
}
