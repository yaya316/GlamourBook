using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlamourBook;

/// <summary>
/// 记录持久化。存成单个 JSON 文件放在插件配置目录，方便备份和手动编辑。
/// </summary>
public sealed class RecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string filePath;
    private readonly List<GlamourRecord> records = new();

    public RecordStore()
    {
        var dir = Service.Interface.GetPluginConfigDirectory();
        Directory.CreateDirectory(dir);
        this.filePath = Path.Combine(dir, "records.json");
        this.Load();
    }

    public IReadOnlyList<GlamourRecord> Records => this.records;

    public string FilePath => this.filePath;

    public void Load()
    {
        this.records.Clear();
        if (!File.Exists(this.filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(this.filePath);
            var loaded = JsonSerializer.Deserialize<List<GlamourRecord>>(json, JsonOptions);
            if (loaded != null)
            {
                this.records.AddRange(loaded);
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "读取幻化记录失败: {Path}", this.filePath);
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this.records, JsonOptions);
            File.WriteAllText(this.filePath, json);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "写入幻化记录失败: {Path}", this.filePath);
        }
    }

    public void Add(GlamourRecord record)
    {
        this.records.Add(record);
        this.Save();
    }

    public void Remove(Guid id)
    {
        var removed = this.records.RemoveAll(r => r.Id == id);
        if (removed > 0)
        {
            this.Save();
        }
    }

    public IEnumerable<GlamourRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return this.records;
        }

        return this.records.Where(r =>
            r.PlayerName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || r.WorldName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || r.Note.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
