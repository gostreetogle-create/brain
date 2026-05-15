using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Brain.Desktop.Models;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class DataViewerViewModel : ObservableObject
{
    private readonly MemoryService _memory;

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
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        Rows.Clear();
        var filtered = _memory.Search(
            docType: FilterType,
            textFilter: SearchText,
            tagFilter: TagFilter
        );

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
