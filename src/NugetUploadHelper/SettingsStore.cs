using System.Text;
using System.Text.Json;

namespace NugetUploadHelper;

/// <summary>
/// 负责本地配置文件（<c>settings.json</c>）的明文读取与原子写入。
/// </summary>
internal sealed class SettingsStore(string filePath)
{
    /// <summary>
    /// JSON 序列化选项：启用缩进以便人工查看与编辑。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 配置文件的完整本地路径。
    /// </summary>
    public string FilePath { get; } = Path.GetFullPath(filePath);

    /// <summary>
    /// 读取并验证本地明文配置；仅当配置文件不存在时返回首次启动的默认配置。
    /// </summary>
    /// <returns>反序列化并通过基础校验的 <see cref="UploadSettings"/> 实例。</returns>
    /// <exception cref="InvalidDataException">当 JSON 格式损坏或配置字段无效时抛出。</exception>
    public UploadSettings Load()
    {
        string json;
        try
        {
            json = File.ReadAllText(FilePath, Encoding.UTF8);
        }
        catch (FileNotFoundException)
        {
            return new UploadSettings();
        }

        try
        {
            var settings = JsonSerializer.Deserialize<UploadSettings>(json, JsonOptions)
                ?? throw new JsonException("配置文件内容为空。");
            settings.Validate(requireCredentials: false);
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException("settings.json 格式或字段无效，请修正文件后重试，或在界面重新填写并保存。", ex);
        }
    }

    /// <summary>
    /// 先写入同目录临时文件再原子替换目标配置文件，避免写入中断或异常损坏原有配置。
    /// </summary>
    /// <param name="settings">待保存的配置实例。</param>
    public void Save(UploadSettings settings)
    {
        settings.Validate(requireCredentials: false);

        if (Path.GetDirectoryName(FilePath) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            string content = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            // 若移动失败，确保清理残留的临时文件
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
