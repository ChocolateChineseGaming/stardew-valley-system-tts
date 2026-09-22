using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MandarinVoice;

internal sealed class MacAudioPlayer : IDisposable
{
    private Process? process;
    private string? generatedFile;
    private string? inputFile;
    private string? cacheFile;
    private float pendingVolume;
    private float pendingPlaybackRate = 1f;
    private bool playbackStarted;

    public void Play(string path, float volume, float playbackRate)
    {
        Stop();
        StartPlayback(path, volume, playbackRate);
    }

    private void StartPlayback(string path, float volume, float playbackRate)
    {
        var start = new ProcessStartInfo("/usr/bin/afplay") { UseShellExecute = false };
        start.ArgumentList.Add("-v");
        start.ArgumentList.Add(volume.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add("-r");
        start.ArgumentList.Add(playbackRate.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add("-q");
        start.ArgumentList.Add("1");
        start.ArgumentList.Add(path);
        process = Process.Start(start) ?? throw new IOException("无法启动 afplay。");
        playbackStarted = true;
    }

    public bool ConsumePlaybackStarted()
    {
        bool result = playbackStarted;
        playbackStarted = false;
        return result;
    }

    public void GenerateAndPlay(string text, string voice, int rate, string destination,
        float volume, float playbackRate)
    {
        Stop();
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = Path.Combine(Path.GetDirectoryName(destination)!,
            "." + Guid.NewGuid().ToString("N"));
        inputFile = temporary + ".txt";
        generatedFile = temporary + ".wav";
        cacheFile = destination;
        pendingVolume = volume;
        pendingPlaybackRate = playbackRate;
        try
        {
            File.WriteAllText(inputFile, text, new UTF8Encoding(false));
            var start = new ProcessStartInfo("/usr/bin/say") { UseShellExecute = false };
            foreach (string argument in new[]
            {
                "-v", voice, "-r", rate.ToString(CultureInfo.InvariantCulture),
                "-f", inputFile, "-o", generatedFile,
                "--file-format=WAVE", "--data-format=LEI16@22050"
            })
                start.ArgumentList.Add(argument);
            process = Process.Start(start) ?? throw new IOException("无法启动系统 TTS。");
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public int? Reap()
    {
        if (process is null || !process.HasExited) return null;
        int code = process.ExitCode;
        process.Dispose();
        process = null;
        if (generatedFile is null) return code;
        if (code != 0 || !SpeechCache.HasAudio(generatedFile))
        {
            int result = code != 0 ? code : -1;
            CleanTemporaryFiles();
            return result;
        }

        string playbackFile = cacheFile!;
        File.Move(generatedFile, playbackFile, overwrite: true);
        generatedFile = null;
        cacheFile = null;
        try { File.Delete(inputFile!); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        inputFile = null;
        StartPlayback(playbackFile, pendingVolume, pendingPlaybackRate);
        return null;
    }

    public void Stop()
    {
        Process? old = process;
        process = null;
        if (old is not null)
        {
            try { if (!old.HasExited) old.Kill(); }
            catch (InvalidOperationException) { }
            finally { old.Dispose(); }
        }
        CleanTemporaryFiles();
    }

    private void CleanTemporaryFiles()
    {
        foreach (string? path in new[] { generatedFile, inputFile })
        {
            if (path is null) continue;
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        generatedFile = inputFile = cacheFile = null;
        pendingPlaybackRate = 1f;
        playbackStarted = false;
    }

    public void Dispose() => Stop();
}
