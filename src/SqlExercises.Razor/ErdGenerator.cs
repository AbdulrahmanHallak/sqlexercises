using System.Diagnostics;

namespace SqlExercises.Razor;

public interface IErdGenerator
{
    void GenerateErd(string schemaName);
}

public class ErdGenerator(
    IWebHostEnvironment env,
    PythonConnectionString connString,
    ILogger<ErdGenerator> logger
) : IErdGenerator
{
    public void GenerateErd(string schemaName)
    {
        RunPythonScript(schemaName);
    }

    private void RunPythonScript(string schemaName)
    {
        var workingDir = Path.Join(GetSrcPath(Environment.CurrentDirectory), "ErdGenerator");
        var psi = new ProcessStartInfo()
        {
            FileName = $"{workingDir}/venv/bin/python",
            Arguments =
                $"erdgen.py {connString} {schemaName} {env.WebRootPath}/images/{schemaName}.webp",

            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = new Process();
        process.StartInfo = psi;

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                logger.LogError("Error occurred trying to generate erd: {ErdError}", e.Data);
                throw new Exception(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        logger.LogInformation($"Python script exited with code {process.ExitCode}");
    }

    string GetSrcPath(string path)
    {
        var dir = new DirectoryInfo(path);

        while (
            dir != null
            && !string.Equals(dir.Name, "sqlexercises", StringComparison.OrdinalIgnoreCase)
        )
        {
            dir = dir.Parent;
        }
        if (dir is null)
        {
            logger.LogCritical($"src dir not found in {path}");
            throw new Exception($"src dir not found in {path}");
        }

        return Path.Join(dir.FullName, "src");
    }
}
