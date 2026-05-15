using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Brain.Desktop.Models;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class DataViewerViewModel : ObservableObject
{
    private readonly MemoryService _memory;
    private List<MemoryRecord> _allRecords = new();

    [ObservableProperty] private string _filterType = "Все";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _tagFilter = "";
    [ObservableProperty] private string _recordCount = "Записей: 0";
    [ObservableProperty] private string _detailText = "Выберите запись";

    public ObservableCollection<DataRow> Rows { get; } = new();
    public List<string> TypeFilters { get; } = ["Все", "invoice", "contract", "claim", "note", "other"];

    public DataViewerViewModel(MemoryService memory)
    {
        _memory = memory;
        LoadData();
    }

    public void LoadData()
    {
        _allRecords = _memory.LoadAll();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        Rows.Clear();
        var filtered = _allRecords.AsEnumerable();

        if (FilterType != "Все")
            filtered = filtered.Where(r => r.DocType == FilterType);

        if (!string.IsNullOrEmpty(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(r =>
                JsonSerializer.Serialize(r).ToLower().Contains(search));
        }

        if (!string.IsNullOrEmpty(TagFilter))
        {
            var tag = TagFilter.ToLower();
            filtered = filtered.Where(r =>
                r.Tags.Any(t => t.ToLower().Contains(tag)));
        }

        foreach (var r in filtered)
        {
            Rows.Add(new DataRow
            {
                DocType = r.DocType,
                SourceFile = r.SourceFile,
                Entities = string.Join(", ", r.Entities.Select(e => e.Name)),
                Tags = string.Join(", ", r.Tags),
                Summary = (r.Summary?.Length > 150 ? r.Summary[..150] : r.Summary) ?? ""
            });
        }

        RecordCount = $"Записей: {Rows.Count}";
    }
}

public class DataRow
{
    public string DocType { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string Entities { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Summary { get; set; } = "";
}
