using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;

namespace midilonia.Views;

public partial class ImportExport : UserControl
{
    public ImportExport()
    {
        InitializeComponent();
    }

    private async void OpenFile_OnClick(object? sender, RoutedEventArgs e)
    {
        // Create a new open file dialog
        var openFileDialog = new OpenFileDialog
        {
            Title = "Open a file",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>  {
                new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
            }
        };

        // Get the containing window
        var parentWindow = (Window)TopLevel.GetTopLevel(this);

        // Call ShowAsync with that window
        var results = await openFileDialog.ShowAsync(parentWindow);

        if (results is { Length: > 0 })
        {
            var pickedFile = results[0];
            FilePathTextBlock.Text = $"Opened file: {pickedFile}";

            // TODO: Read or process the file
            // string contents = await File.ReadAllTextAsync(pickedFile);
        }
        else
        {
            FilePathTextBlock.Text = "No file selected.";
        }
    }
}
